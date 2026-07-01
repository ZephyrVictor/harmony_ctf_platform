#!/bin/sh
set -e

# Install pgbackrest if not present (survives container restart with volume mounts)
if ! command -v pgbackrest >/dev/null 2>&1; then
    apk add --no-cache pgbackrest >/dev/null 2>&1
fi

# Ensure pgbackrest config and repo directories exist with correct ownership
mkdir -p /etc/pgbackrest /var/lib/pgbackrest
chown -R postgres:postgres /var/lib/pgbackrest

# Execute original entrypoint
exec /usr/local/bin/docker-entrypoint.sh "$@"
