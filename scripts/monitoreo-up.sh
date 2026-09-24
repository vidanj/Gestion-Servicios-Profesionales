#!/usr/bin/env bash
# =============================================================================
#  Levanta el entorno de monitoreo y, opcionalmente, la aplicacion conectada.
#
#  El orden importa: el monitoreo primero, porque declara la red que la
#  superposicion de la aplicacion referencia como externa.
#
#  Uso:
#    ./scripts/monitoreo-up.sh
#    ./scripts/monitoreo-up.sh --sin-api
# =============================================================================
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SIN_API=0

while [ $# -gt 0 ]; do
  case "$1" in
    --sin-api) SIN_API=1; shift ;;
    -h|--help) sed -n '2,11p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Opcion desconocida: $1" >&2; exit 2 ;;
  esac
done

if [ -f "${RAIZ}/.env" ]; then
  set -a
  # shellcheck disable=SC1091
  . "${RAIZ}/.env"
  set +a
fi

# Se comprueba ANTES de arrancar: si falta, Grafana quedaria con la contrasena
# por defecto y esa instalacion acabaria en las capturas del documento.
if [ -z "${GRAFANA_ADMIN_PASSWORD:-}" ]; then
  echo "ERROR: GRAFANA_ADMIN_PASSWORD no esta definida." >&2
  echo "Anadela al .env antes de levantar el entorno." >&2
  exit 1
fi
export GRAFANA_ADMIN_PASSWORD

docker version --format '{{.Server.Version}}' > /dev/null 2>&1 || {
  echo "ERROR: Docker no esta en marcha." >&2
  exit 1
}

echo "Levantando el entorno de monitoreo..."
docker compose -f "${RAIZ}/monitoring/docker-compose.yml" up -d

esperar() {
  for _ in $(seq 1 30); do
    if curl -fsS --max-time 5 "$2" > /dev/null 2>&1; then
      echo "  [ok]  $1"
      return 0
    fi
    sleep 5
  done
  echo "  [FALLA] $1 no respondio en 150 s" >&2
  return 1
}

echo "Esperando a que cada herramienta responda..."
SANAS=0
esperar "colector"     "http://localhost:13133"            || SANAS=1
esperar "prometheus"   "http://localhost:9090/-/ready"     || SANAS=1
esperar "alertmanager" "http://localhost:9093/-/ready"     || SANAS=1
esperar "grafana"      "http://localhost:3001/api/health"  || SANAS=1
# Spec 009. Loki tarda unos segundos mas que el resto en responder /ready: espera
# a que su anillo interno se estabilice antes de declararse listo.
esperar "loki"         "http://localhost:3100/ready"       || SANAS=1
esperar "tempo"        "http://localhost:3200/ready"       || SANAS=1
esperar "alloy"        "http://localhost:12345/-/ready"    || SANAS=1

if [ "$SANAS" -ne 0 ]; then
  echo "El entorno no quedo sano." >&2
  exit 1
fi

if [ "$SIN_API" -eq 0 ]; then
  echo "Levantando la aplicacion conectada al colector..."
  docker compose -f "${RAIZ}/docker-compose.yml" \
                 -f "${RAIZ}/monitoring/docker-compose.api.yml" up -d --build \
    || echo "La aplicacion no arranco; el monitoreo sigue en pie."
fi

cat <<'FIN'

================================================================
 Entorno en marcha
================================================================
  Tableros (Grafana) .... http://localhost:3001
  Prometheus ............ http://localhost:9090
  Alarmas ............... http://localhost:9093
  Sondeo externo ........ http://localhost:9115
  Registros (Loki) ...... http://localhost:3100   (solo API; se consulta desde Grafana)
  Trazas (Tempo) ........ http://localhost:3200   (solo API; se consulta desde Grafana)
  Recolector (Alloy) .... http://localhost:12345  (diagnostico de la recoleccion)

 Siguiente paso:
   ./scripts/generar-trafico.sh
   ./scripts/verificar-monitoreo.sh
================================================================
FIN
