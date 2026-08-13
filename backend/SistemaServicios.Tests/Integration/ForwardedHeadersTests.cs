using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SistemaServicios.API.Extensions;
using Xunit;

namespace SistemaServicios.Tests.Integration;

/// <summary>
/// Pruebas del middleware de cabeceras reenviadas.
/// Usan la misma configuración que aplica la API (ForwardedHeadersConfiguration), no
/// una copia: si esa configuración cambia, estas pruebas lo reflejan.
/// </summary>
public class ForwardedHeadersTests
{
    /// <summary>
    /// Host mínimo con el middleware configurado y un terminal que devuelve la
    /// dirección resuelta. Evita añadir a la API un endpoint de diagnóstico.
    /// </summary>
    private static async Task<IHost> CrearHost(string? limite, string? redes = null)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                _ = webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                        services.Configure<ForwardedHeadersOptions>(options =>
                            ForwardedHeadersConfiguration.Configure(options, limite, redes)
                        )
                    )
                    .Configure(app =>
                    {
                        app.UseForwardedHeaders();
                        app.Run(async context =>
                        {
                            var direccion =
                                context.Connection.RemoteIpAddress?.ToString() ?? "(nula)";
                            await context.Response.WriteAsync(
                                $"{direccion}|{context.Request.Scheme}"
                            );
                        });
                    });
            })
            .StartAsync();

        return host;
    }

    private static async Task<string> Pedir(
        IHost host,
        string? forwardedFor,
        string? forwardedProto = null
    )
    {
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");

        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        if (forwardedProto is not null)
        {
            request.Headers.Add("X-Forwarded-Proto", forwardedProto);
        }

        var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    // ─────────────────────────────────────────────────────────────
    // Resolución de la dirección del cliente
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConDosSaltosResuelveLaDireccionDelCliente()
    {
        // Arrange: es la topología real, Cloudflare y el edge de Render.
        using var host = await CrearHost("2");

        // Act: el cliente es el primero por la izquierda; a la derecha, los proxies.
        var resultado = await Pedir(host, "203.0.113.7, 198.51.100.1");

        // Assert
        _ = resultado.Should().StartWith("203.0.113.7");
    }

    [Fact]
    public async Task ConElLimitePorDefectoDeUnoSeQuedaEnLaDireccionDelProxy()
    {
        // Arrange: es el defecto que motiva este issue. El valor por defecto del
        // framework es 1, insuficiente para una cadena de dos proxies.
        using var host = await CrearHost("1");

        // Act
        var resultado = await Pedir(host, "203.0.113.7, 198.51.100.1");

        // Assert: se queda en el proxy, no llega al cliente
        _ = resultado.Should().StartWith("198.51.100.1");
    }

    [Fact]
    public async Task SinConfigurarUsaDosSaltosPorDefecto()
    {
        // Arrange: el default de esta aplicación es 2, no el 1 del framework.
        using var host = await CrearHost(null);

        // Act
        var resultado = await Pedir(host, "203.0.113.7, 198.51.100.1");

        // Assert
        _ = resultado.Should().StartWith("203.0.113.7");
    }

    // ─────────────────────────────────────────────────────────────
    // Defensa contra falsificación
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DescartaLaDireccionQueElClienteAntepone()
    {
        // Arrange: el atacante antepone una dirección falsa; los proxies añaden las
        // suyas a la derecha. Esta es la prueba que da sentido a ForwardLimit.
        using var host = await CrearHost("2");

        // Act: "9.9.9.9" la puso el cliente; las dos siguientes, los proxies reales.
        var resultado = await Pedir(host, "9.9.9.9, 203.0.113.7, 198.51.100.1");

        // Assert: la falsificada se descarta y se resuelve la que puso el primer proxy
        _ = resultado.Should().StartWith("203.0.113.7");
        _ = resultado.Should().NotContain("9.9.9.9");
    }

    [Fact]
    public async Task UnaCadenaMuyLargaNoPermiteSuplantarLaDireccion()
    {
        // Arrange: por muchas entradas que anteponga el cliente, solo se consideran
        // las dos de la derecha.
        using var host = await CrearHost("2");

        // Act
        var resultado = await Pedir(
            host,
            "1.1.1.1, 2.2.2.2, 3.3.3.3, 4.4.4.4, 203.0.113.7, 198.51.100.1"
        );

        // Assert
        _ = resultado.Should().StartWith("203.0.113.7");
    }

    [Fact]
    public async Task UnLimiteInvalidoNoDegradaAConfiarEnTodo()
    {
        // Arrange: un valor mal escrito en la configuración no debe convertirse en
        // "acepta cualquier cadena", que sería el fallo peligroso.
        using var host = await CrearHost("no-es-un-numero");

        // Act
        var resultado = await Pedir(host, "9.9.9.9, 203.0.113.7, 198.51.100.1");

        // Assert: cae al valor por defecto, que sigue descartando la falsificada
        _ = resultado.Should().StartWith("203.0.113.7");
    }

    [Fact]
    public async Task UnLimiteCeroONegativoCaeAlValorPorDefecto()
    {
        // Arrange
        using var host = await CrearHost("0");

        // Act
        var resultado = await Pedir(host, "9.9.9.9, 203.0.113.7, 198.51.100.1");

        // Assert
        _ = resultado.Should().StartWith("203.0.113.7");
    }

    // ─────────────────────────────────────────────────────────────
    // Protocolo original
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RespetaElProtocoloOriginalDelCliente()
    {
        // Arrange: el proxy termina TLS y habla http con la aplicación. Sin esto, la
        // aplicación creería que la petición llegó sin cifrar.
        using var host = await CrearHost("2");

        // Act
        var resultado = await Pedir(host, "203.0.113.7, 198.51.100.1", "https");

        // Assert
        _ = resultado.Should().EndWith("|https");
    }

    [Fact]
    public async Task SinCabecerasNoAlteraLaPeticion()
    {
        // Arrange: una petición directa, sin proxy delante, debe pasar intacta.
        using var host = await CrearHost("2");

        // Act
        var resultado = await Pedir(host, forwardedFor: null);

        // Assert
        _ = resultado.Should().EndWith("|http");
    }

    // ─────────────────────────────────────────────────────────────
    // Redes conocidas
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConRedesConocidasIgnoraLaCabeceraDeUnPeerDesconocido()
    {
        // Arrange: al declarar redes, el middleware solo procesa la cabecera si el peer
        // inmediato pertenece a ellas. El servidor de pruebas no está en 10.0.0.0/8.
        using var host = await CrearHost("2", "10.0.0.0/8");

        // Act
        var resultado = await Pedir(host, "203.0.113.7, 198.51.100.1");

        // Assert: no se aplica la cabecera, así que no aparece la dirección del cliente
        _ = resultado.Should().NotStartWith("203.0.113.7");
    }
}
