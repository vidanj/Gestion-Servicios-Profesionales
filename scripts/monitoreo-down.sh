#!/usr/bin/env bash
# =============================================================================
#  Derriba el entorno de monitoreo.
#
#  Por omision conserva los datos: el historico de metricas es evidencia.
#  --purgar los elimina, y pide confirmacion porque es destructivo
#  (constitucion, Principio IV).
#
#  Uso:
#    ./scripts/monitoreo-down.sh
#    ./scripts/monitoreo-down.sh --purgar
# =============================================================================
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PURGAR=0

while [ $# -gt 0 ]; do
  case "$1" in
    --purgar)  PURGAR=1; shift ;;
    -h|--help) sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Opcion desconocida: $1" >&2; exit 2 ;;
  esac
done

# La aplicacion primero: si se quitara antes la red del monitoreo, quedaria un
# contenedor colgado de una red que ya no existe.
echo "Deteniendo la aplicacion..."
docker compose -f "${RAIZ}/docker-compose.yml" \
               -f "${RAIZ}/monitoring/docker-compose.api.yml" down > /dev/null 2>&1 || true

if [ "$PURGAR" -eq 1 ]; then
  cat <<'AVISO'

ATENCION: se van a eliminar los volumenes del monitoreo.
  gsp_prometheus_datos   (historico de metricas)
  gsp_alertmanager_datos (silencios y estado de alarmas)
  gsp_grafana_datos      (estado de Grafana)
  gsp_loki_datos         (registros de la API)
  gsp_tempo_datos        (trazas de la API)
  gsp_alloy_datos        (posicion de lectura del recolector)

Esto NO se puede deshacer.
AVISO
  printf 'Escribe PURGAR para confirmar: '
  read -r respuesta
  if [ "$respuesta" != "PURGAR" ]; then
    echo "Cancelado. No se borro nada."
    exit 0
  fi
  docker compose -f "${RAIZ}/monitoring/docker-compose.yml" down -v
  echo "Entorno derribado y datos eliminados."
else
  docker compose -f "${RAIZ}/monitoring/docker-compose.yml" down
  echo "Entorno derribado. Los datos se conservan para el proximo arranque."
  echo "Para eliminarlos tambien: ./scripts/monitoreo-down.sh --purgar"
fi
