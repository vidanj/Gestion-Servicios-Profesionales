#!/usr/bin/env bash
# =============================================================================
# backup.sh — Genera un respaldo de la base de datos PostgreSQL
# Uso:  ./scripts/backup.sh
# =============================================================================
set -euo pipefail

# Rutas
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
ENV_FILE="$REPO_ROOT/.env"

# Cargar variables del archivo .env
if [ -f "$ENV_FILE" ]; then
    set -o allexport
    # shellcheck disable=SC1090
    source "$ENV_FILE"
    set +o allexport
fi

# ── Resolver credenciales ──────────────────────────────────────────────────
# Prioridad 1: variables individuales DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD
# Prioridad 2: parsear DB_CONNECTION (formato Npgsql: Host=;Port=;Database=;Username=;Password=)

if [ -n "${DB_HOST:-}" ]; then
    PG_HOST="$DB_HOST"
    PG_PORT="${DB_PORT:-5432}"
    PG_DB="$DB_NAME"
    PG_USER="$DB_USER"
    PGPASSWORD="$DB_PASSWORD"
elif [ -n "${DB_CONNECTION:-}" ]; then
    _parse() { echo "$DB_CONNECTION" | grep -oP "(?i)(?<=$1=)[^;]+" | head -1; }
    PG_HOST="$(_parse Host)"
    PG_PORT="$(_parse Port)"; PG_PORT="${PG_PORT:-5432}"
    PG_DB="$(_parse Database)"
    PG_USER="$(_parse Username)"
    PGPASSWORD="$(_parse Password)"
else
    echo "ERROR: No se encontraron variables de conexión." >&2
    echo "       Define DB_HOST / DB_PORT / DB_NAME / DB_USER / DB_PASSWORD" >&2
    echo "       o DB_CONNECTION en el archivo .env" >&2
    exit 1
fi

export PGPASSWORD

# ── Directorio y nombre del archivo ──────────────────────────────────────
BACKUP_DIR="$REPO_ROOT/backups"
mkdir -p "$BACKUP_DIR"

TIMESTAMP="$(date +%Y%m%d_%H%M)"
BACKUP_FILE="$BACKUP_DIR/backup_${TIMESTAMP}.sql"

# ── Ejecutar pg_dump ──────────────────────────────────────────────────────
echo "Generando respaldo de '${PG_DB}' (${PG_HOST}:${PG_PORT})..."
echo "Destino: ${BACKUP_FILE}"

pg_dump \
    --host="$PG_HOST" \
    --port="$PG_PORT" \
    --username="$PG_USER" \
    --dbname="$PG_DB" \
    --format=plain \
    --no-owner \
    --no-acl \
    --file="$BACKUP_FILE"

echo ""
echo "Respaldo completado exitosamente: $BACKUP_FILE"
