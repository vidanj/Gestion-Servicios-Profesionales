#!/usr/bin/env bash
# =============================================================================
#  Despliega y verifica, o revierte a un commit anterior.
#
#  Equivalente local de .github/workflows/deploy.yml: mismo disparo, misma espera,
#  mismo criterio de exito. Revertir no es otra cosa: es desplegar el commit sano
#  anterior, de modo que ese camino se ejercita en cada despliegue.
#
#  Variables requeridas (del entorno o del .env; nunca escritas aqui):
#    RENDER_DEPLOY_HOOK_URL   secreto del disparador
#    RENDER_SERVICE_URL       direccion publica a sondear
#
#  Uso:
#    ./scripts/desplegar.sh
#    ./scripts/desplegar.sh --sha a1b2c3d
#    ./scripts/desplegar.sh --revertir a1b2c3d
# =============================================================================
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OBJETIVO=""
ES_REVERSION=0

while [ $# -gt 0 ]; do
  case "$1" in
    --sha)      OBJETIVO="$2"; shift 2 ;;
    --revertir) OBJETIVO="$2"; ES_REVERSION=1; shift 2 ;;
    -h|--help)  sed -n '2,17p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Opcion desconocida: $1" >&2; exit 2 ;;
  esac
done

if [ -f "${RAIZ}/.env" ]; then
  set -a
  # shellcheck disable=SC1091
  . "${RAIZ}/.env"
  set +a
fi

if [ -z "${RENDER_DEPLOY_HOOK_URL:-}" ]; then
  echo "ERROR: falta RENDER_DEPLOY_HOOK_URL." >&2
  echo "Obtenlo en el panel del servicio (Settings -> Deploy Hook)." >&2
  exit 1
fi
if [ -z "${RENDER_SERVICE_URL:-}" ]; then
  echo "ERROR: falta RENDER_SERVICE_URL." >&2
  exit 1
fi

if [ "$ES_REVERSION" -eq 1 ]; then
  echo "REVERSION al commit ${OBJETIVO}"
elif [ -n "$OBJETIVO" ]; then
  echo "Despliegue del commit ${OBJETIVO}"
else
  echo "Despliegue del ultimo commit de la rama de integracion"
fi

INICIO=$SECONDS

URL="$RENDER_DEPLOY_HOOK_URL"
[ -n "$OBJETIVO" ] && URL="${URL}&ref=${OBJETIVO}"

# La salida se descarta: la URL lleva la clave dentro.
if curl -fsS -X POST "$URL" > /dev/null 2>&1; then
  echo "Despliegue solicitado."
else
  echo "ERROR: el disparador respondio con error." >&2
  exit 1
fi

# Sin esta pausa se sondearia la instancia ANTERIOR, que responde sana, y el
# despliegue se daria por bueno sin haber ocurrido.
echo "Pausa de 30 s antes del primer sondeo..."
sleep 30

SONDA="${RENDER_SERVICE_URL}/health/ready"
SANAS=0
ULTIMA=""
LOGRADO=0

echo "Sondeando ${SONDA} (hasta 600 s, exigiendo 3 respuestas sanas seguidas)..."
for intento in $(seq 1 60); do
  ULTIMA=$(curl -fsS --max-time 30 "$SONDA" 2>/dev/null || true)
  case "$ULTIMA" in
    *'"status":"Healthy"'*)
      SANAS=$((SANAS+1))
      echo "  intento ${intento}: sana (${SANAS}/3)"
      if [ "$SANAS" -ge 3 ]; then LOGRADO=1; break; fi
      ;;
    *)
      [ "$SANAS" -gt 0 ] && echo "  intento ${intento}: rebote, se reinicia el conteo"
      SANAS=0
      ;;
  esac
  sleep 10
done

MINUTOS=$(( (SECONDS - INICIO) / 60 ))

if [ "$LOGRADO" -ne 1 ]; then
  echo ""
  echo "FALLO: la sonda nunca respondio sana de forma sostenida en 600 s." >&2
  echo "Ultima respuesta: ${ULTIMA:-(ninguna)}" >&2
  echo "Duracion: ${MINUTOS} min" >&2
  echo ""
  echo "Para revertir:  ./scripts/desplegar.sh --revertir <commit sano anterior>" >&2
  exit 1
fi

for ruta in "/" "/health/live"; do
  if curl -fsS --max-time 30 "${RENDER_SERVICE_URL}${ruta}" > /dev/null 2>&1; then
    echo "  [ok]  ${ruta}"
  else
    echo "  [FALLA] ${ruta}" >&2
    exit 1
  fi
done

echo ""
echo "================================================================"
if [ "$ES_REVERSION" -eq 1 ]; then
  echo " Reversion completada y verificada"
else
  echo " Despliegue completado y verificado"
fi
echo "================================================================"
echo "  Commit ....... ${OBJETIVO:-HEAD de la rama}"
echo "  Duracion ..... ${MINUTOS} min"
echo ""
echo " Una migracion destructiva NO se revierte redesplegando: la imagen"
echo " vuelve atras, los datos no. La recuperacion pasa por los guiones de"
echo " respaldo, que exigen confirmacion explicita."
echo "================================================================"
