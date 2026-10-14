#!/bin/sh
set -eu

manifest="${POSSERVER_SCHEMA_MANIFEST:-/opt/exitpass/db/rebuild/pos_sql_apply_order.txt}"
controlled_code_dir="${POSSERVER_CONTROLLED_CODE_DIR:-/opt/exitpass/db/reference-data/controlled-codes/generated/sql}"
repo_root=/opt/exitpass

if [ ! -r "$manifest" ]; then
    echo "Authoritative schema manifest is missing: $manifest" >&2
    exit 66
fi

database_object_count="$(psql --set ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --tuples-only --no-align <<'SQL'
SELECT
    (SELECT count(*) FROM pg_catalog.pg_namespace
      WHERE nspname !~ '^pg_' AND nspname NOT IN ('information_schema', 'public'))
  + (SELECT count(*) FROM pg_catalog.pg_class c
       JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
      WHERE n.nspname = 'public')
  + (SELECT count(*) FROM pg_catalog.pg_proc p
       JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace
      WHERE n.nspname = 'public')
  + (SELECT count(*) FROM pg_catalog.pg_type t
       JOIN pg_catalog.pg_namespace n ON n.oid = t.typnamespace
      WHERE n.nspname = 'public')
  + (SELECT count(*) FROM pg_catalog.pg_largeobject_metadata);
SQL
)"
database_object_count="$(printf '%s' "$database_object_count" | tr -d '[:space:]')"
case "$database_object_count" in
    ''|*[!0-9]*) echo "Could not verify that the target database is empty." >&2; exit 65 ;;
esac
if [ "$database_object_count" -ne 0 ]; then
    echo "Refusing schema initialization because the target database contains user objects." >&2
    exit 65
fi

apply_as_admin() {
    sql_file="$1"
    {
        printf '%s\n' 'SET ROLE posserver_admin;'
        sed '1s/^\xEF\xBB\xBF//' "$sql_file"
    } | psql --set ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB"
}

while IFS= read -r manifest_entry || [ -n "$manifest_entry" ]; do
    manifest_entry="$(printf '%s' "$manifest_entry" | tr -d '\r')"
    case "$manifest_entry" in
        ''|'#'*) continue ;;
        db/*.sql) ;;
        *)
            echo "Unsafe or unsupported schema manifest entry: $manifest_entry" >&2
            exit 65
            ;;
    esac
    case "$manifest_entry" in
        *'..'*)
            echo "Parent traversal is not allowed in schema manifest entries." >&2
            exit 65
            ;;
    esac

    sql_file="$repo_root/$manifest_entry"
    if [ ! -r "$sql_file" ]; then
        echo "Manifest SQL file is missing: $sql_file" >&2
        exit 66
    fi
    echo "Applying authoritative schema object: $manifest_entry"
    apply_as_admin "$sql_file"
done < "$manifest"

controlled_code_count=0
for sql_file in "$controlled_code_dir"/*.sql; do
    if [ ! -f "$sql_file" ]; then
        echo "No generated controlled-code SQL files were found." >&2
        exit 66
    fi
    echo "Applying controlled-code source: ${sql_file#${repo_root}/}"
    apply_as_admin "$sql_file"
    controlled_code_count=$((controlled_code_count + 1))
done

if [ "$controlled_code_count" -eq 0 ]; then
    echo "Controlled-code initialization applied no files." >&2
    exit 65
fi

echo "Authoritative schema and controlled-code initialization completed."
