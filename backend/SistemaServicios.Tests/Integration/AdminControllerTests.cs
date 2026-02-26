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

        builder.ConfigureServices(services =>
        {
            // Reemplaza el BackupService real con el mock
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IBackupService));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddScoped<IBackupService>(_ => BackupServiceMock.Object);
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
    private const string TestJwtKey    = "ClaveSecretaParaIntegracionTests_32Ch!";
    private const string TestIssuer    = "TestIssuer";
    private const string TestAudience  = "TestAudience";

    public AdminControllerTests(AdminWebApplicationFactory factory)
    {
        _client     = factory.CreateClient();
        _backupMock = factory.BackupServiceMock;
    }

    // ── Helper: genera un JWT firmado con la clave de prueba ─────────────────

    private static string GenerarToken(UserRole rol)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"]              = TestJwtKey,
                ["JwtSettings:Issuer"]           = TestIssuer,
                ["JwtSettings:Audience"]         = TestAudience,
                ["JwtSettings:ExpiresInMinutes"] = "60",
            })
            .Build();

        var tokenService = new TokenService(config);

        var user = new User
        {
            Id           = Guid.NewGuid(),
            Email        = $"{rol.ToString().ToLower()}@test.com",
            PasswordHash = "hash-no-relevante",
            FirstName    = rol.ToString(),
            LastName     = "Test",
            Role         = rol,
            Status       = true,
        };

        return tokenService.CreateToken(user);
    }

    private static HttpRequestMessage BuildRequest(string method, string url, string? token = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/admin/backup — autorización
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBackup_SinToken_Retorna401()
    {
        // Arrange
        var request = BuildRequest("POST", "/api/admin/backup");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateBackup_ConTokenDeClient_Retorna403()
    {
        // Arrange: usuario autenticado pero sin el rol requerido
        var token = GenerarToken(UserRole.Client);
        var request = BuildRequest("POST", "/api/admin/backup", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert: autorizado como usuario pero sin permisos de Admin
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateBackup_ConTokenDeProfessional_Retorna403()
    {
        // Arrange
        var token = GenerarToken(UserRole.Professional);
        var request = BuildRequest("POST", "/api/admin/backup", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateBackup_ConTokenMalformado_Retorna401()
    {
        // Arrange
        var request = BuildRequest("POST", "/api/admin/backup", "esto.no.es.un.jwt");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/admin/backup — flujo de negocio (con token Admin)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBackup_ConTokenDeAdmin_BackupExitoso_Retorna201()
    {
        // Arrange
        var backupEsperado = new BackupResponseDto
        {
            FileName      = "backup_20260226_1200.sql",
            CreatedAt     = new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc),
            FileSizeBytes = 20480,
        };

        _backupMock.Setup(s => s.GenerateBackupAsync())
            .ReturnsAsync(backupEsperado);

        var token   = GenerarToken(UserRole.Admin);
        var request = BuildRequest("POST", "/api/admin/backup", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<BackupResponseDto>();
        body!.FileName.Should().Be("backup_20260226_1200.sql");
        body.FileSizeBytes.Should().Be(20480);
    }

    [Fact]
    public async Task CreateBackup_ConTokenDeAdmin_BackupExitoso_RetornaJsonConFileName()
    {
        // Arrange
        _backupMock.Setup(s => s.GenerateBackupAsync())
            .ReturnsAsync(new BackupResponseDto
            {
                FileName      = "backup_20260226_1430.sql",
                CreatedAt     = DateTime.UtcNow,
                FileSizeBytes = 1024,
            });

        var token   = GenerarToken(UserRole.Admin);
        var request = BuildRequest("POST", "/api/admin/backup", token);

        // Act
        var response = await _client.SendAsync(request);
        var contenido = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        contenido.Should().Contain("backup_20260226_1430.sql");
        contenido.Should().Contain("fileSizeBytes");
        contenido.Should().Contain("createdAt");
    }

    [Fact]
    public async Task CreateBackup_ConTokenDeAdmin_BackupFalla_Retorna500()
    {
        // Arrange: el servicio lanza una excepción de operación inválida (pg_dump falló)
        _backupMock.Setup(s => s.GenerateBackupAsync())
            .ThrowsAsync(new InvalidOperationException("pg_dump falló (código 1): autenticación fallida"));

        var token   = GenerarToken(UserRole.Admin);
        var request = BuildRequest("POST", "/api/admin/backup", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var contenido = await response.Content.ReadAsStringAsync();
        contenido.Should().Contain("pg_dump falló");
    }

    [Fact]
    public async Task CreateBackup_ConTokenDeAdmin_LlamaAlServicioUnaVez()
    {
        // Arrange: el mock es compartido por IClassFixture; se limpian las invocaciones
        // previas para que Times.Once solo cuente la llamada de este test.
        _backupMock.Invocations.Clear();
        _backupMock.Setup(s => s.GenerateBackupAsync())
            .ReturnsAsync(new BackupResponseDto { FileName = "backup.sql" });

        var token   = GenerarToken(UserRole.Admin);
        var request = BuildRequest("POST", "/api/admin/backup", token);

        // Act
        await _client.SendAsync(request);

        // Assert: el pipeline no genera llamadas duplicadas al servicio
        _backupMock.Verify(s => s.GenerateBackupAsync(), Times.Once);
    }
}
