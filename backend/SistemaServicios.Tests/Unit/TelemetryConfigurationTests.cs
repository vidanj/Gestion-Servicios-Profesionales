using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using SistemaServicios.API.Extensions;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Comprueba que la telemetría no convierte al colector en un requisito de arranque.
/// </summary>
public class TelemetryConfigurationTests
{
    private static IConfiguration Configuracion(string? endpoint)
    {
        var valores = new Dictionary<string, string?>();

        if (endpoint is not null)
        {
            valores[TelemetryConfiguration.VariableDeEndpoint] = endpoint;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SinEndpointNoSeConsideraQueHayaColector(string? endpoint)
    {
        _ = TelemetryConfiguration
            .HayColectorConfigurado(Configuracion(endpoint))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ConEndpointSeConsideraQueHayColector()
    {
        _ = TelemetryConfiguration
            .HayColectorConfigurado(Configuracion("http://localhost:4317"))
            .Should()
            .BeTrue();
    }

    [Fact]
    public void LaAplicacionResuelveLaTelemetriaAunqueNoHayaColector()
    {
        // Es el caso de hoy en producción: no hay colector desplegado. Si la ausencia de
        // la variable impidiera construir el proveedor, la aplicación no arrancaría por
        // no poder exportar telemetría, que es exactamente lo que no debe ocurrir.
        var services = new ServiceCollection();
        _ = services.AddLogging();

        _ = services.AddTelemetry(Configuracion(null));

        using var provider = services.BuildServiceProvider();
        _ = provider.GetService<TracerProvider>().Should().NotBeNull();
    }
}
