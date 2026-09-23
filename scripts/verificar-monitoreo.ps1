# =============================================================================
#  Verifica que el entorno de monitoreo esta completo.
#
#  Recorre el catalogo de metricas y pregunta por cada una a Prometheus; comprueba
#  que las 10 alarmas estan cargadas y que Grafana responde. Informa que falta y
#  termina con codigo distinto de cero.
#
#  Es el equivalente exacto de verificar-monitoreo.sh, que es el que ejecuta la
#  integracion continua: no hay dos definiciones de "el monitoreo esta bien".
#
#  Uso:
#    .\scripts\verificar-monitoreo.ps1
#    .\scripts\verificar-monitoreo.ps1 -Prometheus http://localhost:9090
# =============================================================================
[CmdletBinding()]
param(
    [string]$Prometheus = "http://localhost:9090",
    [string]$Grafana    = "http://localhost:3001",
    [switch]$SinGrafana
)

$ErrorActionPreference = "Stop"

# Esta lista es la especificacion ejecutable del catalogo de data-model.md.
# Anadir una metrica alli sin anadirla aqui hace que su ausencia pase inadvertida.
$catalogo = [ordered]@{
    "Peticiones y servidor" = @(
        "http_server_request_duration_seconds_count",
        "http_server_request_duration_seconds_bucket",
        "http_server_active_requests",
        "kestrel_active_connections",
        "aspnetcore_routing_match_attempts_total"
    )
    # En .NET 9 los nombres son dotnet_*, no process_runtime_dotnet_*.
    "Tiempo de ejecucion (.NET 9)" = @(
        "dotnet_process_memory_working_set_bytes",
        "dotnet_gc_collections_total",
        "dotnet_thread_pool_queue_length_total",
        "dotnet_exceptions_total"
    )
    "Negocio" = @(
        "gsp_autenticacion_intentos_total",
        "gsp_usuarios_registrados_total",
        "gsp_solicitudes_creadas_total"
    )
    "Sondeo externo" = @(
        "probe_success",
        "probe_duration_seconds"
    )
    "Reglas de registro" = @(
        "gsp:http_peticiones:tasa5m",
        "gsp:http_proporcion_error:5m",
        "gsp:http_latencia:p95_5m"
    )
}

$alertas = @(
    "ApiNoDisponible", "BaseDeDatosInalcanzable", "ApiSinMetricas",
    "TasaDeErrorServidorAlta", "LatenciaP95Degradada", "LatenciaP95FueraDelAcuerdo",
    "MemoriaCercaDelLimite", "ColaDeHilosCreciente",
    "IntentosDeAutenticacionFallidos", "RespaldoFallido"
)

$fallos = 0

Write-Host "================================================================"
Write-Host " Verificacion del entorno de monitoreo"
Write-Host " Prometheus: $Prometheus"
Write-Host "================================================================"

try {
    $null = Invoke-WebRequest -Uri "$Prometheus/-/ready" -TimeoutSec 10 -UseBasicParsing
} catch {
    Write-Host "ERROR: Prometheus no responde en $Prometheus." -ForegroundColor Red
    Write-Host "Levanta el entorno con .\scripts\monitoreo-up.ps1"
    exit 1
}

foreach ($grupo in $catalogo.Keys) {
    Write-Host ""
    Write-Host $grupo
    foreach ($metrica in $catalogo[$grupo]) {
        $n = 0
        try {
            $r = Invoke-RestMethod -Uri "$Prometheus/api/v1/query" -Body @{ query = $metrica } -TimeoutSec 15
            $n = @($r.data.result).Count
        } catch { $n = 0 }

        if ($n -gt 0) {
            Write-Host ("  [ok]   {0,-52} {1} series" -f $metrica, $n) -ForegroundColor Green
        } else {
            Write-Host ("  [FALTA] {0,-52}" -f $metrica) -ForegroundColor Red
            $fallos++
        }
    }
}

Write-Host ""
Write-Host "Reglas de alerta"
$cargadas = @()
try {
    $r = Invoke-RestMethod -Uri "$Prometheus/api/v1/rules" -TimeoutSec 15
    $cargadas = $r.data.groups.rules | Where-Object { $_.type -eq "alerting" } | ForEach-Object { $_.name }
} catch { }

foreach ($alerta in $alertas) {
    if ($cargadas -contains $alerta) {
        Write-Host ("  [ok]   {0,-52}" -f $alerta) -ForegroundColor Green
    } else {
        Write-Host ("  [FALTA] {0,-52}" -f $alerta) -ForegroundColor Red
        $fallos++
    }
}

if (-not $SinGrafana) {
    Write-Host ""
    Write-Host "Grafana"
    try {
        $null = Invoke-WebRequest -Uri "$Grafana/api/health" -TimeoutSec 10 -UseBasicParsing
        Write-Host ("  [ok]   responde en {0}" -f $Grafana) -ForegroundColor Green
    } catch {
        Write-Host ("  [FALTA] no responde en {0}" -f $Grafana) -ForegroundColor Red
        $fallos++
    }
}

Write-Host ""
Write-Host "================================================================"
if ($fallos -eq 0) {
    Write-Host " Catalogo completo. El monitoreo esta operativo." -ForegroundColor Green
    Write-Host "================================================================"
    exit 0
}

Write-Host " Faltan $fallos elemento(s)." -ForegroundColor Red
Write-Host ""
Write-Host " Causas habituales, en orden de probabilidad:"
Write-Host "   1. Todavia no se ha generado trafico:  .\scripts\generar-trafico.ps1"
Write-Host "   2. No han pasado dos ciclos de recoleccion (30 s)."
Write-Host "   3. La API no tiene OTEL_EXPORTER_OTLP_ENDPOINT definido."
Write-Host "   4. El nombre de la metrica cambio de version (ver data-model.md)."
Write-Host "================================================================"
exit 1
