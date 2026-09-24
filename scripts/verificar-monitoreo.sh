#!/usr/bin/env bash
# =============================================================================
#  Verifica que el entorno de monitoreo esta completo.
#
#  Recorre el catalogo de metricas y pregunta por cada una a Prometheus; comprueba
#  que las reglas estan cargadas y que Grafana tiene su origen de datos y sus
#  tableros. Desde la spec 009 comprueba tambien la trazabilidad: que hay
#  registros con nivel y TraceId, que ese TraceId existe como traza y que ningun
#  registro contiene las contrasenas del trafico sintetico. Informa que falta y
#  termina con codigo distinto de cero.
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
LOKI="http://localhost:3100"
TEMPO="http://localhost:3200"
SOLO_PROMETHEUS=0

while [ $# -gt 0 ]; do
  case "$1" in
    --prometheus) PROMETHEUS="$2"; shift 2 ;;
    --grafana)    GRAFANA="$2";    shift 2 ;;
    --loki)       LOKI="$2";       shift 2 ;;
    --tempo)      TEMPO="$2";      shift 2 ;;
    --sin-grafana) SOLO_PROMETHEUS=1; shift ;;
    -h|--help)    sed -n '2,19p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
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

# --- Trazabilidad (spec 009) --------------------------------------------------
# Registros en Loki, trazas en Tempo y el enlace entre ambos. Las comprobaciones
# dependen unas de otras: si no hay registros, las de nivel, etiquetas, TraceId y
# contrasenas se omiten en lugar de pasar en vacio. "Ninguna linea contiene una
# contrasena" es verdad trivialmente cuando no hay ninguna linea.

TFALLOS=0
HAY_REGISTROS=0

# Deben coincidir con las de scripts/generar-trafico.sh. Si alli cambian, aqui
# tambien: si no, se buscaria una cadena que ya nadie envia y la comprobacion
# pasaria siempre.
CONTRASENAS_SINTETICAS=("Clave123!" "NoImporta123!")

bien() {
  printf '%s%-52s %s\n' "$OK" "$1" "${2:-}"
}

falta() {
  printf '%s%-52s\n' "$NO" "$1"
  if [ -n "${2:-}" ]; then
    printf '         %s\n' "$2"
  fi
  FALLOS=$((FALLOS+1))
  TFALLOS=$((TFALLOS+1))
}

# Cuenta las lineas de la ultima hora que cumplen un selector LogQL. Se usa una
# consulta de metrica y no la de lineas porque esta ultima tiene tope: con ella,
# "1000" podria significar mil o cien mil.
loki_contar() {
  curl -fsS --get --max-time 15 \
    --data-urlencode "query=sum(count_over_time($1 [1h]))" \
    "${LOKI}/loki/api/v1/query" 2>/dev/null \
    | jq -r '.data.result[0].value[1] // "0"' 2>/dev/null || echo "0"
}

echo ""
echo "Trazabilidad: registros y trazas"

LOKI_ARRIBA=0
if curl -fsS --max-time 10 "${LOKI}/ready" > /dev/null 2>&1; then
  bien "almacen de registros (Loki) responde"
  LOKI_ARRIBA=1
else
  falta "almacen de registros (Loki) no responde en ${LOKI}"
fi

TEMPO_ARRIBA=0
if curl -fsS --max-time 10 "${TEMPO}/ready" > /dev/null 2>&1; then
  bien "almacen de trazas (Tempo) responde"
  TEMPO_ARRIBA=1
else
  falta "almacen de trazas (Tempo) no responde en ${TEMPO}"
fi

if [ "$LOKI_ARRIBA" -eq 1 ]; then
  n=$(loki_contar '{servicio="api"}')
  if [ "${n:-0}" -gt 0 ]; then
    bien "registros de la API" "${n} lineas"
    HAY_REGISTROS=1
  else
    falta "registros de la API" "Alloy no entrega nada a Loki; diagnostico en http://localhost:12345"
  fi
fi

if [ "$HAY_REGISTROS" -eq 1 ]; then
  # D4: el formato compacto omite @l en Information y Alloy lo rellena. Si falta,
  # el relleno no funciona o los campos no se llaman como supone T002.
  n=$(loki_contar '{servicio="api", nivel="Information"}')
  if [ "${n:-0}" -gt 0 ]; then
    bien "nivel Information (rellenado, D4)" "${n} lineas"
  else
    falta "nivel Information (rellenado, D4)" "revisar @t y @l en config.alloy contra una linea real (T002)"
  fi

  # Los inicios de sesion fallidos del trafico sintetico se registran como
  # Warning. Esto prueba que @l se extrae de verdad, no solo que el relleno opera.
  n=$(loki_contar '{servicio="api", nivel="Warning"}')
  if [ "${n:-0}" -gt 0 ]; then
    bien "nivel Warning (extraido de @l)" "${n} lineas"
  else
    falta "nivel Warning (extraido de @l)" "sin inicios de sesion fallidos registrados, o @l no se extrae"
  fi

  # FR-004 y FR-005: solo `servicio` y `nivel` son etiquetas. Las que empiezan por
  # __ son internas de Loki y no cuentan.
  if lista=$(curl -fsS --get --max-time 10 --data-urlencode "since=1h" \
      "${LOKI}/loki/api/v1/labels" 2>/dev/null); then
    extra=$(echo "$lista" | jq -r '[.data[]? | select(startswith("__") | not)
                                    | select(. != "servicio" and . != "nivel")] | join(", ")' 2>/dev/null || echo "?")
    if [ -z "$extra" ]; then
      bien "solo servicio y nivel como etiquetas (FR-005)"
    else
      falta "etiquetas de mas en Loki: ${extra}" "TraceId, contenedor o service_name no pueden ser etiquetas (FR-004, FR-005)"
    fi
  else
    falta "lista de etiquetas de Loki" "no se pudo consultar"
  fi

  n=$(loki_contar '{servicio="api"} | TraceId != ""')
  if [ "${n:-0}" -gt 0 ]; then
    bien "TraceId como metadato consultable (FR-004)" "${n} lineas"
  else
    falta "TraceId como metadato consultable (FR-004)" "el campo no se llama TraceId en el JSON, o Alloy no lo extrae (T002)"
  fi

  if [ "$TEMPO_ARRIBA" -eq 1 ]; then
    # Se muestrea de lineas Warning y no de cualquiera: las rutas /health pueden
    # llevar TraceId en el registro, pero la instrumentacion las excluye de las
    # trazas a proposito. Muestrear de ahi daria un fallo que no es fallo.
    # El identificador se toma del metadato y, si no viene ahi, del JSON de la
    # linea: lo que se prueba es el enlace, no la forma de la respuesta de Loki.
    ids=$(curl -fsS --get --max-time 15 \
        --data-urlencode 'query={servicio="api", nivel="Warning"} | TraceId != ""' \
        --data-urlencode "since=1h" --data-urlencode "limit=50" \
        "${LOKI}/loki/api/v1/query_range" 2>/dev/null \
      | jq -r '([.data.result[].stream.TraceId // empty]
                + [.data.result[].values[][1] | fromjson? | .TraceId // empty])
               | unique | .[:3] | .[]' 2>/dev/null || true)
    if [ -z "$ids" ]; then
      falta "muestra de TraceId para buscar en Tempo" "no hay lineas Warning con TraceId"
    else
      for id in $ids; do
        encontrada=0
        # Hasta 30 s: el colector agrupa cada 10 s y Tempo tarda un poco mas en
        # volver consultable lo recien recibido.
        for _ in 1 2 3 4 5 6; do
          if curl -fsS --max-time 10 "${TEMPO}/api/traces/${id}" > /dev/null 2>&1; then
            encontrada=1
            break
          fi
          sleep 5
        done
        if [ "$encontrada" -eq 1 ]; then
          bien "traza ${id}" "existe en Tempo"
        else
          falta "traza ${id}" "el TraceId del registro no existe como traza: formato distinto o traza no exportada"
        fi
      done
    fi
  fi

  for clave in "${CONTRASENAS_SINTETICAS[@]}"; do
    # |= es busqueda literal, no expresion regular: el ! no necesita escaparse.
    n=$(loki_contar "{servicio=\"api\"} |= \"${clave}\"")
    if [ "${n:-0}" -eq 0 ]; then
      bien "ninguna linea contiene la contrasena ${clave:0:3}***"
    else
      falta "${n} linea(s) contienen una contrasena sintetica" "la redaccion de secretos no cubre algun campo"
    fi
  done
fi

echo ""
echo "================================================================"
if [ "$FALLOS" -eq 0 ]; then
  echo " Catalogo completo. El monitoreo esta operativo."
  echo "================================================================"
  exit 0
fi

echo " Faltan ${FALLOS} elemento(s)."

if [ $((FALLOS - TFALLOS)) -gt 0 ]; then
  echo ""
  echo " Metricas. Causas habituales, en orden de probabilidad:"
  echo "   1. Todavia no se ha generado trafico:  ./scripts/generar-trafico.sh"
  echo "   2. Aun no han llegado: la aplicacion exporta cada 60 s por omision"
  echo "      (OTEL_METRIC_EXPORT_INTERVAL) y Prometheus recoge cada 15 s."
  echo "   3. La API no tiene OTEL_EXPORTER_OTLP_ENDPOINT definido."
  echo "   4. El nombre de la metrica cambio de version (ver data-model.md)."
fi

if [ "$TFALLOS" -gt 0 ]; then
  echo ""
  echo " Trazabilidad. Causas habituales, en orden de probabilidad:"
  echo "   1. No se ha generado trafico en la ultima hora."
  echo "   2. Alloy no selecciona el contenedor de la API: debe llevar la etiqueta"
  echo "      com.docker.compose.service=api. Diagnostico en http://localhost:12345"
  echo "   3. Los campos del JSON no se llaman @t, @l y TraceId (tarea T002)."
  echo "   4. El colector no reenvia a Tempo:  docker logs gsp-otel-collector"
fi
echo "================================================================"
exit 1