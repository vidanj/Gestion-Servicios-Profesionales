using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace SistemaServicios.Tests.Integration;

/// <summary>
/// Sonda siempre fallida, para simular la base de datos caída sin tener que apagarla.
/// </summary>
internal sealed class SondaCaida : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(HealthCheckResult.Unhealthy("La base de datos no está disponible."));
}

/// <summary>
/// Factory que sustituye la comprobación de base de datos por una que siempre falla.
/// </summary>
public class UnhealthyWebApplicationFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        _ = builder.ConfigureServices(services =>
        {
            // Se retira el registro real y se deja solo la sonda que falla, conservando
            // la etiqueta "ready" para que /health/ready siga evaluándola.
            var registros = services
                .Where(d => d.ServiceType == typeof(HealthCheckService))
                .ToList();
            foreach (var registro in registros)
            {
                _ = services.Remove(registro);
            }

            _ = services.Configure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(
                    new HealthCheckRegistration(
                        "postgresql",
                        new SondaCaida(),
                        HealthStatus.Unhealthy,
                        ["ready"]
                    )
                );
            });

            _ = services.AddHealthChecks();
        });
    }
}

/// <summary>
/// Pruebas de las sondas de disponibilidad.
/// </summary>
public class HealthChecksTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthChecksTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LiveRespondeSinToken()
    {
        // Arrange & Act: la sonda debe ser accesible sin autenticación; un orquestador
        // no tiene credenciales de la aplicación.
        var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative));

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LiveNoConsultaDependencias()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative));
        var contenido = await response.Content.ReadAsStringAsync();

        // Assert: liveness no evalúa ninguna comprobación. Si evaluara la base, una
        // caída de PostgreSQL provocaría reinicios en bucle de un proceso sano.
        _ = contenido.Should().Contain("\"status\":\"Healthy\"");
        _ = contenido.Should().Contain("\"checks\":[]");
    }

    [Fact]
    public async Task ReadyRespondeSinToken()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadyIncluyeLaComprobacionDeBaseDeDatos()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var contenido = await response.Content.ReadAsStringAsync();

        // Assert
        _ = contenido.Should().Contain("postgresql");
        _ = contenido.Should().Contain("\"status\":\"Healthy\"");
    }

    [Fact]
    public async Task ReadyNoFiltraConfiguracionSensible()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var contenido = await response.Content.ReadAsStringAsync();

        // Assert: la respuesta es anónima, así que no puede revelar cómo se conecta
        // la aplicación ni exponer trazas de excepción.
        _ = contenido.Should().NotContain("Password");
        _ = contenido.Should().NotContain("Host=");
        _ = contenido.Should().NotContain("Username");
        _ = contenido.Should().NotContain("Exception");
        _ = contenido.Should().NotContain("at Npgsql");
    }

    [Fact]
    public async Task ReadyDevuelveLaDuracionDeCadaComprobacion()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var contenido = await response.Content.ReadAsStringAsync();

        // Assert
        _ = contenido.Should().Contain("totalDurationMs");
        _ = contenido.Should().Contain("durationMs");
    }
}

/// <summary>
/// Comportamiento de las sondas con la base de datos inalcanzable.
/// </summary>
public class HealthChecksConBaseCaidaTests : IClassFixture<UnhealthyWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthChecksConBaseCaidaTests(UnhealthyWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ReadyDevuelve503CuandoLaBaseNoResponde()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        // Assert: es la señal que permite sacar la instancia de rotación en lugar de
        // seguir enviándole tráfico que va a fallar.
        _ = response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task LiveSigueRespondiendo200AunqueLaBaseEsteCaida()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative));

        // Assert: el proceso está sano; reiniciarlo no arreglaría la base y solo
        // provocaría un bucle de reinicios.
        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadyConBaseCaidaTampocoFiltraDetalleInterno()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var contenido = await response.Content.ReadAsStringAsync();

        // Assert
        _ = contenido.Should().Contain("Unhealthy");
        _ = contenido.Should().NotContain("Exception");
        _ = contenido.Should().NotContain("Host=");
    }
}
