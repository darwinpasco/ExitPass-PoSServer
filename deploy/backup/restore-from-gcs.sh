#!/bin/sh
set -eu

# Restore runs are operator-started but all gcloud calls must remain non-interactive.
export CLOUDSDK_CORE_DISABLE_PROMPTS=1

require_value() {
    name="$1"
    eval "value=\${$name:-}"
    if [ -z "$value" ]; then
        echo "Required restore configuration $name is missing." >&2
        exit 64
    fi
}

require_value BACKUP_ID
require_value GCS_BUCKET
require_value GCS_PREFIX
require_value RESTORE_PGHOST
require_value RESTORE_PGDATABASE
require_value RESTORE_PGUSER
require_value RESTORE_PG_PASSWORD_FILE
require_value RESTORE_ANNEX_E1_ROOT
require_value AGE_IDENTITY_FILE

case "$BACKUP_ID" in
    ''|.|..|*'..'*|*[!A-Za-z0-9._-]*) echo "BACKUP_ID contains unsupported characters." >&2; exit 65;;
esac
case "$GCS_BUCKET" in
    gs://*|*/*|*'..'*|'') echo "GCS_BUCKET must be a bucket name, not a URL or path." >&2; exit 65;;
esac
case "$RESTORE_PGDATABASE" in
    *restore*|*isolated*|*validation*|*disposable*|*test*) ;;
    *)
        echo "Refusing restore because the database name is not explicitly isolated." >&2
        exit 65
        ;;
esac

if [ ! -r "$RESTORE_PG_PASSWORD_FILE" ] || [ ! -r "$AGE_IDENTITY_FILE" ]; then
    echo "Restore database password or age identity file is unreadable." >&2
    exit 66
fi
if [ ! -d "$RESTORE_ANNEX_E1_ROOT" ]; then
    echo "The isolated Annex E-1 restore directory does not exist." >&2
    exit 66
fi
if [ -n "$(find "$RESTORE_ANNEX_E1_ROOT" -mindepth 1 -print -quit)" ]; then
    echo "Refusing restore because the isolated Annex E-1 restore directory is not empty." >&2
    exit 65
fi

gcs_prefix=$GCS_PREFIX
case "$gcs_prefix" in
    ''|/*|*/|*//*|*'..'*|*[!A-Za-z0-9._/-]*) echo "GCS_PREFIX is empty or unsafe." >&2; exit 65;;
esac
restore_root=${RESTORE_WORK_ROOT:-/var/lib/exitpass-backup/staging/restore}
work_dir="$restore_root/$BACKUP_ID"
if [ -e "$work_dir" ]; then
    echo "Restore work directory already exists; use a clean isolated path." >&2
    exit 73
fi
mkdir -p "$work_dir"

object_root="gs://${GCS_BUCKET}/${gcs_prefix}/${BACKUP_ID}"
encrypted_name="${BACKUP_ID}.tar.age"
checksum_name="${encrypted_name}.sha256"
completion_name="COMPLETE.json"
archive_object="$object_root/$encrypted_name"
checksum_object="$object_root/$checksum_name"
completion_object="$object_root/$completion_name"

# The completion marker is the publication commit point. If it is absent, malformed, or
# inconsistent with the requested object paths, the set is incomplete and must not restore.
gcloud storage cp "$completion_object" "$work_dir/$completion_name" --quiet
if ! jq --exit-status \
    --arg backupId "$BACKUP_ID" \
    --arg archiveObject "$archive_object" \
    --arg checksumObject "$checksum_object" \
    '.protocol == "exitpass-posserver-complete-backup:v1"
      and .status == "complete"
      and .backupId == $backupId
      and .archive.object == $archiveObject
      and .checksum.object == $checksumObject
      and .checksum.algorithm == "sha256"
      and (.archive.sha256 | test("^[0-9a-f]{64}$"))
      and (.archive.bytes > 0)' \
    "$work_dir/$completion_name" > /dev/null; then
    echo "The backup completion marker is invalid or does not describe this backup set." >&2
    exit 65
fi

expected_encrypted_sha256="$(jq -r '.archive.sha256' "$work_dir/$completion_name")"
expected_encrypted_bytes="$(jq -r '.archive.bytes' "$work_dir/$completion_name")"

gcloud storage cp "$archive_object" "$work_dir/$encrypted_name" --quiet
gcloud storage cp "$checksum_object" "$work_dir/$checksum_name" --quiet
if [ "$(wc -l < "$work_dir/$checksum_name" | tr -d ' ')" != 1 ]; then
    echo "The encrypted archive checksum metadata must contain exactly one record." >&2
    exit 65
fi
checksum_sha256="$(awk 'NR == 1 {print $1}' "$work_dir/$checksum_name")"
checksum_file="$(awk 'NR == 1 {print $2}' "$work_dir/$checksum_name")"
if [ "$checksum_sha256" != "$expected_encrypted_sha256" ] || [ "$checksum_file" != "$encrypted_name" ]; then
    echo "The encrypted archive checksum metadata does not match the completion marker." >&2
    exit 65
fi
if [ "$(wc -c < "$work_dir/$encrypted_name" | tr -d ' ')" != "$expected_encrypted_bytes" ]; then
    echo "The encrypted archive size does not match the completion marker." >&2
    exit 65
fi
(
    cd "$work_dir"
    sha256sum --check "$checksum_name"
)

age --decrypt --identity "$AGE_IDENTITY_FILE" --output "$work_dir/package.tar" "$work_dir/$encrypted_name"
tar --list --file "$work_dir/package.tar" > "$work_dir/package.list"
if grep -Eq '(^/|(^|/)\.\.(/|$))' "$work_dir/package.list"; then
    echo "The decrypted package contains an unsafe path." >&2
    exit 65
fi
mkdir "$work_dir/package"
tar --extract --file "$work_dir/package.tar" --directory "$work_dir/package"
(
    cd "$work_dir/package"
    sha256sum --check checksums.sha256
    pg_restore --list database.dump > /dev/null
)
tar --list --file "$work_dir/package/annex-e1.tar" > "$work_dir/annex-e1.list"
if grep -Eq '(^/|(^|/)\.\.(/|$))' "$work_dir/annex-e1.list"; then
    echo "The Annex E-1 archive contains an unsafe path." >&2
    exit 65
fi
if ! tar --list --verbose --file "$work_dir/package/annex-e1.tar" \
    | awk '{type=substr($1,1,1); if (type != "-" && type != "d") exit 1}'; then
    echo "The Annex E-1 archive contains an unsupported entry type." >&2
    exit 65
fi

restore_password="$(tr -d '\r\n' < "$RESTORE_PG_PASSWORD_FILE")"
escaped_password="$(printf '%s' "$restore_password" | sed 's/\\/\\\\/g; s/:/\\:/g')"
pgpass_file="/tmp/restore-pgpass.$$"
umask 077
printf '%s:%s:%s:%s:%s\n' \
    "$RESTORE_PGHOST" "${RESTORE_PGPORT:-5432}" "$RESTORE_PGDATABASE" "$RESTORE_PGUSER" "$escaped_password" > "$pgpass_file"
unset restore_password escaped_password
export PGPASSFILE="$pgpass_file"
trap 'rm -f "$pgpass_file"' EXIT HUP INT TERM

existing_objects="$(psql --host "$RESTORE_PGHOST" --port "${RESTORE_PGPORT:-5432}" --username "$RESTORE_PGUSER" --dbname "$RESTORE_PGDATABASE" --tuples-only --no-align --command "SELECT count(*) FROM information_schema.tables WHERE table_schema NOT IN ('pg_catalog','information_schema')")"
if [ "$existing_objects" != 0 ]; then
    echo "Refusing restore because the isolated target database is not empty." >&2
    exit 65
fi

pg_restore \
    --host "$RESTORE_PGHOST" \
    --port "${RESTORE_PGPORT:-5432}" \
    --username "$RESTORE_PGUSER" \
    --dbname "$RESTORE_PGDATABASE" \
    --exit-on-error \
    --no-owner \
    --no-privileges \
    "$work_dir/package/database.dump"

tar --extract --file "$work_dir/package/annex-e1.tar" --directory "$RESTORE_ANNEX_E1_ROOT"

jq -n \
    --arg backupId "$BACKUP_ID" \
    --arg restoredAt "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
    --arg database "$RESTORE_PGDATABASE" \
    --arg artifactRoot "$RESTORE_ANNEX_E1_ROOT" \
    '{status:"restored_to_isolated_target",backupId:$backupId,restoredAtUtc:$restoredAt,database:$database,artifactRoot:$artifactRoot}' \
    > "$work_dir/restore-result.json"

echo "Backup $BACKUP_ID restored to the isolated target. Application traffic was not enabled."
