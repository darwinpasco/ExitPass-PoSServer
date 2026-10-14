#!/bin/sh
set -eu

status_root=${BACKUP_STATUS_ROOT:-/var/lib/exitpass-backup/status}
interval=${BACKUP_INTERVAL_SECONDS:-3600}
start_delay=${BACKUP_START_DELAY_SECONDS:-300}

case "$interval" in ''|*[!0-9]*) echo "BACKUP_INTERVAL_SECONDS must be a positive integer." >&2; exit 64;; esac
case "$start_delay" in ''|*[!0-9]*) echo "BACKUP_START_DELAY_SECONDS must be a nonnegative integer." >&2; exit 64;; esac
if [ "$interval" -lt 300 ]; then
    echo "BACKUP_INTERVAL_SECONDS must be at least 300." >&2
    exit 64
fi

mkdir -p "$status_root"

write_status() {
    result="$1"
    message="$2"
    next_epoch="$3"
    now="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    jq -n \
        --arg serviceStatus running \
        --arg result "$result" \
        --arg message "$message" \
        --arg updatedAt "$now" \
        --argjson nextRunEpoch "$next_epoch" \
        '{serviceStatus:$serviceStatus,lastResult:$result,message:$message,updatedAtUtc:$updatedAt,nextRunEpoch:$nextRunEpoch}' \
        > "$status_root/service-status.json.tmp"
    mv "$status_root/service-status.json.tmp" "$status_root/service-status.json"
}

stop_requested=false
sleep_pid=""

request_stop() {
    stop_requested=true
    if [ -n "$sleep_pid" ]; then
        kill "$sleep_pid" 2>/dev/null || true
    fi
}

stop_service() {
    now="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    jq -n --arg updatedAt "$now" '{serviceStatus:"stopped",updatedAtUtc:$updatedAt}' > "$status_root/service-status.json.tmp"
    mv "$status_root/service-status.json.tmp" "$status_root/service-status.json"
    exit 0
}

interruptible_sleep() {
    sleep "$1" &
    sleep_pid=$!
    wait "$sleep_pid" || true
    sleep_pid=""
    [ "$stop_requested" = false ]
}

trap request_stop HUP INT TERM

next_epoch=$(( $(date +%s) + start_delay ))
write_status waiting "Waiting for the configured initial delay." "$next_epoch"
if ! interruptible_sleep "$start_delay"; then
    stop_service
fi

while :; do
    attempt_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    next_epoch=$(( $(date +%s) + interval ))
    write_status running "Backup attempt started at $attempt_at." "$next_epoch"

    if /opt/exitpass-backup/run-backup.sh; then
        result=success
        message="The last backup was verified, encrypted, and uploaded."
    else
        exit_code=$?
        result=failure
        message="The last backup attempt failed with exit code $exit_code."
        echo "$message" >&2
    fi

    next_epoch=$(( $(date +%s) + interval ))
    write_status "$result" "$message" "$next_epoch"
    if ! interruptible_sleep "$interval"; then
        stop_service
    fi
done
