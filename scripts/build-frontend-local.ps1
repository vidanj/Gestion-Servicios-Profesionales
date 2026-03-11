$ErrorActionPreference = "Stop"

# Script simple para ejecutar build del frontend desde la raíz del repo
$repoRoot = Split-Path -Parent $PSScriptRoot
$frontendPath = Join-Path $repoRoot "frontend"

if (-not (Test-Path $frontendPath)) {
    Write-Host "No se encontró la carpeta frontend en $frontendPath" -ForegroundColor Red
    exit 1
}

Set-Location $frontendPath

Write-Host "Ejecutando build de frontend..." -ForegroundColor Cyan
& npm run build

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nBuild de frontend falló." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "`nBuild de frontend completado correctamente." -ForegroundColor Green
