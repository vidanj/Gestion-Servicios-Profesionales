using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace SistemaServicios.Tests.Integration;

/// <summary>
/// Factory que arranca el servidor con ambiente "Development" para que
/// Program.cs active app.UseSwagger() y app.UseSwaggerUI().
/// Esto permite que la lambda de AddSwaggerGen(c => { ... }) se ejecute
/// durante la generación del documento OpenAPI y sus líneas sean cubiertas.
/// </summary>
public class DevelopmentWebApplicationFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Hereda variables de entorno, InMemory DB y CORS de la factory base
        base.ConfigureWebHost(builder);

        // Sobreescribe a "Development" para que IsDevelopment() devuelva true
        // y el pipeline registre el middleware de Swagger
        builder.UseEnvironment("Development");
    }
}

/// <summary>
/// Pruebas de integración que verifican la configuración de Swagger
/// registrada en ApplicationServiceExtensions.AddSwaggerGen().
///
/// Por qué son necesarias para el coverage:
/// AddSwaggerGen(c => { ... }) registra una lambda que ASP.NET Core ejecuta
/// de forma diferida cuando el middleware de Swagger genera el documento
/// /swagger/v1/swagger.json. En ambiente "Testing" ese middleware no está
/// activo (Program.cs lo registra solo en IsDevelopment()), por lo que las
/// líneas de configuración (SwaggerDoc, AddSecurityDefinition,
/// AddSecurityRequirement) nunca se ejecutan en el resto de tests.
/// </summary>
public class SwaggerTests : IClassFixture<DevelopmentWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SwaggerTests(DevelopmentWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─────────────────────────────────────────────────────────────
    // GET /swagger/v1/swagger.json
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Swagger_DocumentoOpenApi_Retorna200()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "el endpoint de Swagger debe estar disponible en ambiente Development");
    }

    [Fact]
    public async Task Swagger_DocumentoOpenApi_ContieneNombreYVersionDelApi()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var contenido = await response.Content.ReadAsStringAsync();

        // Assert — refleja el SwaggerDoc configurado en AddApplicationServices
        contenido.Should().Contain("Sistema Servicios API",
            because: "el título fue configurado en c.SwaggerDoc(\"v1\", new OpenApiInfo { Title = ... })");
        contenido.Should().Contain("v1",
            because: "la versión fue configurada en c.SwaggerDoc(\"v1\", ...)");
    }

    [Fact]
    public async Task Swagger_DocumentoOpenApi_ContieneEsquemaDeSeguridad()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var contenido = await response.Content.ReadAsStringAsync();

        // Assert — refleja AddSecurityDefinition y AddSecurityRequirement
        contenido.Should().Contain("Bearer",
            because: "el esquema de seguridad JWT Bearer fue registrado con c.AddSecurityDefinition");
    }

    [Fact]
    public async Task Swagger_DocumentoOpenApi_ContentTypeEsJson()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/json");
    }
}
