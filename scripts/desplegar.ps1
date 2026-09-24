# =============================================================================
#  Despliega y verifica, o revierte a un commit anterior.
#
#  Es el equivalente local exacto de .github/workflows/deploy.yml: mismo disparo,
#  misma espera, mismo criterio de exito. Existe para poder ejecutar y depurar el
#  despliegue sin pasar por la interfaz de Actions.
#
#  Revertir NO es otra cosa: es desplegar el commit sano anterior. Por eso el
#  camino de reversion se ejercita cada vez que se despliega, en lugar de ser una
#  ruta que nadie ha recorrido hasta el dia del incidente.
#
#  Variables requeridas (del entorno o del .env; nunca escritas aqui):
#    RENDER_DEPLOY_HOOK_URL   secreto del disparador
#    RENDER_SERVICE_URL       direccion publica a sondear
#
#  Uso:
#    .\scripts\desplegar.ps1
#    .\scripts\desplegar.ps1 -Sha a1b2c3d
#    .\scripts\desplegar.ps1 -Revertir a1b2c3d
# =============================================================================
[CmdletBinding()]
param(
    [string]$Sha,
    [string]$Revertir
)

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot

$env_file = Join-Path $raiz ".env"
if (Test-Path $env_file) {
    Get-Content $env_file | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]*)=(.*)$') {
            [Environment]::SetEnvironmentVariable($Matches[1].Trim(), $Matches[2].Trim())
        }
    }
}

if (-not $env:RENDER_DEPLOY_HOOK_URL) {
    Write-Host "ERROR: falta RENDER_DEPLOY_HOOK_URL." -ForegroundColor Red
    Write-Host "Obtenlo en el panel del servicio (Settings -> Deploy Hook)."
    exit 1
}
if (-not $env:RENDER_SERVICE_URL) {
    Write-Host "ERROR: falta RENDER_SERVICE_URL (direccion publica a sondear)." -ForegroundColor Red
    exit 1
}

# -Revertir es solo un alias legible de -Sha: subraya en el historial de la consola
# que esto fue una reversion, sin que el mecanismo sea distinto.
$objetivo = if ($Revertir) { $Revertir } elseif ($Sha) { $Sha } else { "" }
$esReversion = [bool]$Revertir

if ($esReversion) {
    Write-Host "REVERSION al commit $objetivo" -ForegroundColor Yellow
} elseif ($objetivo) {
    Write-Host "Despliegue del commit $objetivo" -ForegroundColor Cyan
} else {
    Write-Host "Despliegue del ultimo commit de la rama de integracion" -ForegroundColor Cyan
}

$inicio = Get-Date

# --- Disparo ------------------------------------------------------------------
$url = $env:RENDER_DEPLOY_HOOK_URL
if ($objetivo) { $url = "$url&ref=$objetivo" }

try {
    $null = Invoke-WebRequest -Uri $url -Method POST -TimeoutSec 30 -UseBasicParsing
    Write-Host "Despliegue solicitado." -ForegroundColor Green
} catch {
    # No se imprime la excepcion completa: podria incluir la URL, que lleva la clave.
    Write-Host "ERROR: el disparador respondio con error." -ForegroundColor Red
    exit 1
}

# --- Espera inicial -----------------------------------------------------------
# Sin esta pausa se sondearia la instancia ANTERIOR, que responde sana, y el
# despliegue se daria por bueno sin haber ocurrido.
Write-Host "Pausa de 30 s antes del primer sondeo..."
Start-Sleep -Seconds 30

# --- Verificacion -------------------------------------------------------------
$sondaUrl = "$($env:RENDER_SERVICE_URL)/health/ready"
$sanas = 0
$ultima = ""
$logrado = $false

Write-Host "Sondeando $sondaUrl (hasta 600 s, exigiendo 3 respuestas sanas seguidas)..."
for ($i = 1; $i -le 60; $i++) {
    try {
        $r = Invoke-WebRequest -Uri $sondaUrl -TimeoutSec 30 -UseBasicParsing
        $ultima = $r.Content
        if ($ultima -match '"status"\s*:\s*"Healthy"') {
            $sanas++
            Write-Host ("  intento {0}: sana ({1}/3)" -f $i, $sanas) -ForegroundColor Green
            # Tres seguidas: durante un reinicio el servicio rebota y puede
            # responder sano un instante antes de volver a caer.
            if ($sanas -ge 3) { $logrado = $true; break }
        } else {
            if ($sanas -gt 0) { Write-Host ("  intento {0}: rebote, se reinicia el conteo" -f $i) -ForegroundColor Yellow }
            $sanas = 0
        }
    } catch {
        if ($sanas -gt 0) { Write-Host ("  intento {0}: rebote, se reinicia el conteo" -f $i) -ForegroundColor Yellow }
        $sanas = 0
    }
    Start-Sleep -Seconds 10
}

$duracion = (Get-Date) - $inicio

if (-not $logrado) {
    Write-Host ""
    Write-Host "FALLO: la sonda nunca respondio sana de forma sostenida en 600 s." -ForegroundColor Red
    Write-Host "Ultima respuesta: $ultima"
    Write-Host "Duracion: $([math]::Round($duracion.TotalMinutes, 1)) min"
    Write-Host ""
    Write-Host "Para revertir:  .\scripts\desplegar.ps1 -Revertir <commit sano anterior>"
    exit 1
}

# --- Humo ---------------------------------------------------------------------
foreach ($ruta in @("/", "/health/live")) {
    try {
        $null = Invoke-WebRequest -Uri "$($env:RENDER_SERVICE_URL)$ruta" -TimeoutSec 30 -UseBasicParsing
        Write-Host ("  [ok]  {0}" -f $ruta) -ForegroundColor Green
    } catch {
        Write-Host ("  [FALLA] {0}" -f $ruta) -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "================================================================"
if ($esReversion) {
    Write-Host " Reversion completada y verificada"
} else {
    Write-Host " Despliegue completado y verificado"
}
Write-Host "================================================================"
Write-Host ("  Commit ....... {0}" -f $(if ($objetivo) { $objetivo } else { "HEAD de la rama" }))
Write-Host ("  Duracion ..... {0} min" -f [math]::Round($duracion.TotalMinutes, 1))
Write-Host ""
Write-Host " Una migracion destructiva NO se revierte redesplegando: la imagen" -ForegroundColor Yellow
Write-Host " vuelve atras, los datos no. La recuperacion pasa por los guiones de" -ForegroundColor Yellow
Write-Host " respaldo, que exigen confirmacion explicita." -ForegroundColor Yellow
Write-Host "================================================================"
