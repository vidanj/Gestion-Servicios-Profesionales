#!/usr/bin/env bash
# =============================================================================
#  Genera trafico representativo contra la API.
#
#  Sirve para dos cosas: poblar los tableros antes de tomar una captura, y dar al
#  flujo de validacion algo que medir. El trafico es deliberadamente variado
#  -correcto, no encontrado, credenciales malas, alta, solicitud- porque si solo
#  hubiera peticiones correctas, los paneles de error quedarian vacios y no se
#  podria comprobar que funcionan.
#
#  No crea nada que no se pueda repetir: las altas usan correos con marca de
#  tiempo y todo ocurre contra el entorno que se le indique.
#
#  Uso:
#    ./scripts/generar-trafico.sh
#    ./scripts/generar-trafico.sh --url http://localhost:10000 --duracion 60
# =============================================================================
set -euo pipefail

URL="http://localhost:5000"
DURACION=30

while [ $# -gt 0 ]; do
  case "$1" in
    --url)      URL="$2";      shift 2 ;;
    --duracion) DURACION="$2"; shift 2 ;;
    -h|--help)  sed -n '2,18p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Opcion desconocida: $1" >&2; exit 2 ;;
  esac
done

if ! curl -fsS --max-time 10 "${URL}/health/live" > /dev/null 2>&1; then
  echo "ERROR: la API no responde en ${URL}." >&2
  exit 1
fi

echo "Generando trafico contra ${URL} durante ${DURACION} s..."

correctas=0; noencontradas=0; loginfallido=0; altas=0; solicitudes=0

pedir() {
  curl -fsS -o /dev/null --max-time 10 "$@" 2>/dev/null
}

# El endpoint de solicitudes exige sesion iniciada. Sin este paso, las peticiones se
# quedan en el 401 del middleware y nunca llegan a la capa de servicios, de modo que
# gsp_solicitudes_creadas_total no se mueve y parece que la instrumentacion falla.
MARCA_INICIAL="$(date +%s%N)"
TOKEN=$(curl -fsS --max-time 15 -X POST "${URL}/api/Auth/register" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"trafico_sesion_${MARCA_INICIAL}@ejemplo.com\",\"password\":\"Clave123!\",\"firstName\":\"Trafico\",\"lastName\":\"Sesion\"}" 2>/dev/null \
  | sed -n 's/.*"token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' || true)

if [ -n "${TOKEN:-}" ]; then
  echo "  sesion iniciada para el trafico autenticado"
else
  echo "  aviso: no se pudo iniciar sesion; el trafico autenticado se omite"
fi

fin=$((SECONDS + DURACION))
while [ "$SECONDS" -lt "$fin" ]; do
  # Peticiones correctas: alimentan latencia y volumen.
  pedir "${URL}/" && correctas=$((correctas+1)) || true
  pedir "${URL}/health/ready" && correctas=$((correctas+1)) || true
  pedir "${URL}/api/Categories" && correctas=$((correctas+1)) || true
  pedir "${URL}/api/Services" && correctas=$((correctas+1)) || true

  # Ruta inexistente: alimenta aspnetcore_routing_match_attempts_total.
  curl -fsS -o /dev/null --max-time 10 "${URL}/api/NoExiste" 2>/dev/null \
    || noencontradas=$((noencontradas+1))

  # Inicio de sesion fallido: alimenta gsp_autenticacion_intentos_total y, si se
  # repite lo suficiente, tambien los rechazos del limitador.
  curl -fsS -o /dev/null --max-time 10 -X POST "${URL}/api/Auth/login" \
    -H 'Content-Type: application/json' \
    -d '{"email":"inexistente@ejemplo.com","password":"NoImporta123!"}' 2>/dev/null \
    || loginfallido=$((loginfallido+1))

  # Alta de usuario: alimenta gsp_usuarios_registrados_total.
  marca="$(date +%s%N)"
  if curl -fsS -o /dev/null --max-time 10 -X POST "${URL}/api/Auth/register" \
      -H 'Content-Type: application/json' \
      -d "{\"email\":\"trafico_${marca}@ejemplo.com\",\"password\":\"Clave123!\",\"firstName\":\"Trafico\",\"lastName\":\"Sintetico\"}" 2>/dev/null; then
    altas=$((altas+1))
  fi

  # Solicitud sobre un servicio inexistente, CON sesion: alimenta el resultado
  # "servicio_no_disponible" de gsp_solicitudes_creadas_total. Sin el token la
  # peticion muere en el 401 y nunca llega a la capa de servicios.
  if [ -n "${TOKEN:-}" ]; then
    curl -fsS -o /dev/null --max-time 10 -X POST "${URL}/api/ServiceRequests" \
      -H 'Content-Type: application/json' \
      -H "Authorization: Bearer ${TOKEN}" \
      -d '{"serviceId":999999,"description":"trafico sintetico"}' 2>/dev/null \
      || solicitudes=$((solicitudes+1))
  fi

  sleep 1
done

echo ""
echo "  Peticiones correctas .............. ${correctas}"
echo "  Rutas inexistentes ................ ${noencontradas}"
echo "  Inicios de sesion fallidos ........ ${loginfallido}"
echo "  Altas de usuario .................. ${altas}"
echo "  Solicitudes rechazadas ............ ${solicitudes}"
echo ""
echo "Espera unos 30 s (dos ciclos de recoleccion) antes de verificar:"
echo "  ./scripts/verificar-monitoreo.sh"
