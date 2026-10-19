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
backup_schema="$(jq -r '.schema // empty' "$work_dir/package/manifest.json")"
case "$backup_schema" in
    exitpass-posserver-backup:v1|exitpass-posserver-backup:v2) ;;
    *) echo "The backup package manifest schema is unsupported." >&2; exit 65;;
esac
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

fiscal_report_verify_root=""
if [ "$backup_schema" = "exitpass-posserver-backup:v2" ]; then
    require_value RESTORE_FISCAL_REPORT_ARTIFACT_ROOT
    if [ ! -d "$RESTORE_FISCAL_REPORT_ARTIFACT_ROOT" ]; then
        echo "The isolated fiscal report artifact restore directory does not exist." >&2
        exit 66
    fi
    if [ -n "$(find "$RESTORE_FISCAL_REPORT_ARTIFACT_ROOT" -mindepth 1 -print -quit)" ]; then
        echo "Refusing restore because the isolated fiscal report artifact directory is not empty." >&2
        exit 65
    fi
    fiscal_report_archive="$work_dir/package/fiscal-report-artifacts.tar"
    fiscal_report_manifest="$work_dir/package/fiscal-report-artifacts-manifest.json"
    if [ ! -f "$fiscal_report_archive" ] || [ ! -f "$fiscal_report_manifest" ]; then
        echo "The backup package is missing fiscal report artifact components." >&2
        exit 65
    fi
    if ! jq --exit-status '
        .schema == "exitpass-posserver-fiscal-report-artifact-manifest:v1"
        and (.files | type == "array")
        and ([.files[].path] | length) == ([.files[].path] | unique | length)
        and all(.files[];
          ((.path | type) == "string" and (.path | test("^[A-Za-z0-9._/-]+$")) and (.path | startswith("/") | not) and (.path | contains("..") | not))
          and ((.byteLength | type) == "number" and .byteLength >= 0 and (.byteLength | floor) == .byteLength)
          and ((.sha256 | type) == "string" and (.sha256 | test("^[0-9a-f]{64}$"))))' \
        "$fiscal_report_manifest" > /dev/null; then
        echo "The fiscal report artifact file manifest is invalid." >&2
        exit 65
    fi
    expected_archive_sha256="$(jq -r '.fiscalReportArtifacts.archiveSha256' "$work_dir/package/manifest.json")"
    expected_manifest_sha256="$(jq -r '.fiscalReportArtifacts.fileManifestSha256' "$work_dir/package/manifest.json")"
    expected_file_count="$(jq -r '.fiscalReportArtifacts.fileCount' "$work_dir/package/manifest.json")"
    expected_file_bytes="$(jq -r '.fiscalReportArtifacts.bytes' "$work_dir/package/manifest.json")"
    actual_archive_sha256="$(sha256sum "$fiscal_report_archive" | awk '{print $1}')"
    actual_manifest_sha256="$(sha256sum "$fiscal_report_manifest" | awk '{print $1}')"
    manifest_file_count="$(jq '.files | length' "$fiscal_report_manifest")"
    manifest_file_bytes="$(jq '[.files[].byteLength] | add // 0' "$fiscal_report_manifest")"
    if [ "$actual_archive_sha256" != "$expected_archive_sha256" ] || \
       [ "$actual_manifest_sha256" != "$expected_manifest_sha256" ] || \
       [ "$manifest_file_count" != "$expected_file_count" ] || \
       [ "$manifest_file_bytes" != "$expected_file_bytes" ]; then
        echo "Fiscal report artifact package metadata is inconsistent." >&2
        exit 65
    fi
    tar --list --file "$fiscal_report_archive" > "$work_dir/fiscal-report-artifacts.list"
    if grep -Eq '(^/|(^|/)\.\.(/|$))' "$work_dir/fiscal-report-artifacts.list"; then
        echo "The fiscal report artifact archive contains an unsafe path." >&2
        exit 65
    fi
    if ! tar --list --verbose --file "$fiscal_report_archive" \
        | awk '{type=substr($1,1,1); if (type != "-" && type != "d") exit 1}'; then
        echo "The fiscal report artifact archive contains an unsupported entry type." >&2
        exit 65
    fi
    fiscal_report_verify_root="$work_dir/fiscal-report-artifacts-verify"
    mkdir "$fiscal_report_verify_root"
    tar --extract --file "$fiscal_report_archive" --directory "$fiscal_report_verify_root"
    jq -r '.files[] | [.path, (.byteLength|tostring), .sha256] | @tsv' "$fiscal_report_manifest" \
        | LC_ALL=C sort > "$work_dir/fiscal-report-artifacts-expected.tsv"
    cut -f 1 "$work_dir/fiscal-report-artifacts-expected.tsv" > "$work_dir/fiscal-report-artifacts-expected-paths.txt"
    find "$fiscal_report_verify_root" -type f -printf '%P\n' | LC_ALL=C sort > "$work_dir/fiscal-report-artifacts-actual-paths.txt"
    if ! cmp -s "$work_dir/fiscal-report-artifacts-expected-paths.txt" "$work_dir/fiscal-report-artifacts-actual-paths.txt"; then
        echo "The fiscal report artifact archive and file manifest contain different paths." >&2
        exit 65
    fi
    tab="$(printf '\t')"
    while IFS="$tab" read -r relative_path expected_bytes expected_sha256; do
        artifact_path="$fiscal_report_verify_root/$relative_path"
        actual_bytes="$(wc -c < "$artifact_path" | tr -d ' ')"
        actual_sha256="$(sha256sum "$artifact_path" | awk '{print $1}')"
        if [ "$actual_bytes" != "$expected_bytes" ] || [ "$actual_sha256" != "$expected_sha256" ]; then
            echo "A restored fiscal report artifact failed size or checksum validation." >&2
            exit 74
        fi
    done < "$work_dir/fiscal-report-artifacts-expected.tsv"
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

if [ "$backup_schema" = "exitpass-posserver-backup:v2" ]; then
    psql --host "$RESTORE_PGHOST" --port "${RESTORE_PGPORT:-5432}" --username "$RESTORE_PGUSER" --dbname "$RESTORE_PGDATABASE" \
        --tuples-only --no-align --field-separator '|' \
        --command "SELECT artifact_storage_key, artifact_byte_length, artifact_sha256 FROM pos.fiscal_report_artifacts ORDER BY artifact_storage_key" \
        > "$work_dir/restored-fiscal-report-artifact-catalog.tsv"
    while IFS='|' read -r storage_key expected_bytes expected_sha256; do
        [ -n "$storage_key" ] || continue
        artifact_path="$fiscal_report_verify_root/$storage_key"
        if [ ! -f "$artifact_path" ]; then
            echo "The restored database references a missing fiscal report artifact." >&2
            exit 74
        fi
        actual_bytes="$(wc -c < "$artifact_path" | tr -d ' ')"
        actual_sha256="$(sha256sum "$artifact_path" | awk '{print $1}')"
        if [ "$actual_bytes" != "$expected_bytes" ] || [ "$actual_sha256" != "$expected_sha256" ]; then
            echo "The restored database fiscal report catalog does not match the artifact archive." >&2
            exit 74
        fi
    done < "$work_dir/restored-fiscal-report-artifact-catalog.tsv"
fi

tar --extract --file "$work_dir/package/annex-e1.tar" --directory "$RESTORE_ANNEX_E1_ROOT"
if [ "$backup_schema" = "exitpass-posserver-backup:v2" ]; then
    tar --extract --file "$work_dir/package/fiscal-report-artifacts.tar" --directory "$RESTORE_FISCAL_REPORT_ARTIFACT_ROOT"
fi

jq -n \
    --arg backupId "$BACKUP_ID" \
    --arg restoredAt "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
    --arg database "$RESTORE_PGDATABASE" \
    --arg artifactRoot "$RESTORE_ANNEX_E1_ROOT" \
    --arg fiscalReportArtifactRoot "${RESTORE_FISCAL_REPORT_ARTIFACT_ROOT:-}" \
    '{status:"restored_to_isolated_target",backupId:$backupId,restoredAtUtc:$restoredAt,database:$database,artifactRoot:$artifactRoot,fiscalReportArtifactRoot:$fiscalReportArtifactRoot}' \
    > "$work_dir/restore-result.json"

echo "Backup $BACKUP_ID restored to the isolated target. Application traffic was not enabled."
