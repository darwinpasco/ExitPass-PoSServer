#!/bin/sh
set -eu

status_file=${BACKUP_STATUS_ROOT:-/var/lib/exitpass-backup/status}/service-status.json
grace=${BACKUP_HEALTH_GRACE_SECONDS:-900}

case "$grace" in ''|*[!0-9]*) exit 1;; esac
[ -s "$status_file" ] || exit 1

service_status="$(jq -r '.serviceStatus // empty' "$status_file")"
last_result="$(jq -r '.lastResult // empty' "$status_file")"
next_run_epoch="$(jq -r '.nextRunEpoch // 0' "$status_file")"

[ "$service_status" = running ] || exit 1
[ "$last_result" != failure ] || exit 1
case "$next_run_epoch" in ''|*[!0-9]*) exit 1;; esac

now_epoch="$(date +%s)"
[ "$now_epoch" -le $((next_run_epoch + grace)) ]
