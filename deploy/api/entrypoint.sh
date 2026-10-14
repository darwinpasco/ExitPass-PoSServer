#!/bin/sh
set -eu

password_file="${POSSERVER_DB_PASSWORD_FILE:-/run/secrets/postgres_app_password}"
pgpass_file="${POSSERVER_DB_PASSFILE:-/tmp/exitpass-posserver.pgpass}"

require_value() {
    name="$1"
    eval "value=\${$name:-}"
    if [ -z "$value" ]; then
        echo "Required configuration $name is missing." >&2
        exit 64
    fi
}

require_value POSSERVER_DB_HOST
require_value POSSERVER_DB_NAME
require_value POSSERVER_DB_USER

if [ ! -r "$password_file" ]; then
    echo "Database password file is missing or unreadable." >&2
    exit 66
fi

db_password="$(tr -d '\r\n' < "$password_file")"
if [ -z "$db_password" ]; then
    echo "Database password file is empty." >&2
    exit 65
fi

escaped_password="$(printf '%s' "$db_password" | sed 's/\\/\\\\/g; s/:/\\:/g')"
umask 077
printf '%s:%s:%s:%s:%s\n' \
    "$POSSERVER_DB_HOST" \
    "${POSSERVER_DB_PORT:-5432}" \
    "$POSSERVER_DB_NAME" \
    "$POSSERVER_DB_USER" \
    "$escaped_password" > "$pgpass_file"
unset db_password escaped_password

export ConnectionStrings__PosServer="Host=${POSSERVER_DB_HOST};Port=${POSSERVER_DB_PORT:-5432};Database=${POSSERVER_DB_NAME};Username=${POSSERVER_DB_USER};Passfile=${pgpass_file};SSL Mode=Disable;Application Name=ExitPass.PosServer.Api"

exec dotnet ExitPass.PosServer.Api.dll
