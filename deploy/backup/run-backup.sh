#!/bin/sh
set -eu

# Fail instead of prompting if unattended authentication or configuration is incomplete.
export CLOUDSDK_CORE_DISABLE_PROMPTS=1

staging_root=${BACKUP_STAGING_ROOT:-/var/lib/exitpass-backup/staging}
status_root=${BACKUP_STATUS_ROOT:-/var/lib/exitpass-backup/status}
annex_root=${ANNEX_E1_ROOT:-/var/lib/exitpass/annex-e1}
password_file=${PGPASSWORD_FILE:-/run/secrets/postgres_backup_password}
recipient_file=${AGE_RECIPIENT_FILE:-/run/config/age-recipient.txt}

require_value() {
    name="$1"
    eval "value=\${$name:-}"
    if [ -z "$value" ]; then
        echo "Required backup configuration $name is missing." >&2
        exit 64
    fi
}

require_value PGHOST
require_value PGDATABASE
require_value PGUSER
require_value GCS_BUCKET
require_value GCS_PREFIX
require_value GCS_EXPECTED_LOCATION
require_value SOURCE_REVISION

backup_interval_seconds=${BACKUP_INTERVAL_SECONDS:-3600}
rpo_target_seconds=${BACKUP_RPO_TARGET_SECONDS:-3600}
restore_rto_target_seconds=${RESTORE_RTO_TARGET_SECONDS:-14400}
for numeric_value in "$backup_interval_seconds" "$rpo_target_seconds" "$restore_rto_target_seconds"; do
    case "$numeric_value" in
        ''|*[!0-9]*) echo "Backup schedule and recovery targets must be integer seconds." >&2; exit 64 ;;
    esac
    if [ "$numeric_value" -lt 300 ]; then
        echo "Backup schedule and recovery targets must be at least 300 seconds." >&2
        exit 64
    fi
done

case "$GCS_BUCKET" in
    gs://*|*/*|*'..'*)
        echo "GCS_BUCKET must be a bucket name, not a URL or path." >&2
        exit 65
        ;;
esac

gcs_prefix=$GCS_PREFIX
case "$gcs_prefix" in
    ''|/*|*/|*//*|*'..'*|*[!A-Za-z0-9._/-]*)
        echo "GCS_PREFIX is empty or unsafe." >&2
        exit 65
        ;;
esac

if [ ! -r "$password_file" ]; then
    echo "PostgreSQL backup password file is missing or unreadable." >&2
    exit 66
fi
if [ ! -r "$recipient_file" ]; then
    echo "Age recipient file is missing or unreadable." >&2
    exit 66
fi
if [ ! -d "$annex_root" ]; then
    echo "Annex E-1 artifact root is missing." >&2
    exit 66
fi
if [ -n "$(find "$annex_root" -mindepth 1 ! -type f ! -type d -print -quit)" ]; then
    echo "Annex E-1 storage contains an unsupported non-file entry." >&2
    exit 65
fi

db_password="$(tr -d '\r\n' < "$password_file")"
recipient="$(tr -d '\r\n' < "$recipient_file")"
if [ -z "$db_password" ] || [ -z "$recipient" ]; then
    echo "A required backup credential or encryption recipient is empty." >&2
    exit 65
fi

case "$recipient" in
    age1*|age-plugin-*) ;;
    *)
        echo "The encryption recipient is not a supported age recipient." >&2
        exit 65
        ;;
esac

pgpass_file="/tmp/pgpass.$$"
lock_dir="/tmp/exitpass-posserver-backup.lock"
if ! mkdir "$lock_dir" 2>/dev/null; then
    echo "Another backup operation is already running." >&2
    exit 75
fi
trap 'rm -f "$pgpass_file"; rmdir "$lock_dir" 2>/dev/null || true' EXIT HUP INT TERM

umask 077
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
random_id="$(tr -d '-' < /proc/sys/kernel/random/uuid)"
backup_id="${timestamp}-${random_id}"
work_dir="$staging_root/$backup_id"

if [ -e "$work_dir" ]; then
    echo "Backup work directory already exists." >&2
    exit 73
fi
mkdir -p "$work_dir" "$status_root"

escaped_password="$(printf '%s' "$db_password" | sed 's/\\/\\\\/g; s/:/\\:/g')"
printf '%s:%s:%s:%s:%s\n' "$PGHOST" "${PGPORT:-5432}" "$PGDATABASE" "$PGUSER" "$escaped_password" > "$pgpass_file"
unset db_password escaped_password
export PGPASSFILE="$pgpass_file"

started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
database_dump="$work_dir/database.dump"
database_list="$work_dir/database.list"
annex_archive="$work_dir/annex-e1.tar"

pg_dump \
    --host "$PGHOST" \
    --port "${PGPORT:-5432}" \
    --username "$PGUSER" \
    --dbname "$PGDATABASE" \
    --format custom \
    --compress 9 \
    --no-owner \
    --no-privileges \
    --file "$database_dump"

pg_restore --list "$database_dump" > "$database_list"
if [ ! -s "$database_list" ]; then
    echo "PostgreSQL archive verification produced an empty object list." >&2
    exit 74
fi

tar \
    --create \
    --file "$annex_archive" \
    --directory "$annex_root" \
    --exclude='*.tmp' \
    --sort=name \
    --mtime='UTC 1970-01-01' \
    --owner=0 \
    --group=0 \
    --numeric-owner \
    .
tar --list --file "$annex_archive" > /dev/null

postgres_version="$(psql --host "$PGHOST" --port "${PGPORT:-5432}" --username "$PGUSER" --dbname "$PGDATABASE" --tuples-only --no-align --command 'SHOW server_version')"
annex_file_count="$(find "$annex_root" -type f -name '*.xlsx' | wc -l | tr -d ' ')"
annex_bytes="$(du -sb "$annex_root" | awk '{print $1}')"
completed_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

(
    cd "$work_dir"
    sha256sum database.dump database.list annex-e1.tar > checksums.sha256
)

database_sha256="$(sha256sum "$database_dump" | awk '{print $1}')"
annex_sha256="$(sha256sum "$annex_archive" | awk '{print $1}')"

jq -n \
    --arg schema "exitpass-posserver-backup:v1" \
    --arg backupId "$backup_id" \
    --arg startedAt "$started_at" \
    --arg completedAt "$completed_at" \
    --arg sourceRevision "$SOURCE_REVISION" \
    --arg database "$PGDATABASE" \
    --arg postgresVersion "$postgres_version" \
    --arg databaseSha256 "$database_sha256" \
    --arg annexSha256 "$annex_sha256" \
    --argjson annexFileCount "$annex_file_count" \
    --argjson annexBytes "$annex_bytes" \
    --arg encryption "age-x25519" \
    --arg bucket "$GCS_BUCKET" \
    --arg prefix "$gcs_prefix" \
    --arg location "$GCS_EXPECTED_LOCATION" \
    --arg storageClass "${GCS_STORAGE_CLASS:-STANDARD}" \
    --arg retentionDays "${BACKUP_RETENTION_DAYS:-UNAPPROVED}" \
    --argjson backupIntervalSeconds "$backup_interval_seconds" \
    --argjson rpoTargetSeconds "$rpo_target_seconds" \
    --argjson restoreRtoTargetSeconds "$restore_rto_target_seconds" \
    '{
      schema: $schema,
      backupId: $backupId,
      startedAtUtc: $startedAt,
      completedAtUtc: $completedAt,
      sourceRevision: $sourceRevision,
      database: {name: $database, postgresVersion: $postgresVersion, sha256: $databaseSha256},
      annexE1: {fileCount: $annexFileCount, bytes: $annexBytes, archiveSha256: $annexSha256},
      encryption: $encryption,
      destination: {bucket: $bucket, prefix: $prefix, expectedLocation: $location, storageClass: $storageClass},
      consistency: {
        databaseSnapshot: "pg_dump consistent snapshot",
        artifactCaptureOrder: "after database dump",
        crossResourceAtomic: false,
        quietPeriodEnforcedByBackupService: false
      },
      schedule: {intervalSeconds: $backupIntervalSeconds},
      recoveryTargets: {rpoSeconds: $rpoTargetSeconds, restoreRtoSeconds: $restoreRtoTargetSeconds},
      retentionDays: $retentionDays,
      destructivePruningImplemented: false
    }' > "$work_dir/manifest.json"

(
    cd "$work_dir"
    sha256sum manifest.json >> checksums.sha256
    sha256sum --check checksums.sha256
)

plain_package="$work_dir/${backup_id}.tar"
encrypted_package="$work_dir/${backup_id}.tar.age"
encrypted_checksum="$work_dir/${backup_id}.tar.age.sha256"

tar --create --file "$plain_package" --directory "$work_dir" \
    --sort=name --mtime='UTC 1970-01-01' --owner=0 --group=0 --numeric-owner \
    manifest.json checksums.sha256 database.dump database.list annex-e1.tar
tar --list --file "$plain_package" > /dev/null

age --encrypt --recipient "$recipient" --output "$encrypted_package" "$plain_package"
(
    cd "$work_dir"
    sha256sum "${backup_id}.tar.age" > "${backup_id}.tar.age.sha256"
    sha256sum --check "${backup_id}.tar.age.sha256"
)

object_root="gs://${GCS_BUCKET}/${gcs_prefix}/${backup_id}"
archive_object="$object_root/${backup_id}.tar.age"
checksum_object="$object_root/${backup_id}.tar.age.sha256"
completion_object="$object_root/COMPLETE.json"
gcloud storage cp \
    "$encrypted_package" \
    "$archive_object" \
    --if-generation-match=0 \
    --storage-class="${GCS_STORAGE_CLASS:-STANDARD}" \
    --content-type=application/octet-stream \
    --quiet
gcloud storage cp \
    "$encrypted_checksum" \
    "$checksum_object" \
    --if-generation-match=0 \
    --storage-class="${GCS_STORAGE_CLASS:-STANDARD}" \
    --content-type=text/plain \
    --quiet

encrypted_sha256="$(awk '{print $1}' "$encrypted_checksum")"
encrypted_bytes="$(wc -c < "$encrypted_package" | tr -d ' ')"
components_uploaded_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
completion_marker="$work_dir/COMPLETE.json"
jq -n \
    --arg protocol "exitpass-posserver-complete-backup:v1" \
    --arg status "complete" \
    --arg backupId "$backup_id" \
    --arg componentsUploadedAt "$components_uploaded_at" \
    --arg archiveObject "$archive_object" \
    --arg checksumObject "$checksum_object" \
    --arg archiveSha256 "$encrypted_sha256" \
    --argjson archiveBytes "$encrypted_bytes" \
    '{
      protocol: $protocol,
      status: $status,
      backupId: $backupId,
      componentsUploadedAtUtc: $componentsUploadedAt,
      archive: {object: $archiveObject, sha256: $archiveSha256, bytes: $archiveBytes},
      checksum: {object: $checksumObject, algorithm: "sha256"}
    }' > "$completion_marker"
jq --exit-status \
    --arg backupId "$backup_id" \
    --arg archiveObject "$archive_object" \
    --arg checksumObject "$checksum_object" \
    '.protocol == "exitpass-posserver-complete-backup:v1"
      and .status == "complete"
      and .backupId == $backupId
      and .archive.object == $archiveObject
      and .checksum.object == $checksumObject
      and (.archive.sha256 | test("^[0-9a-f]{64}$"))
      and (.archive.bytes > 0)' \
    "$completion_marker" > /dev/null

# COMPLETE.json is the publication commit point. It is uploaded only after both required
# objects succeed, and create-only preconditions prevent replacement of any set member.
gcloud storage cp \
    "$completion_marker" \
    "$completion_object" \
    --if-generation-match=0 \
    --storage-class="${GCS_STORAGE_CLASS:-STANDARD}" \
    --content-type=application/json \
    --quiet

published_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
jq -n \
    --arg backupId "$backup_id" \
    --arg publishedAt "$published_at" \
    --arg object "$archive_object" \
    --arg completionMarker "$completion_object" \
    --arg sha256 "$encrypted_sha256" \
    '{status:"success", publicationProtocol:"exitpass-posserver-complete-backup:v1", backupId:$backupId, publishedAtUtc:$publishedAt, object:$object, completionMarker:$completionMarker, encryptedSha256:$sha256}' \
    > "$status_root/last-success.json.tmp"
mv "$status_root/last-success.json.tmp" "$status_root/last-success.json"

case "$work_dir" in
    "$staging_root"/*)
        rm -rf -- "$work_dir"
        ;;
    *)
        echo "Refusing to clean an unexpected backup work directory." >&2
        exit 70
        ;;
esac

echo "Backup $backup_id verified, encrypted, and uploaded successfully."
