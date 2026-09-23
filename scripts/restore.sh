#!/usr/bin/env bash
# =============================================================================
# restore.sh — Restaura la base de datos PostgreSQL desde un archivo .sql
# Uso:  ./scripts/restore.sh <ruta/al/backup.sql>
# =============================================================================
set -euo pipefail

# ── Validar argumento ─────────────────────────────────────────────────────
if [ $# -lt 1 ]; then
    echo "Uso: $0 <ruta/al/backup.sql>" >&2
    echo "Ejemplo: $0 backups/backup_20260225_1430.sql" >&2
    exit 1
fi

BACKUP_FILE="$1"

if [ ! -f "$BACKUP_FILE" ]; then
    echo "ERROR: No se encontró el archivo: $BACKUP_FILE" >&2
    exit 1
fi

# ── Rutas ─────────────────────────────────────────────────────────────────
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

# ── Confirmación de seguridad ─────────────────────────────────────────────
echo "==========================================================="
echo "  RESTAURACION DE BASE DE DATOS"
echo "==========================================================="
echo "  Base de datos : $PG_DB"
echo "  Servidor      : $PG_HOST:$PG_PORT"
echo "  Archivo       : $BACKUP_FILE"
echo "==========================================================="
echo ""
echo "ADVERTENCIA: Esta operacion sobreescribira los datos actuales."
read -rp "¿Desea continuar? (escriba 's' para confirmar): " CONFIRM

if [[ "${CONFIRM,,}" != "s" ]]; then
    echo "Operacion cancelada."
    exit 0
fi

# ── Ejecutar psql ─────────────────────────────────────────────────────────
echo ""
echo "Restaurando '$PG_DB' desde '$BACKUP_FILE'..."

psql \
    --host="$PG_HOST" \
    --port="$PG_PORT" \
    --username="$PG_USER" \
    --dbname="$PG_DB" \
    --file="$BACKUP_FILE"

echo ""
echo "Restauracion completada exitosamente desde: $BACKUP_FILE"
