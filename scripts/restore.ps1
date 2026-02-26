# =============================================================================
# restore.ps1 — Restaura la base de datos PostgreSQL desde un archivo .sql
# Uso:  .\scripts\restore.ps1 -BackupFile <ruta\al\backup.sql>
# =============================================================================
#Requires -Version 5.1
param(
    [Parameter(Mandatory = $true, HelpMessage = "Ruta al archivo .sql de respaldo")]
    [string]$BackupFile
)
$ErrorActionPreference = "Stop"

# ── Validar archivo ────────────────────────────────────────────────────────
if (-not (Test-Path $BackupFile)) {
    Write-Error "ERROR: No se encontro el archivo: $BackupFile"
    exit 1
}
$BackupFile = (Resolve-Path $BackupFile).Path

# ── Rutas ─────────────────────────────────────────────────────────────────
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot  = Split-Path -Parent $ScriptDir
$EnvFile   = Join-Path $RepoRoot ".env"

# ── Cargar variables del archivo .env ─────────────────────────────────────
if (Test-Path $EnvFile) {
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match '^\s*([^#=][^=]*)=(.+)$') {
            $key   = $Matches[1].Trim()
            $value = $Matches[2].Trim()
            [System.Environment]::SetEnvironmentVariable($key, $value, "Process")
        }
    }
}

# ── Resolver credenciales ──────────────────────────────────────────────────
$DbHost = $env:DB_HOST
$DbPort = if ($env:DB_PORT) { $env:DB_PORT } else { "5432" }
$DbName = $env:DB_NAME
$DbUser = $env:DB_USER
$DbPass = $env:DB_PASSWORD

if (-not $DbHost) {
    $conn = $env:DB_CONNECTION
    if (-not $conn) {
        Write-Error "ERROR: No se encontraron variables de conexion.`n" +
                    "       Define DB_HOST / DB_PORT / DB_NAME / DB_USER / DB_PASSWORD`n" +
                    "       o DB_CONNECTION en el archivo .env"
        exit 1
    }
    $DbHost = [regex]::Match($conn, '(?i)Host=([^;]+)').Groups[1].Value
    $portM  = [regex]::Match($conn, '(?i)Port=([^;]+)')
    $DbPort = if ($portM.Success) { $portM.Groups[1].Value } else { "5432" }
    $DbName = [regex]::Match($conn, '(?i)Database=([^;]+)').Groups[1].Value
    $DbUser = [regex]::Match($conn, '(?i)Username=([^;]+)').Groups[1].Value
    $DbPass = [regex]::Match($conn, '(?i)Password=([^;]+)').Groups[1].Value
}

$env:PGPASSWORD = $DbPass

# ── Confirmacion de seguridad ─────────────────────────────────────────────
Write-Host "==========================================================="
Write-Host "  RESTAURACION DE BASE DE DATOS"
Write-Host "==========================================================="
Write-Host "  Base de datos : $DbName"
Write-Host "  Servidor      : $DbHost`:$DbPort"
Write-Host "  Archivo       : $BackupFile"
Write-Host "==========================================================="
Write-Host ""
Write-Warning "Esta operacion sobreescribira los datos actuales."
$confirm = Read-Host "¿Desea continuar? (escriba 's' para confirmar)"

if ($confirm.ToLower() -ne "s") {
    Write-Host "Operacion cancelada."
    exit 0
}

# ── Ejecutar psql ─────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Restaurando '$DbName' desde '$BackupFile'..."

& psql `
    "--host=$DbHost" `
    "--port=$DbPort" `
    "--username=$DbUser" `
    "--dbname=$DbName" `
    "--file=$BackupFile"

if ($LASTEXITCODE -ne 0) {
    Write-Error "psql fallo con codigo de salida $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Restauracion completada exitosamente desde: $BackupFile"
