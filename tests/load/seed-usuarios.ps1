<#
.SYNOPSIS
    Siembra usuarios de prueba para la prueba de carga del listado paginado.

.DESCRIPTION
    La base de desarrollo tiene un puñado de usuarios. Medir GET /api/Users con
    esa cantidad no dice nada: la consulta filtra por Status, ordena por fecha y
    proyecta la pagina a DTO, y con dos filas ninguno de esos costos se nota.
    El plan (seccion 5) exige ademas un volumen semejante entre integrantes para
    que las mediciones sean comparables.

    Los usuarios se crean por la propia API (POST /api/Users, de rol Admin), no
    con INSERT directo: asi pasan por la validacion y el hash de la aplicacion y
    no se toca la base a mano.

    Es idempotente: si el correo ya existe, la API responde 400 y el script lo
    cuenta como omitido en lugar de fallar.

    La contraseña de los usuarios sembrados se genera al azar en cada ejecucion
    y no se guarda: estas cuentas solo existen como filas del listado, nadie
    inicia sesion con ellas. Asi no queda ninguna credencial en el repositorio.

.EXAMPLE
    ./tests/load/seed-usuarios.ps1 -AdminUser admin@ejemplo.com -AdminPass ****
#>
[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:5000',

    [Parameter(Mandatory = $true)]
    [string]$AdminUser,

    [Parameter(Mandatory = $true)]
    [string]$AdminPass,

    [int]$Cantidad = 200,

    [string]$Dominio = 'ejemplo.local'
)

$ErrorActionPreference = 'Stop'

Write-Host "Iniciando sesion en $BaseUrl ..."
$respuestaLogin = Invoke-RestMethod -Uri "$BaseUrl/api/Auth/login" -Method Post `
    -ContentType 'application/json' `
    -Body (@{ email = $AdminUser; password = $AdminPass } | ConvertTo-Json)

$token = $respuestaLogin.token
if (-not $token) {
    throw 'El inicio de sesion no devolvio token.'
}

$cabeceras = @{ Authorization = "Bearer $token" }
$creados = 0
$omitidos = 0

Write-Host "Sembrando $Cantidad usuarios ..."
for ($i = 1; $i -le $Cantidad; $i++) {
    $etiqueta = '{0:D4}' -f $i
    $cuerpo = @{
        email     = "carga-$etiqueta@$Dominio"
        password  = [System.Guid]::NewGuid().ToString('N') + 'aA1!'
        firstName = "Carga$etiqueta"
        lastName  = 'Prueba'
        role      = 1  # Client
    } | ConvertTo-Json

    try {
        Invoke-RestMethod -Uri "$BaseUrl/api/Users" -Method Post -Headers $cabeceras `
            -ContentType 'application/json' -Body $cuerpo | Out-Null
        $creados++
    }
    catch {
        $codigo = $_.Exception.Response.StatusCode.value__
        if ($codigo -eq 400) {
            # La API valida unicidad de correo: ya existia de una siembra previa.
            $omitidos++
        }
        else {
            throw
        }
    }

    if ($i % 25 -eq 0) {
        Write-Host "  $i de $Cantidad ..."
    }
}

Write-Host ""
Write-Host "Creados:  $creados"
Write-Host "Omitidos: $omitidos (ya existian)"
Write-Host ""
Write-Host "Total de usuarios activos en la base (lo que vera el listado):"
$pagina = Invoke-RestMethod -Uri "$BaseUrl/api/Users?page=1&size=1" -Headers $cabeceras
Write-Host "  $($pagina.total)"
