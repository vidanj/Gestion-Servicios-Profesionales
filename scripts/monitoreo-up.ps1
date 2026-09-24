# =============================================================================
#  Levanta el entorno de monitoreo y, opcionalmente, la aplicacion conectada a el.
#
#  El orden importa: el monitoreo primero, porque declara la red que la
#  superposicion de la aplicacion referencia como externa. Al reves, Docker falla
#  con "network gsp-observabilidad declared as external, but could not be found".
#
#  Uso:
#    .\scripts\monitoreo-up.ps1              # monitoreo + aplicacion
#    .\scripts\monitoreo-up.ps1 -SinApi      # solo el monitoreo
# =============================================================================
[CmdletBinding()]
param(
    [switch]$SinApi
)

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot

# --- Carga del .env, mismo patron que backup.ps1 ------------------------------
$env_file = Join-Path $raiz ".env"
if (Test-Path $env_file) {
    Get-Content $env_file | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]*)=(.*)$') {
            [Environment]::SetEnvironmentVariable($Matches[1].Trim(), $Matches[2].Trim())
        }
    }
}

# Se comprueba ANTES de arrancar: si falta, Grafana quedaria con la contrasena por
# defecto y esa instalacion acabaria en las capturas del documento.
if (-not $env:GRAFANA_ADMIN_PASSWORD) {
    Write-Host "ERROR: GRAFANA_ADMIN_PASSWORD no esta definida." -ForegroundColor Red
    Write-Host "Anadela al .env antes de levantar el entorno. Sin ella, Grafana"
    Write-Host "arrancaria con la contrasena por defecto."
    exit 1
}

docker version --format '{{.Server.Version}}' *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Docker no esta en marcha." -ForegroundColor Red
    exit 1
}

Write-Host "Levantando el entorno de monitoreo..." -ForegroundColor Cyan
docker compose -f "$raiz\monitoring\docker-compose.yml" up -d
if ($LASTEXITCODE -ne 0) { exit 1 }

function Esperar($nombre, $url) {
    for ($i = 1; $i -le 30; $i++) {
        try {
            $null = Invoke-WebRequest -Uri $url -TimeoutSec 5 -UseBasicParsing
            Write-Host ("  [ok]  {0}" -f $nombre) -ForegroundColor Green
            return $true
        } catch { Start-Sleep -Seconds 5 }
    }
    Write-Host ("  [FALLA] {0} no respondio en 150 s" -f $nombre) -ForegroundColor Red
    return $false
}

Write-Host "Esperando a que cada herramienta responda..." -ForegroundColor Cyan
$sanas = $true
$sanas = (Esperar "colector"     "http://localhost:13133")   -and $sanas
$sanas = (Esperar "prometheus"   "http://localhost:9090/-/ready")   -and $sanas
$sanas = (Esperar "alertmanager" "http://localhost:9093/-/ready")   -and $sanas
$sanas = (Esperar "grafana"      "http://localhost:3001/api/health") -and $sanas
# Spec 009. Loki tarda unos segundos mas que el resto en responder /ready: espera
# a que su anillo interno se estabilice antes de declararse listo.
$sanas = (Esperar "loki"         "http://localhost:3100/ready")      -and $sanas
$sanas = (Esperar "tempo"        "http://localhost:3200/ready")      -and $sanas
$sanas = (Esperar "alloy"        "http://localhost:12345/-/ready")   -and $sanas

if (-not $sanas) {
    Write-Host "El entorno no quedo sano. Revisa: docker compose -f monitoring/docker-compose.yml logs" -ForegroundColor Red
    exit 1
}

if (-not $SinApi) {
    Write-Host "Levantando la aplicacion conectada al colector..." -ForegroundColor Cyan
    docker compose -f "$raiz\docker-compose.yml" -f "$raiz\monitoring\docker-compose.api.yml" up -d --build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "La aplicacion no arranco; el monitoreo sigue en pie." -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "================================================================"
Write-Host " Entorno en marcha"
Write-Host "================================================================"
Write-Host "  Tableros (Grafana) .... http://localhost:3001   (admin / valor de GRAFANA_ADMIN_PASSWORD)"
Write-Host "  Prometheus ............ http://localhost:9090"
Write-Host "  Alarmas ............... http://localhost:9093"
Write-Host "  Sondeo externo ........ http://localhost:9115"
Write-Host "  Registros (Loki) ...... http://localhost:3100   (solo API; se consulta desde Grafana)"
Write-Host "  Trazas (Tempo) ........ http://localhost:3200   (solo API; se consulta desde Grafana)"
Write-Host "  Recolector (Alloy) .... http://localhost:12345  (diagnostico de la recoleccion)"
if (-not $SinApi) {
    Write-Host "  API (tras el proxy) ... http://localhost:8080"
}
Write-Host ""
Write-Host " Siguiente paso:"
Write-Host "   .\scripts\generar-trafico.ps1     # poblar los tableros"
Write-Host "   .\scripts\verificar-monitoreo.ps1 # comprobar el catalogo"
Write-Host "================================================================"
