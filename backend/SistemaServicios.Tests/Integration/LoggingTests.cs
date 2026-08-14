using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using SistemaServicios.API.DTOs;
using SistemaServicios.API.Extensions;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using SistemaServicios.Tests.Unit;
using Xunit;

namespace SistemaServicios.Tests.Integration;

/// <summary>
/// Factory que añade un sink en memoria a la <b>misma</b> configuración de Serilog que usa
/// la aplicación, invocando <see cref="LoggingConfiguration.Configure"/>. Copiar aquí la
/// configuración habría hecho que las pruebas comprobaran una tubería paralela que puede
/// divergir de la real sin que nadie se entere.
/// </summary>
public class LoggingWebApplicationFactory : CustomWebApplicationFactory
{
    public List<LogEvent> Eventos { get; } = [];

    // Se aplica sobre el IHostBuilder y no en ConfigureWebHost porque UseSerilog no existe
    // sobre IWebHostBuilder. Al ejecutarse después del registro de Program.cs, este logger
    // sustituye al de la aplicación conservando su misma configuración.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.UseSerilog(
            (context, logger) =>
            {
                LoggingConfiguration.Configure(
                    logger,
                    context.Configuration,
                    context.HostingEnvironment
                );
                _ = logger.WriteTo.Sink(new SinkDeMemoria(Eventos));
            }
        );

        return base.CreateHost(builder);
    }
}

public class LoggingTests : IClassFixture<LoggingWebApplicationFactory>
{
    private const string Contrasena = "Sup3rSecreta!";

    private readonly LoggingWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LoggingTests(LoggingWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>Token de administrador firmado con la clave del entorno de pruebas.</summary>
    private static string GenerarTokenDeAdmin()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["JwtSettings:Key"] = "ClaveSecretaParaIntegracionTests_32Ch!",
                    ["JwtSettings:Issuer"] = "TestIssuer",
                    ["JwtSettings:Audience"] = "TestAudience",
                    ["JwtSettings:ExpiresInMinutes"] = "60",
                }
            )
            .Build();

        var usuario = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            PasswordHash = "hash-no-relevante",
            FirstName = "Admin",
            LastName = "Test",
            Role = UserRole.Admin,
            Status = true,
        };

        return new TokenService(config).CreateToken(usuario);
    }

    /// <summary>Renderiza los eventos igual que el formateador que escribe a stdout.</summary>
    private static string RenderizarComoStdout(IEnumerable<LogEvent> eventos)
    {
        var formateador = new CompactJsonFormatter();
        using var escritor = new StringWriter(CultureInfo.InvariantCulture);

        foreach (var evento in eventos)
        {
            formateador.Format(evento, escritor);
        }

        return escritor.ToString();
    }

    [Fact]
    public async Task ElLogDeUnLoginRealNuncaContieneLaContrasena()
    {
        // Act: da igual que las credenciales sean inválidas; lo que se comprueba es qué
        // deja escrito la petición a su paso, y una que falla registra más, no menos.
        using var respuestaLogin = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "ana@ejemplo.com", Password = Contrasena }
        );

        var salida = RenderizarComoStdout(_factory.Eventos);

        // Guardia contra una prueba vacía: si el sink no capturó nada, la afirmación de
        // abajo pasaría sola y no probaría absolutamente nada.
        _ = _factory.Eventos.Should().NotBeEmpty("el sink debe haber recibido eventos");
        _ = salida.Should().Contain("/api/auth/login", "debe haberse registrado la petición");

        // Assert
        _ = salida.Should().NotContain(Contrasena);
    }

    [Fact]
    public async Task CadaPeticionSeRegistraConSuTraceId()
    {
        using var respuesta = await _client.GetAsync(new Uri("/api/auth/login", UriKind.Relative));

        var conTraza = _factory.Eventos.Where(e => e.Properties.ContainsKey("TraceId")).ToList();

        // Es la propiedad que permite reunir las líneas de una misma petición durante un
        // incidente; sin ella el log estructurado pierde casi todo su valor.
        _ = conTraza.Should().NotBeEmpty();
        _ = conTraza[0].Properties["TraceId"].ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LaBitacoraDeNegocioQuedaEnlazadaConElLogDeOperacion()
    {
        // Es el puente entre las dos bitácoras. UserLog responde "quién hizo qué" y vive
        // en PostgreSQL; el log de stdout responde "qué pasó por dentro". Sin este enlace
        // son dos mundos incomunicados: ante una reclamación habría que adivinar qué
        // líneas del log corresponden a la acción registrada en la tabla.
        var token = GenerarTokenDeAdmin();
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/UserLogs")
        {
            Content = JsonContent.Create(
                new
                {
                    UserId = Guid.NewGuid(),
                    Action = 6,
                    Detail = "Usuario creado desde la prueba",
                    Status = 0,
                }
            ),
        };
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await _client.SendAsync(peticion);
        _ = respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        var creado = await respuesta.Content.ReadFromJsonAsync<UserLogDto>();

        // La entrada guarda la traza de la petición que la originó...
        _ = creado!.TraceId.Should().NotBeNullOrWhiteSpace();

        // ...y esa traza aparece en el log de operación, que es lo que permite pasar de
        // una fila del panel a todo lo que ocurrió técnicamente en esa misma petición.
        var eventosDeLaTraza = _factory
            .Eventos.Where(e =>
                e.Properties.TryGetValue("TraceId", out var t)
                && t.ToString().Contains(creado.TraceId!, StringComparison.Ordinal)
            )
            .ToList();

        _ = eventosDeLaTraza.Should().NotBeEmpty();
        _ = RenderizarComoStdout(eventosDeLaTraza).Should().Contain("/api/UserLogs");
    }

    [Fact]
    public async Task LasSondasDeSaludNoEnsucianElLog()
    {
        var antes = _factory.Eventos.Count;

        using var live = await _client.GetAsync(new Uri("/health/live", UriKind.Relative));
        using var ready = await _client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        var nuevos = _factory.Eventos.Skip(antes).ToList();
        var salida = RenderizarComoStdout(nuevos);

        // El contenedor sondea cada 30 s: sin esta exclusión el log de producción sería
        // sobre todo ruido de sondas, y se pagaría retención por algo que no informa.
        _ = salida.Should().NotContain("/health/live");
        _ = salida.Should().NotContain("/health/ready");
    }
}
