$ErrorActionPreference = "Stop"

# Script simple para usar el SDK local instalado en ~/.dotnet
$repoRoot = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path $env:USERPROFILE ".dotnet"

if (-not (Test-Path $localDotnet)) {
    Write-Host "No se encontró $localDotnet" -ForegroundColor Red
    Write-Host "Instala primero el SDK local (9.0.201) y vuelve a intentar." -ForegroundColor Yellow
    exit 1
}

$env:DOTNET_ROOT = $localDotnet
$env:PATH = "$localDotnet;$env:PATH"

Write-Host "Usando DOTNET_ROOT=$env:DOTNET_ROOT" -ForegroundColor Cyan
Write-Host "SDKs detectados:" -ForegroundColor Cyan
& dotnet --list-sdks

Set-Location (Join-Path $repoRoot "backend")
Write-Host "`nEjecutando build de backend..." -ForegroundColor Cyan
& dotnet build "SistemaServicios.sln"

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nBuild falló." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "`nBuild completado correctamente." -ForegroundColor Green
