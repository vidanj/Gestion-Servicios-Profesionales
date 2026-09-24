# =============================================================================
#  Genera trafico representativo contra la API.
#
#  Sirve para poblar los tableros antes de tomar una captura. El trafico es
#  deliberadamente variado -correcto, no encontrado, credenciales malas, alta,
#  solicitud rechazada- porque si solo hubiera peticiones correctas, los paneles
#  de error quedarian vacios y no se podria comprobar que funcionan.
#
#  Uso:
#    .\scripts\generar-trafico.ps1
#    .\scripts\generar-trafico.ps1 -Url http://localhost:8080 -Duracion 60
# =============================================================================
[CmdletBinding()]
param(
    [string]$Url = "http://localhost:5000",
    [int]$Duracion = 30
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

try {
    $null = Invoke-WebRequest -Uri "$Url/health/live" -TimeoutSec 10 -UseBasicParsing
} catch {
    Write-Host "ERROR: la API no responde en $Url." -ForegroundColor Red
    exit 1
}

Write-Host "Generando trafico contra $Url durante $Duracion s..." -ForegroundColor Cyan

$correctas = 0; $noEncontradas = 0; $loginFallido = 0; $altas = 0; $solicitudes = 0

function Pedir($metodo, $ruta, $cuerpo, $token) {
    try {
        $cabeceras = @{}
        if ($token) { $cabeceras["Authorization"] = "Bearer $token" }
        if ($cuerpo) {
            $null = Invoke-WebRequest -Uri "$Url$ruta" -Method $metodo -Body $cuerpo `
                -ContentType "application/json" -Headers $cabeceras -TimeoutSec 10 -UseBasicParsing
        } else {
            $null = Invoke-WebRequest -Uri "$Url$ruta" -Method $metodo `
                -Headers $cabeceras -TimeoutSec 10 -UseBasicParsing
        }
        return $true
    } catch { return $false }
}

# El endpoint de solicitudes exige sesion iniciada. Sin este paso, las peticiones se
# quedan en un 401 del middleware y nunca llegan a la capa de servicios, de modo que
# gsp_solicitudes_creadas_total no se mueve y parece que la instrumentacion falla.
$marcaInicial = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$correoSesion = "trafico_sesion_$marcaInicial@ejemplo.com"
$token = $null
try {
    $alta = "{`"email`":`"$correoSesion`",`"password`":`"Clave123!`",`"firstName`":`"Trafico`",`"lastName`":`"Sesion`"}"
    $r = Invoke-RestMethod -Uri "$Url/api/Auth/register" -Method POST -Body $alta `
        -ContentType "application/json" -TimeoutSec 15
    $token = $r.token
    Write-Host "  sesion iniciada para el trafico autenticado" -ForegroundColor DarkGray
} catch {
    Write-Host "  aviso: no se pudo iniciar sesion; el trafico autenticado se omite" -ForegroundColor Yellow
}

$fin = (Get-Date).AddSeconds($Duracion)
while ((Get-Date) -lt $fin) {
    foreach ($ruta in @("/", "/health/ready", "/api/Categories", "/api/Services")) {
        if (Pedir "GET" $ruta $null $null) { $correctas++ }
    }

    # Ruta inexistente: alimenta aspnetcore_routing_match_attempts_total.
    if (-not (Pedir "GET" "/api/NoExiste" $null $null)) { $noEncontradas++ }

    # Inicio de sesion fallido: alimenta gsp_autenticacion_intentos_total.
    $login = '{"email":"inexistente@ejemplo.com","password":"NoImporta123!"}'
    if (-not (Pedir "POST" "/api/Auth/login" $login $null)) { $loginFallido++ }

    # Alta de usuario: alimenta gsp_usuarios_registrados_total.
    $marca = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $alta = "{`"email`":`"trafico_$marca@ejemplo.com`",`"password`":`"Clave123!`",`"firstName`":`"Trafico`",`"lastName`":`"Sintetico`"}"
    if (Pedir "POST" "/api/Auth/register" $alta $null) { $altas++ }

    # Solicitud sobre un servicio inexistente, CON sesion: alimenta el resultado
    # "servicio_no_disponible" de gsp_solicitudes_creadas_total. Sin el token la
    # peticion muere en el 401 y nunca llega a la capa de servicios.
    if ($token) {
        $solicitud = '{"serviceId":999999,"description":"trafico sintetico"}'
        if (-not (Pedir "POST" "/api/ServiceRequests" $solicitud $token)) { $solicitudes++ }
    }

    Start-Sleep -Seconds 1
}

Write-Host ""
Write-Host ("  Peticiones correctas .............. {0}" -f $correctas)
Write-Host ("  Rutas inexistentes ................ {0}" -f $noEncontradas)
Write-Host ("  Inicios de sesion fallidos ........ {0}" -f $loginFallido)
Write-Host ("  Altas de usuario .................. {0}" -f $altas)
Write-Host ("  Solicitudes rechazadas ............ {0}" -f $solicitudes)
Write-Host ""
Write-Host "Espera unos 30 s (dos ciclos de recoleccion) antes de verificar:"
Write-Host "  .\scripts\verificar-monitoreo.ps1"
