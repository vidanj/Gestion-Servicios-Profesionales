# =============================================================================
# backup.ps1 — Genera un respaldo de la base de datos PostgreSQL
# Uso:  .\scripts\backup.ps1
# =============================================================================
#Requires -Version 5.1
$ErrorActionPreference = "Stop"

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
# Prioridad 1: variables individuales
# Prioridad 2: parsear DB_CONNECTION (Host=;Port=;Database=;Username=;Password=)

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

# ── Directorio y nombre del archivo ──────────────────────────────────────
$BackupDir = Join-Path $RepoRoot "backups"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

$Timestamp  = Get-Date -Format "yyyyMMdd_HHmm"
$BackupFile = Join-Path $BackupDir "backup_$Timestamp.sql"

# ── Ejecutar pg_dump ──────────────────────────────────────────────────────
Write-Host "Generando respaldo de '$DbName' ($DbHost`:$DbPort)..."
Write-Host "Destino: $BackupFile"

& pg_dump `
    "--host=$DbHost" `
    "--port=$DbPort" `
    "--username=$DbUser" `
    "--dbname=$DbName" `
    "--format=plain" `
    "--no-owner" `
    "--no-acl" `
    "--file=$BackupFile"

if ($LASTEXITCODE -ne 0) {
    Write-Error "pg_dump fallo con codigo de salida $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Respaldo completado exitosamente: $BackupFile"
