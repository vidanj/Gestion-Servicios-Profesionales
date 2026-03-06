#!/bin/bash
set -e

CONNECTION="Host=${DB_HOST};Port=${DB_PORT:-5432};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"

echo "Aplicando migraciones..."
./efbundle --connection "$CONNECTION"

echo "Iniciando aplicacion..."
exec dotnet SistemaServicios.API.dll
