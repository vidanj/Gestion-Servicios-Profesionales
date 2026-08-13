using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SistemaServicios.API.DTOs.Admin;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Integration;

/// <summary>
/// Factory especializada para AdminController.
/// Extiende CustomWebApplicationFactory reemplazando IBackupService con un mock
/// controlable para que las pruebas no dependan de que pg_dump esté instalado.
/// </summary>
public class AdminWebApplicationFactory : CustomWebApplicationFactory
{
    public Mock<IBackupService> BackupServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configura InMemory DB y variables de entorno (heredado)
        base.ConfigureWebHost(builder);

        _ = builder.ConfigureServices(services =>
        {
            // Reemplaza el BackupService real con el mock
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBackupService));
            if (descriptor != null)
            {
                _ = services.Remove(descriptor);
            }

            _ = services.AddScoped<IBackupService>(_ => BackupServiceMock.Object);
        });
    }
}

/// <summary>
/// Pruebas de integración del AdminController.
/// Cubren el pipeline completo: HTTP → middleware de autorización → controller → servicio (mock).
/// </summary>
public class AdminControllerTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Mock<IBackupService> _backupMock;

    // Clave JWT idéntica a la configurada en CustomWebApplicationFactory
    private const string TestJwtKey = "ClaveSecretaParaIntegracionTests_32Ch!";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    public AdminControllerTests(AdminWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _backupMock = factory.BackupServiceMock;
    }

    // ── Helper: genera un JWT firmado con la clave de prueba ─────────────────

    private static string GenerarToken(UserRole rol)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["JwtSettings:Key"] = TestJwtKey,
                    ["JwtSettings:Issuer"] = TestIssuer,
                    ["JwtSettings:Audience"] = TestAudience,
                    ["JwtSettings:ExpiresInMinutes"] = "60",
                }
            )
            .Build();

        var tokenService = new TokenService(config);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email =
                $"{rol.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}@test.com",
            PasswordHash = "hash-no-relevante",
            FirstName = rol.ToString(),
            LastName = "Test",
            Role = rol,
            Status = true,
        };

        return tokenService.CreateToken(user);
    }

    private static HttpRequestMessage BuildRequest(string method, string url, string? token = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (token != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/admin/backup — autorización
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBackupSinTokenRetorna401()
    {
        // Arrange
        var request = BuildRequest("POST", "/api/admin/backup");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateBackupConTokenDeClientRetorna403()
    {
        // Arrange: usuario autenticado pero sin el rol requerido
        var token = GenerarToken(UserRole.Client);
        var request = BuildRequest("POST", "/api/admin/backup", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert: autorizado como usuario pero sin permisos de Admin
        _ = response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateBackupConTokenDeProfessionalRetorna403()
    {
        // Arrange
        var token = GenerarToken(UserRole.Professional);
        var request = BuildRequest("POST", "/api/admin/backup", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateBackupConTokenMalformadoRetorna401()
    {
        // Arrange
        var request = BuildRequest("POST", "/api/admin/backup", "esto.no.es.un.jwt");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Espera a que el trabajo llegue a un estado final. El respaldo corre en segundo
    /// plano, así que consultar el estado justo tras aceptar la petición es una carrera.
    /// </summary>
    private async Task<BackupJobDto> EsperarEstadoFinal(Guid jobId, string token)
    {
        for (var intento = 0; intento < 50; intento++)
        {
            var res = await _client.SendAsync(
                BuildRequest("GET", $"/api/admin/backup/jobs/{jobId}", token)
            );
            var job = await res.Content.ReadFromJsonAsync<BackupJobDto>();

            if (job!.Status is BackupJobStatus.Completado or BackupJobStatus.Fallido)
            {
                return job;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("El trabajo de respaldo no terminó a tiempo.");
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/admin/backup — flujo de negocio (con token Admin)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBackupConTokenDeAdminRetorna202YNoEsperaAlVolcado()
    {
        // Arrange: el respaldo pasó a segundo plano (issue #126). Antes esta prueba
        // esperaba 201 con el archivo ya generado.
        _ = _backupMock
            .Setup(s => s.GenerateBackupAsync())
            .ReturnsAsync(
                new BackupResponseDto
                {
                    FileName = "backup_20260226_1200.sql",
                    CreatedAt = new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc),
                    FileSizeBytes = 20480,
                }
            );

        var token = GenerarToken(UserRole.Admin);
        var request = BuildRequest("POST", "/api/admin/backup", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert: la petición se acepta de inmediato
        _ = response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var job = await response.Content.ReadFromJsonAsync<BackupJobDto>();
        _ = job!.Id.Should().NotBeEmpty();

        // Y el trabajo se completa después, ya fuera de la petición: esto verifica
        // la tubería entera, incluido el consumidor en segundo plano.
        var final = await EsperarEstadoFinal(job.Id, token);
        _ = final.Status.Should().Be(BackupJobStatus.Completado);
        _ = final.FileName.Should().Be("backup_20260226_1200.sql");
        _ = final.FileSizeBytes.Should().Be(20480);
    }

    [Fact]
    public async Task CreateBackupConTokenDeAdminAceptaAunqueElVolcadoVayaAFallar()
    {
        // Arrange: el fallo ocurre en segundo plano, así que la petición se acepta igual.
        // Antes devolvía 500 porque esperaba a pg_dump dentro de la petición.
        _ = _backupMock
            .Setup(s => s.GenerateBackupAsync())
            .ThrowsAsync(
                new InvalidOperationException("pg_dump falló (código 1): autenticación fallida")
            );

        var token = GenerarToken(UserRole.Admin);
        var request = BuildRequest("POST", "/api/admin/backup", token);

        // Act
        var response = await _client.SendAsync(request);
        var contenido = await response.Content.ReadAsStringAsync();

        // Assert: nunca 500, y el detalle interno tampoco viaja al cliente
        _ = response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        _ = contenido.Should().NotContain("pg_dump");

        // El fallo queda registrado en el estado del trabajo, sin filtrar el detalle
        var job = await response.Content.ReadFromJsonAsync<BackupJobDto>();
        var final = await EsperarEstadoFinal(job!.Id, token);
        _ = final.Status.Should().Be(BackupJobStatus.Fallido);
        _ = final.Error.Should().NotContain("pg_dump");
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/admin/backup/jobs/{id} — estado del trabajo
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBackupJobSinTokenRetorna401()
    {
        // Arrange
        var request = BuildRequest("GET", $"/api/admin/backup/jobs/{Guid.NewGuid()}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBackupJobConTokenDeClientRetorna403()
    {
        // Arrange
        var token = GenerarToken(UserRole.Client);
        var request = BuildRequest("GET", $"/api/admin/backup/jobs/{Guid.NewGuid()}", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBackupJobConIdInexistenteRetorna404()
    {
        // Arrange
        var token = GenerarToken(UserRole.Admin);
        var request = BuildRequest("GET", $"/api/admin/backup/jobs/{Guid.NewGuid()}", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBackupJobDelTrabajoRecienCreadoRetorna200()
    {
        // Arrange
        _ = _backupMock
            .Setup(s => s.GenerateBackupAsync())
            .ReturnsAsync(new BackupResponseDto { FileName = "backup_20260226_1200.sql" });

        var token = GenerarToken(UserRole.Admin);
        var creado = await _client.SendAsync(BuildRequest("POST", "/api/admin/backup", token));
        var job = await creado.Content.ReadFromJsonAsync<BackupJobDto>();

        // Act
        var response = await _client.SendAsync(
            BuildRequest("GET", $"/api/admin/backup/jobs/{job!.Id}", token)
        );

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
        var consultado = await response.Content.ReadFromJsonAsync<BackupJobDto>();
        _ = consultado!.Id.Should().Be(job.Id);

        // Se drena el trabajo para no dejarlo activo y afectar a otras pruebas:
        // el guardia de concurrencia devolvería este mismo trabajo.
        _ = await EsperarEstadoFinal(job.Id, token);
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/admin/backups — listado
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListBackupsSinTokenRetorna401()
    {
        // Arrange
        var request = BuildRequest("GET", "/api/admin/backups");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListBackupsConTokenDeClientRetorna403()
    {
        // Arrange
        var token = GenerarToken(UserRole.Client);
        var request = BuildRequest("GET", "/api/admin/backups", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListBackupsConTokenDeAdminRetornaLaLista()
    {
        // Arrange
        _ = _backupMock
            .Setup(s => s.ListBackups())
            .Returns([
                new BackupResponseDto
                {
                    FileName = "backup_20260305_0900.sql",
                    CreatedAt = DateTime.UtcNow,
                    FileSizeBytes = 2048,
                },
            ]);

        var token = GenerarToken(UserRole.Admin);
        var request = BuildRequest("GET", "/api/admin/backups", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<BackupResponseDto>>();
        _ = body.Should().ContainSingle();
        _ = body![0].FileName.Should().Be("backup_20260305_0900.sql");
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/admin/backups/{fileName} — descarga
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DownloadBackupSinTokenRetorna401()
    {
        // Arrange
        var request = BuildRequest("GET", "/api/admin/backups/backup_20260305_0900.sql");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DownloadBackupConTokenDeClientRetorna403()
    {
        // Arrange
        var token = GenerarToken(UserRole.Client);
        var request = BuildRequest("GET", "/api/admin/backups/backup_20260305_0900.sql", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DownloadBackupConTokenDeAdminYArchivoExistenteDevuelveElContenido()
    {
        // Arrange
        var contenidoEsperado = "-- volcado de prueba"u8.ToArray();
        _ = _backupMock
            .Setup(s => s.OpenBackup("backup_20260305_0900.sql"))
            .Returns(() => new MemoryStream(contenidoEsperado));

        var token = GenerarToken(UserRole.Admin);
        var request = BuildRequest("GET", "/api/admin/backups/backup_20260305_0900.sql", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
        _ = response.Content.Headers.ContentType?.MediaType.Should().Be("application/octet-stream");
        var body = await response.Content.ReadAsStringAsync();
        _ = body.Should().Be("-- volcado de prueba");
    }

    [Fact]
    public async Task DownloadBackupCuandoElServicioRechazaElNombreRetorna404()
    {
        // Arrange: el servicio devuelve null tanto para nombre inválido como para
        // archivo inexistente; el controller debe traducir ambos al mismo 404.
        _ = _backupMock.Setup(s => s.OpenBackup(It.IsAny<string>())).Returns((Stream?)null);

        var token = GenerarToken(UserRole.Admin);
        var request = BuildRequest("GET", "/api/admin/backups/backup_20990101_0000.sql", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadBackupConPathTraversalNoAlcanzaArchivosFueraDelDirectorio()
    {
        // Arrange: el enrutamiento debe impedir que ".." salga del segmento, y aunque
        // llegara al servicio, OpenBackup lo rechazaría (probado en las unitarias).
        _ = _backupMock.Setup(s => s.OpenBackup(It.IsAny<string>())).Returns((Stream?)null);

        var token = GenerarToken(UserRole.Admin);
        var request = BuildRequest("GET", "/api/admin/backups/..%2F..%2Fappsettings.json", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert: en ningún caso 200
        _ = response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
