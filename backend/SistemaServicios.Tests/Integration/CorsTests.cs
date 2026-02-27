using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SistemaServicios.Tests.Integration;

/// <summary>
/// Pruebas de integración del middleware CORS.
/// Verifican que la política "FrontendPolicy" permita el origen configurado en
/// ALLOWED_ORIGINS y rechace cualquier otro origen no autorizado.
/// </summary>
public class CorsTests : IClassFixture<CustomWebApplicationFactory>
{
    // Debe coincidir exactamente con ALLOWED_ORIGINS en CustomWebApplicationFactory
    private const string OrigenPermitido   = "http://localhost:3000";
    private const string OrigenNoPermitido = "http://evil.com";

    private readonly HttpClient _client;

    public CorsTests(CustomWebApplicationFactory factory)
    {
        // AllowAutoRedirect = false: evitar que el cliente siga redirecciones y oculte
        // la respuesta real del servidor (importante para que UseHttpsRedirection no
        // interfiera con las respuestas OPTIONS).
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    // ─────────────────────────────────────────────────────────────
    // Preflight OPTIONS — origen permitido
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cors_Preflight_OrigenPermitido_RetornaHeaderACAO()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", OrigenPermitido);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        // Act
        var response = await _client.SendAsync(request);

        // Assert: el middleware CORS responde al preflight con el origen reflejado
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue(
            because: "una petición preflight desde un origen permitido debe recibir el header CORS");
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().Contain(OrigenPermitido);
    }

    [Fact]
    public async Task Cors_Preflight_OrigenPermitido_PermiteMetodoPOST()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", OrigenPermitido);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type, Authorization");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Contains("Access-Control-Allow-Methods").Should().BeTrue();
        var metodos = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods"));
        metodos.Should().Contain("POST", because: "la política permite cualquier método");
    }

    // ─────────────────────────────────────────────────────────────
    // Peticiones reales — origen permitido
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cors_Post_OrigenPermitido_RetornaHeaderACAO()
    {
        // Arrange: cuerpo vacío → 400, pero el header CORS debe aparecer igualmente
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        request.Headers.Add("Origin", OrigenPermitido);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.SendAsync(request);

        // Assert: independientemente del código de respuesta HTTP, el header CORS debe estar
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue(
            because: "el middleware CORS agrega el header antes de que el controller procese la request");
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().Contain(OrigenPermitido);
    }

    [Fact]
    public async Task Cors_Get_OrigenPermitido_RetornaHeaderACAO()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Origin", OrigenPermitido);

        // Act
        var response = await _client.SendAsync(request);

        // Assert: 401 esperado (sin token), pero CORS debe permitir la petición
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().Contain(OrigenPermitido);
    }

    // ─────────────────────────────────────────────────────────────
    // Peticiones reales — origen NO permitido
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cors_Post_OrigenNoPermitido_NoRetornaHeaderACAO()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        request.Headers.Add("Origin", OrigenNoPermitido);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.SendAsync(request);

        // Assert: el servidor NO incluye el header → el navegador bloqueará la respuesta
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse(
            because: $"'{OrigenNoPermitido}' no está en la lista ALLOWED_ORIGINS");
    }

    [Fact]
    public async Task Cors_Preflight_OrigenNoPermitido_NoRetornaHeaderACAO()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", OrigenNoPermitido);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse(
            because: "un origen no autorizado no debe recibir cabeceras CORS");
    }

    // ─────────────────────────────────────────────────────────────
    // Peticiones sin header Origin (no son peticiones CORS)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cors_SinHeaderOrigin_ApiRespondeNormalmenteSinHeaderCors()
    {
        // Arrange: petición directa sin Origin (curl, Swagger, servidor → servidor)
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.SendAsync(request);

        // Assert: no es petición CORS → el middleware no agrega Access-Control-Allow-Origin
        // La API responde normalmente (400 por cuerpo incompleto) sin cabeceras CORS
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse(
            because: "sin header Origin no es una petición cross-origin y no necesita CORS");
    }
}
