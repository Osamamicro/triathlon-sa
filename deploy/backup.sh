#!/usr/bin/env bash
#
# Nightly backup of the Triathlon site on Linux: the database, then the uploaded media.
#
# Install with:
#   sudo install -m 0750 backup.sh /usr/local/bin/triathlon-backup
#   sudo crontab -e     # 15 2 * * *  /usr/local/bin/triathlon-backup >> /var/log/triathlon-backup.log 2>&1
#
# Reads the same environment file the service does, so there is one place holding the credentials.

set -euo pipefail

ENV_FILE="${ENV_FILE:-/etc/triathlon/production.env}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/triathlon}"
MEDIA_DIR="${MEDIA_DIR:-/var/lib/triathlon/media}"
RETENTION_DAYS="${RETENTION_DAYS:-30}"

if [[ -r "$ENV_FILE" ]]; then
    # shellcheck disable=SC1090
    set -a; source "$ENV_FILE"; set +a
fi

: "${ConnectionStrings__Default:?ConnectionStrings__Default is not set}"

stamp="$(date +%Y%m%d-%H%M%S)"
mkdir -p "$BACKUP_DIR"

# The application's connection string is the ADO.NET form ("Host=...;Database=..."), so the fields
# are pulled out of it rather than duplicated in a second place that could drift.
field() {
    printf '%s' "$ConnectionStrings__Default" \
        | tr ';' '\n' \
        | awk -F= -v key="$1" 'tolower($1)==tolower(key) { sub(/^[^=]*=/, ""); print; exit }'
}

PGHOST="$(field Host)"
PGPORT="$(field Port)"
PGUSER="$(field Username)"
PGPASSWORD="$(field Password)"
PGDATABASE="$(field Database)"
export PGHOST PGPORT PGUSER PGPASSWORD PGDATABASE

db_dump="$BACKUP_DIR/triathlon-$stamp.dump"

# Custom format: compressed, and restorable table-by-table with pg_restore.
pg_dump --format=custom --file="$db_dump" "$PGDATABASE"
echo "Database  -> $db_dump ($(du -h "$db_dump" | cut -f1))"

# Media is not in the database and is not regenerable, so it is backed up with it.
if [[ -d "$MEDIA_DIR" ]]; then
    media_archive="$BACKUP_DIR/media-$stamp.tar.gz"
    tar --create --gzip --file="$media_archive" --directory="$(dirname "$MEDIA_DIR")" "$(basename "$MEDIA_DIR")"
    echo "Media     -> $media_archive ($(du -h "$media_archive" | cut -f1))"
else
    echo "Media     -> skipped, $MEDIA_DIR does not exist"
fi

# Retention last, and only after the new backup succeeded — never delete yesterday's copy before
# today's exists.
find "$BACKUP_DIR" -maxdepth 1 -type f \( -name 'triathlon-*.dump' -o -name 'media-*.tar.gz' \) \
    -mtime "+$RETENTION_DAYS" -print -delete

echo "Done. Retention: $RETENTION_DAYS days."
