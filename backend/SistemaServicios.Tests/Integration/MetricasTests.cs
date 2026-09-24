using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaServicios.API.DTOs.Auth;
using SistemaServicios.API.Extensions;
using SistemaServicios.API.Interfaces;
using Xunit;

namespace SistemaServicios.Tests.Integration;

/// <summary>
/// Comprueba la instrumentación de métricas contra la aplicación en marcha.
/// </summary>
public class MetricasTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MetricasTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// La aplicación <b>no</b> expone un endpoint de métricas, y esta prueba es lo que
    /// impide que la decisión se erosione en silencio.
    /// </summary>
    /// <remarks>
    /// Las razones están en <c>specs/008-monitoreo-metricas-alertas/research.md</c>, D1: el
    /// exportador de Prometheus en proceso nunca ha publicado una versión estable y el
    /// proyecto compila sin advertencias ni silenciadores; y un endpoint de métricas en una
    /// API pública revela rutas, volúmenes y versiones. Las métricas salen por OTLP hacia un
    /// colector, que es quien las publica.
    /// </remarks>
    [Theory]
    [InlineData("/metrics")]
    [InlineData("/api/metrics")]
    public async Task LaApiNoExponeUnEndpointDeMetricas(string ruta)
    {
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.GetAsync(ruta);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Sin colector configurado la aplicación arranca igual y no intenta exportar. Es el
    /// comportamiento que permite trabajar en local sin levantar el entorno de monitoreo.
    /// </summary>
    [Fact]
    public void LaAplicacionArrancaSinColectorConfigurado()
    {
        using var ambito = _factory.Services.CreateScope();
        var configuracion = ambito.ServiceProvider.GetRequiredService<IConfiguration>();

        TelemetryConfiguration.HayColectorConfigurado(configuracion).Should().BeFalse();

        // Y aun así el medidor de negocio se resuelve: la instrumentación se registra
        // siempre, haya o no destino al que exportar.
        ambito
            .ServiceProvider.GetService<IMetricasDeNegocio>()
            .Should()
            .NotBeNull("la instrumentación no depende de que exista un colector");
    }

    /// <summary>
    /// Un inicio de sesión fallido recorre el flujo HTTP completo y llega hasta la métrica.
    /// Es lo que demuestra que la instrumentación está conectada de verdad y no solo
    /// registrada en el contenedor.
    /// </summary>
    [Fact]
    public async Task UnInicioDeSesionFallidoIncrementaElContador()
    {
        using var escucha = new EscuchaDeMetricasDeAplicacion(_factory);
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/Auth/login",
            new LoginRequestDto { Email = "inexistente@ejemplo.com", Password = "NoImporta123!" }
        );

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        escucha
            .Resultados()
            .Should()
            .Contain(
                "credenciales_invalidas",
                "el intento fallido tiene que llegar hasta la métrica"
            );
    }
}
