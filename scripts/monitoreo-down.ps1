# =============================================================================
#  Derriba el entorno de monitoreo.
#
#  Por omision conserva los datos: el historico de metricas es evidencia, y
#  borrarlo entre ensayos hace imposible comparar un antes y un despues.
#  -Purgar los elimina, y pide confirmacion porque es destructivo
#  (constitucion, Principio IV).
#
#  Uso:
#    .\scripts\monitoreo-down.ps1
#    .\scripts\monitoreo-down.ps1 -Purgar
# =============================================================================
[CmdletBinding()]
param(
    [switch]$Purgar
)

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot

# La aplicacion se derriba primero: si se quitara antes la red del monitoreo,
# quedaria un contenedor colgado de una red que ya no existe.
Write-Host "Deteniendo la aplicacion..." -ForegroundColor Cyan
docker compose -f "$raiz\docker-compose.yml" -f "$raiz\monitoring\docker-compose.api.yml" down 2>&1 | Out-Null

if ($Purgar) {
    Write-Host ""
    Write-Host "ATENCION: se van a eliminar los volumenes del monitoreo." -ForegroundColor Yellow
    Write-Host "  gsp_prometheus_datos   (historico de metricas)"
    Write-Host "  gsp_alertmanager_datos (silencios y estado de alarmas)"
    Write-Host "  gsp_grafana_datos      (estado de Grafana)"
    Write-Host "  gsp_loki_datos         (registros de la API)"
    Write-Host "  gsp_tempo_datos        (trazas de la API)"
    Write-Host "  gsp_alloy_datos        (posicion de lectura del recolector)"
    Write-Host ""
    Write-Host "Esto NO se puede deshacer." -ForegroundColor Yellow
    $respuesta = Read-Host "Escribe PURGAR para confirmar"
    if ($respuesta -ne "PURGAR") {
        Write-Host "Cancelado. No se borro nada." -ForegroundColor Green
        exit 0
    }
    docker compose -f "$raiz\monitoring\docker-compose.yml" down -v
    Write-Host "Entorno derribado y datos eliminados." -ForegroundColor Green
} else {
    docker compose -f "$raiz\monitoring\docker-compose.yml" down
    Write-Host "Entorno derribado. Los datos se conservan para el proximo arranque." -ForegroundColor Green
    Write-Host "Para eliminarlos tambien: .\scripts\monitoreo-down.ps1 -Purgar"
}
