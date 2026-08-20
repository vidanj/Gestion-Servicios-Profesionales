using System.Globalization;
using FluentAssertions;
using Serilog;
using Serilog.Events;
using SistemaServicios.API.Logging;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Pruebas del enricher de redacción. Se ejercita a través de un logger real y no
/// invocando <c>Enrich</c> a mano, porque lo que importa es lo que acaba en el sink.
/// </summary>
public class SecretRedactionEnricherTests
{
    private static (ILogger Logger, List<LogEvent> Eventos) CrearLogger()
    {
        var eventos = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.With<SecretRedactionEnricher>()
            .WriteTo.Sink(new SinkDeMemoria(eventos))
            .CreateLogger();

        return (logger, eventos);
    }

    [Theory]
    [InlineData("Password")]
    [InlineData("password")]
    [InlineData("UserPassword")]
    [InlineData("PasswordHash")]
    [InlineData("SmtpPassword")]
    [InlineData("PGPASSWORD")]
    [InlineData("Token")]
    [InlineData("AccessToken")]
    [InlineData("Authorization")]
    [InlineData("ConnectionString")]
    [InlineData("ApiKey")]
    public void RedactaLasPropiedadesCuyoNombreDelataUnSecreto(string nombre)
    {
        var (logger, eventos) = CrearLogger();

        logger.Information("Evento con {" + nombre + "}", "valor-super-secreto");

        var propiedad = eventos.Single().Properties[nombre];
        _ = propiedad.ToString().Should().NotContain("valor-super-secreto");
        _ = propiedad.ToString().Should().Contain("REDACTADO");
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("UserId")]
    [InlineData("RequestPath")]
    [InlineData("StatusCode")]
    public void NoTocaLasPropiedadesInocuas(string nombre)
    {
        // Redactar de más es tan malo como redactar de menos: un log sin el correo ni la
        // ruta no sirve para diagnosticar nada.
        var (logger, eventos) = CrearLogger();

        logger.Information("Evento con {" + nombre + "}", "dato-visible");

        _ = eventos.Single().Properties[nombre].ToString().Should().Contain("dato-visible");
    }

    [Fact]
    public void RedactaDentroDeUnObjetoVolcadoEntero()
    {
        // El descuido habitual no es registrar la contraseña suelta, sino volcar el DTO
        // completo con {@Dto}. Ahí el nombre de la propiedad externa no delata nada.
        var (logger, eventos) = CrearLogger();

        logger.Information(
            "Intento de registro {@Datos}",
            new
            {
                Email = "ana@ejemplo.com",
                Password = "Sup3rSecreta!",
                Nombre = "Ana",
            }
        );

        var texto = eventos.Single().Properties["Datos"].ToString();
        _ = texto.Should().NotContain("Sup3rSecreta!");
        _ = texto.Should().Contain("REDACTADO");

        // Y lo que no es secreto sigue estando: si se redactara el objeto entero, el log
        // dejaría de servir justo cuando hace falta.
        _ = texto.Should().Contain("ana@ejemplo.com");
        _ = texto.Should().Contain("Ana");
    }

    [Fact]
    public void RedactaDentroDeUnaColeccionDeObjetos()
    {
        var (logger, eventos) = CrearLogger();

        logger.Information(
            "Lote {@Usuarios}",
            new[]
            {
                new { Email = "uno@ejemplo.com", Password = "clave-uno" },
                new { Email = "dos@ejemplo.com", Password = "clave-dos" },
            }
        );

        var texto = eventos.Single().Properties["Usuarios"].ToString();
        _ = texto.Should().NotContain("clave-uno");
        _ = texto.Should().NotContain("clave-dos");
        _ = texto.Should().Contain("uno@ejemplo.com");
    }

    [Fact]
    public void NoRedactaElTextoYaInterpoladoEnElMensaje()
    {
        // Documenta el límite real de esta defensa, para que nadie la crea infalible:
        // con interpolación de C# el secreto viaja como parte del texto y aquí no queda
        // ninguna propiedad que redactar. La regla sigue siendo no meter secretos en la
        // plantilla del mensaje; el enricher es la red del descuido, no una garantía.
        var (logger, eventos) = CrearLogger();
        var clave = "Sup3rSecreta!";

        logger.Information($"La clave era {clave}");

        _ = eventos
            .Single()
            .RenderMessage(CultureInfo.InvariantCulture)
            .Should()
            .Contain("Sup3rSecreta!");
    }

    [Fact]
    public void NoRedactaUnSecretoEscondidoTrasUnNombreInocuo()
    {
        // El otro límite: la decisión se toma por el nombre de la propiedad. Si alguien
        // registra la contraseña bajo un nombre que no delata nada, pasa intacta. Queda
        // como prueba para que el límite sea explícito y no una sorpresa en un incidente.
        var (logger, eventos) = CrearLogger();

        logger.Information("Dato {Valor}", "Sup3rSecreta!");

        _ = eventos.Single().Properties["Valor"].ToString().Should().Contain("Sup3rSecreta!");
    }
}
