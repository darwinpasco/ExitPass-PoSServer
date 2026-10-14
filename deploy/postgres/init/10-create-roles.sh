#!/bin/sh
set -eu

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
    echo "Refusing initialization because the target database contains user objects." >&2
    exit 65
fi

existing_role_count="$(psql --set ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --tuples-only --no-align --command "SELECT count(*) FROM pg_catalog.pg_roles WHERE rolname IN ('posserver_admin','posserver_app','posserver_backup')")"
existing_role_count="$(printf '%s' "$existing_role_count" | tr -d '[:space:]')"
case "$existing_role_count" in
    ''|*[!0-9]*) echo "Could not verify that the POS Server runtime roles are absent." >&2; exit 65 ;;
esac
if [ "$existing_role_count" -ne 0 ]; then
    echo "Refusing initialization because one or more POS Server runtime roles already exist." >&2
    exit 65
fi

read_secret() {
    variable_name="$1"
    file_path="$2"
    if [ ! -r "$file_path" ]; then
        echo "Required database role secret is missing: $file_path" >&2
        exit 66
    fi
    secret_value="$(tr -d '\r\n' < "$file_path")"
    if [ -z "$secret_value" ]; then
        echo "Required database role secret is empty: $file_path" >&2
        exit 65
    fi
    export "$variable_name=$secret_value"
    unset secret_value
}

read_secret POSSERVER_ADMIN_DB_PASSWORD /run/secrets/postgres_admin_password
read_secret POSSERVER_APP_DB_PASSWORD /run/secrets/postgres_app_password
read_secret POSSERVER_BACKUP_DB_PASSWORD /run/secrets/postgres_backup_password
export POSSERVER_TARGET_DB="$POSTGRES_DB"

psql --set ON_ERROR_STOP=1 \
    --username "$POSTGRES_USER" \
    --dbname "$POSTGRES_DB" \
    --file /docker-entrypoint-initdb.d/create-roles.psql

unset POSSERVER_ADMIN_DB_PASSWORD POSSERVER_APP_DB_PASSWORD
unset POSSERVER_BACKUP_DB_PASSWORD POSSERVER_TARGET_DB
