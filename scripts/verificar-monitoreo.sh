#!/usr/bin/env bash
# =============================================================================
#  Verifica que el entorno de monitoreo esta completo.
#
#  Recorre el catalogo de metricas y pregunta por cada una a Prometheus; comprueba
#  que las reglas estan cargadas y que Grafana tiene su origen de datos y sus
#  tableros. Informa que falta y termina con codigo distinto de cero.
#
#  Es el MISMO guion que ejecuta la integracion continua. Lo que se comprueba en
#  un equipo de trabajo es exactamente lo que se comprueba en el pipeline: no hay
#  dos definiciones de "el monitoreo esta bien".
#
#  Uso:
#    ./scripts/verificar-monitoreo.sh
#    ./scripts/verificar-monitoreo.sh --prometheus http://localhost:9090
# =============================================================================
set -euo pipefail

PROMETHEUS="http://localhost:9090"
GRAFANA="http://localhost:3001"
SOLO_PROMETHEUS=0

while [ $# -gt 0 ]; do
  case "$1" in
    --prometheus) PROMETHEUS="$2"; shift 2 ;;
    --grafana)    GRAFANA="$2";    shift 2 ;;
    --sin-grafana) SOLO_PROMETHEUS=1; shift ;;
    -h|--help)    sed -n '2,16p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Opcion desconocida: $1" >&2; exit 2 ;;
  esac
done

# --- Catalogo -----------------------------------------------------------------
# Esta lista es la especificacion ejecutable del catalogo documentado en
# specs/008-monitoreo-metricas-alertas/data-model.md. Anadir una metrica alli sin
# anadirla aqui hace que su ausencia pase inadvertida durante meses.

PETICIONES=(
  "http_server_request_duration_seconds_count"
  "http_server_request_duration_seconds_bucket"
  "http_server_active_requests"
  "kestrel_active_connections"
  "aspnetcore_routing_match_attempts_total"
)

# En .NET 9 los nombres son dotnet_*, no process_runtime_dotnet_*: el paquete de
# instrumentacion registra el medidor integrado de la plataforma. Copiar tableros
# escritos para .NET 6-8 produce paneles permanentemente vacios.
RUNTIME=(
  "dotnet_process_memory_working_set_bytes"
  "dotnet_gc_collections_total"
  "dotnet_thread_pool_queue_length_total"
  "dotnet_exceptions_total"
)

NEGOCIO=(
  "gsp_autenticacion_intentos_total"
  "gsp_usuarios_registrados_total"
  "gsp_solicitudes_creadas_total"
)

SONDAS=(
  "probe_success"
  "probe_duration_seconds"
)

REGLAS_REGISTRO=(
  "gsp:http_peticiones:tasa5m"
  "gsp:http_proporcion_error:5m"
  "gsp:http_latencia:p95_5m"
)

ALERTAS=(
  "ApiNoDisponible" "BaseDeDatosInalcanzable" "ApiSinMetricas"
  "TasaDeErrorServidorAlta" "LatenciaP95Degradada" "LatenciaP95FueraDelAcuerdo"
  "MemoriaCercaDelLimite" "ColaDeHilosCreciente"
  "IntentosDeAutenticacionFallidos" "RespaldoFallido"
)

FALLOS=0
OK="  [ok]  "
NO="  [FALTA]"

consultar() {
  # Devuelve el numero de series que responden a la consulta.
  curl -fsS --get --data-urlencode "query=$1" "${PROMETHEUS}/api/v1/query" 2>/dev/null \
    | jq -r '.data.result | length' 2>/dev/null || echo "0"
}

comprobar_grupo() {
  local titulo="$1"; shift
  echo ""
  echo "${titulo}"
  for metrica in "$@"; do
    local n
    n=$(consultar "$metrica")
    if [ "${n:-0}" -gt 0 ]; then
      printf '%s%-52s %s series\n' "$OK" "$metrica" "$n"
    else
      printf '%s%-52s\n' "$NO" "$metrica"
      FALLOS=$((FALLOS+1))
    fi
  done
}

echo "================================================================"
echo " Verificacion del entorno de monitoreo"
echo " Prometheus: ${PROMETHEUS}"
echo "================================================================"

if ! curl -fsS --max-time 10 "${PROMETHEUS}/-/ready" > /dev/null 2>&1; then
  echo "ERROR: Prometheus no responde en ${PROMETHEUS}." >&2
  echo "Levanta el entorno con ./scripts/monitoreo-up.sh" >&2
  exit 1
fi

comprobar_grupo "Peticiones y servidor"        "${PETICIONES[@]}"
comprobar_grupo "Tiempo de ejecucion (.NET 9)" "${RUNTIME[@]}"
comprobar_grupo "Negocio"                      "${NEGOCIO[@]}"
comprobar_grupo "Sondeo externo"               "${SONDAS[@]}"
comprobar_grupo "Reglas de registro"           "${REGLAS_REGISTRO[@]}"

# --- Alertas cargadas ---------------------------------------------------------
echo ""
echo "Reglas de alerta"
CARGADAS=$(curl -fsS "${PROMETHEUS}/api/v1/rules" 2>/dev/null \
  | jq -r '[.data.groups[].rules[] | select(.type=="alerting") | .name] | join(" ")' 2>/dev/null || echo "")

for alerta in "${ALERTAS[@]}"; do
  if echo " $CARGADAS " | grep -q " $alerta "; then
    printf '%s%-52s\n' "$OK" "$alerta"
  else
    printf '%s%-52s\n' "$NO" "$alerta"
    FALLOS=$((FALLOS+1))
  fi
done

# --- Grafana ------------------------------------------------------------------
if [ "$SOLO_PROMETHEUS" -eq 0 ]; then
  echo ""
  echo "Grafana"
  if curl -fsS --max-time 10 "${GRAFANA}/api/health" > /dev/null 2>&1; then
    printf '%s%-52s\n' "$OK" "responde en ${GRAFANA}"
  else
    printf '%s%-52s\n' "$NO" "no responde en ${GRAFANA}"
    FALLOS=$((FALLOS+1))
  fi
fi

echo ""
echo "================================================================"
if [ "$FALLOS" -eq 0 ]; then
  echo " Catalogo completo. El monitoreo esta operativo."
  echo "================================================================"
  exit 0
fi

echo " Faltan ${FALLOS} elemento(s)."
echo ""
echo " Causas habituales, en orden de probabilidad:"
echo "   1. Todavia no se ha generado trafico:  ./scripts/generar-trafico.sh"
echo "   2. Aun no han llegado: la aplicacion exporta cada 60 s por omision"
echo "      (OTEL_METRIC_EXPORT_INTERVAL) y Prometheus recoge cada 15 s."
echo "   3. La API no tiene OTEL_EXPORTER_OTLP_ENDPOINT definido."
echo "   4. El nombre de la metrica cambio de version (ver data-model.md)."
echo "================================================================"
exit 1
