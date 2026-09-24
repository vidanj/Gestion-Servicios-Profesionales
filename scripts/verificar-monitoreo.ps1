# =============================================================================
#  Verifica que el entorno de monitoreo esta completo.
#
#  Recorre el catalogo de metricas y pregunta por cada una a Prometheus; comprueba
#  que las 10 alarmas estan cargadas y que Grafana responde. Desde la spec 009
#  comprueba tambien la trazabilidad: que hay registros con nivel y TraceId, que
#  ese TraceId existe como traza y que ningun registro contiene las contrasenas
#  del trafico sintetico. Informa que falta y termina con codigo distinto de cero.
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
    [string]$Loki       = "http://localhost:3100",
    [string]$Tempo      = "http://localhost:3200",
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

# --- Trazabilidad (spec 009) --------------------------------------------------
# Registros en Loki, trazas en Tempo y el enlace entre ambos. Las comprobaciones
# dependen unas de otras: si no hay registros, las de nivel, etiquetas, TraceId y
# contrasenas se omiten en lugar de pasar en vacio. "Ninguna linea contiene una
# contrasena" es verdad trivialmente cuando no hay ninguna linea.

$tfallos = 0
$hayRegistros = $false

# Deben coincidir con las de scripts\generar-trafico.ps1. Si alli cambian, aqui
# tambien: si no, se buscaria una cadena que ya nadie envia y la comprobacion
# pasaria siempre.
$contrasenasSinteticas = @("Clave123!", "NoImporta123!")

function Bien([string]$texto, [string]$detalle = "") {
    Write-Host ("  [ok]   {0,-52} {1}" -f $texto, $detalle) -ForegroundColor Green
}

# $script: porque dentro de una funcion `$fallos++` crearia una copia local y el
# total del guion no cambiaria.
function Falta([string]$texto, [string]$causa = "") {
    Write-Host ("  [FALTA] {0,-52}" -f $texto) -ForegroundColor Red
    if ($causa) { Write-Host ("          {0}" -f $causa) -ForegroundColor Red }
    $script:fallos++
    $script:tfallos++
}

# Cuenta las lineas de la ultima hora que cumplen un selector LogQL. Consulta de
# metrica y no de lineas: la de lineas tiene tope y "1000" podria ser mil o cien mil.
function Contar([string]$selector) {
    try {
        $r = Invoke-RestMethod -Uri "$Loki/loki/api/v1/query" `
            -Body @{ query = "sum(count_over_time($selector [1h]))" } -TimeoutSec 15
        $res = @($r.data.result)
        if ($res.Count -eq 0) { return 0 }
        return [long]$res[0].value[1]
    } catch { return 0 }
}

Write-Host ""
Write-Host "Trazabilidad: registros y trazas"

$lokiArriba = $false
try {
    $null = Invoke-WebRequest -Uri "$Loki/ready" -TimeoutSec 10 -UseBasicParsing
    $lokiArriba = $true
} catch { }
if ($lokiArriba) { Bien "almacen de registros (Loki) responde" }
else { Falta "almacen de registros (Loki) no responde en $Loki" }

$tempoArriba = $false
try {
    $null = Invoke-WebRequest -Uri "$Tempo/ready" -TimeoutSec 10 -UseBasicParsing
    $tempoArriba = $true
} catch { }
if ($tempoArriba) { Bien "almacen de trazas (Tempo) responde" }
else { Falta "almacen de trazas (Tempo) no responde en $Tempo" }

if ($lokiArriba) {
    $n = Contar '{servicio="api"}'
    if ($n -gt 0) {
        Bien "registros de la API" "$n lineas"
        $hayRegistros = $true
    } else {
        Falta "registros de la API" "Alloy no entrega nada a Loki; diagnostico en http://localhost:12345"
    }
}

if ($hayRegistros) {
    # D4: el formato compacto omite @l en Information y Alloy lo rellena. Si falta,
    # el relleno no funciona o los campos no se llaman como supone T002.
    $n = Contar '{servicio="api", nivel="Information"}'
    if ($n -gt 0) { Bien "nivel Information (rellenado, D4)" "$n lineas" }
    else { Falta "nivel Information (rellenado, D4)" "revisar @t y @l en config.alloy contra una linea real (T002)" }

    # Los inicios de sesion fallidos del trafico sintetico se registran como
    # Warning. Esto prueba que @l se extrae de verdad, no solo que el relleno opera.
    $n = Contar '{servicio="api", nivel="Warning"}'
    if ($n -gt 0) { Bien "nivel Warning (extraido de @l)" "$n lineas" }
    else { Falta "nivel Warning (extraido de @l)" "sin inicios de sesion fallidos registrados, o @l no se extrae" }

    # FR-004 y FR-005: solo `servicio` y `nivel` son etiquetas. Las que empiezan
    # por __ son internas de Loki y no cuentan.
    try {
        $r = Invoke-RestMethod -Uri "$Loki/loki/api/v1/labels" -Body @{ since = "1h" } -TimeoutSec 10
        $extra = @($r.data | Where-Object { -not $_.StartsWith("__") -and $_ -ne "servicio" -and $_ -ne "nivel" })
        if ($extra.Count -eq 0) { Bien "solo servicio y nivel como etiquetas (FR-005)" }
        else { Falta ("etiquetas de mas en Loki: " + ($extra -join ", ")) "TraceId, contenedor o service_name no pueden ser etiquetas (FR-004, FR-005)" }
    } catch {
        Falta "lista de etiquetas de Loki" "no se pudo consultar"
    }

    $n = Contar '{servicio="api"} | TraceId != ""'
    if ($n -gt 0) { Bien "TraceId como metadato consultable (FR-004)" "$n lineas" }
    else { Falta "TraceId como metadato consultable (FR-004)" "el campo no se llama TraceId en el JSON, o Alloy no lo extrae (T002)" }

    if ($tempoArriba) {
        # Se muestrea de lineas Warning y no de cualquiera: las rutas /health pueden
        # llevar TraceId en el registro, pero la instrumentacion las excluye de las
        # trazas a proposito. Muestrear de ahi daria un fallo que no es fallo.
        # El identificador se toma del metadato y, si no viene ahi, del JSON de la
        # linea: lo que se prueba es el enlace, no la forma de la respuesta de Loki.
        $ids = @()
        try {
            $r = Invoke-RestMethod -Uri "$Loki/loki/api/v1/query_range" -TimeoutSec 15 -Body @{
                query = '{servicio="api", nivel="Warning"} | TraceId != ""'
                since = "1h"
                limit = 50
            }
            foreach ($s in @($r.data.result)) {
                if ($s.stream.TraceId) { $ids += [string]$s.stream.TraceId }
                foreach ($v in @($s.values)) {
                    try {
                        $j = [string]$v[1] | ConvertFrom-Json
                        if ($j.TraceId) { $ids += [string]$j.TraceId }
                    } catch { }
                }
            }
        } catch { }
        $ids = @($ids | Select-Object -Unique | Select-Object -First 3)

        if ($ids.Count -eq 0) {
            Falta "muestra de TraceId para buscar en Tempo" "no hay lineas Warning con TraceId"
        } else {
            foreach ($id in $ids) {
                # Hasta 30 s: el colector agrupa cada 10 s y Tempo tarda un poco mas
                # en volver consultable lo recien recibido.
                $encontrada = $false
                for ($i = 1; $i -le 6 -and -not $encontrada; $i++) {
                    try {
                        $null = Invoke-WebRequest -Uri "$Tempo/api/traces/$id" -TimeoutSec 10 -UseBasicParsing
                        $encontrada = $true
                    } catch { Start-Sleep -Seconds 5 }
                }
                if ($encontrada) { Bien "traza $id" "existe en Tempo" }
                else { Falta "traza $id" "el TraceId del registro no existe como traza: formato distinto o traza no exportada" }
            }
        }
    }

    foreach ($clave in $contrasenasSinteticas) {
        # |= es busqueda literal, no expresion regular: el ! no necesita escaparse.
        $n = Contar ('{servicio="api"} |= "' + $clave + '"')
        if ($n -eq 0) { Bien ("ninguna linea contiene la contrasena {0}***" -f $clave.Substring(0, 3)) }
        else { Falta "$n linea(s) contienen una contrasena sintetica" "la redaccion de secretos no cubre algun campo" }
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

if (($fallos - $tfallos) -gt 0) {
    Write-Host ""
    Write-Host " Metricas. Causas habituales, en orden de probabilidad:"
    Write-Host "   1. Todavia no se ha generado trafico:  .\scripts\generar-trafico.ps1"
    Write-Host "   2. Aun no han llegado: la aplicacion exporta cada 60 s por omision"
    Write-Host "      (OTEL_METRIC_EXPORT_INTERVAL) y Prometheus recoge cada 15 s."
    Write-Host "   3. La API no tiene OTEL_EXPORTER_OTLP_ENDPOINT definido."
    Write-Host "   4. El nombre de la metrica cambio de version (ver data-model.md)."
}

if ($tfallos -gt 0) {
    Write-Host ""
    Write-Host " Trazabilidad. Causas habituales, en orden de probabilidad:"
    Write-Host "   1. No se ha generado trafico en la ultima hora."
    Write-Host "   2. Alloy no selecciona el contenedor de la API: debe llevar la etiqueta"
    Write-Host "      com.docker.compose.service=api. Diagnostico en http://localhost:12345"
    Write-Host "   3. Los campos del JSON no se llaman @t, @l y TraceId (tarea T002)."
    Write-Host "   4. El colector no reenvia a Tempo:  docker logs gsp-otel-collector"
}
Write-Host "================================================================"
exit 1
