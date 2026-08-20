using Serilog.Core;
using Serilog.Events;

namespace SistemaServicios.API.Logging;

/// <summary>
/// Sustituye por una marca el valor de las propiedades cuyo nombre delata un secreto.
/// </summary>
/// <remarks>
/// <para>
/// Alcance real, para no dar una falsa sensación de seguridad: esto actúa sobre las
/// <b>propiedades estructuradas</b> del evento, no sobre el texto ya interpolado en la
/// plantilla del mensaje. Si alguien escribe <c>_logger.LogInformation($"clave {clave}")</c>
/// con interpolación de C#, el secreto llega al sink como parte del texto y aquí no hay
/// nada que redactar. La defensa de verdad es no meter secretos en la plantilla; esto es
/// la red que recoge el descuido habitual, que es volcar el DTO entero.
/// </para>
/// <para>
/// Se compara el <b>nombre</b> de la propiedad y nunca su valor: buscar patrones dentro de
/// los valores daría falsos positivos constantes y, peor, obligaría a recorrer datos que
/// puede que ni sean texto.
/// </para>
/// </remarks>
public sealed class SecretRedactionEnricher : ILogEventEnricher
{
    /// <summary>Marca visible: un valor vacío no distinguiría "redactado" de "no venía".</summary>
    public const string Marca = "***REDACTADO***";

    // Fragmentos, no nombres exactos: el descuido tipico no es una propiedad llamada
    // "Password" sino "UserPassword", "PasswordHash" o "SmtpPassword".
    private static readonly string[] FragmentosSensibles =
    [
        "password",
        "passwd",
        "pwd",
        "contrasena",
        "contraseña",
        "secret",
        "token",
        "authorization",
        "apikey",
        "api_key",
        "connectionstring",
        "pgpassword",
        "jwt",
        "credential",
    ];

    /// <summary>Indica si el nombre de una propiedad delata que su valor es un secreto.</summary>
    public static bool EsNombreSensible(string nombre)
    {
        return !string.IsNullOrEmpty(nombre)
            && Array.Exists(
                FragmentosSensibles,
                fragmento => nombre.Contains(fragmento, StringComparison.OrdinalIgnoreCase)
            );
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        // Se toma una instantanea: AddOrUpdateProperty modifica la coleccion que se recorre.
        foreach (var propiedad in logEvent.Properties.ToArray())
        {
            var redactado = Redactar(propiedad.Key, propiedad.Value);

            if (!ReferenceEquals(redactado, propiedad.Value))
            {
                logEvent.AddOrUpdateProperty(new LogEventProperty(propiedad.Key, redactado));
            }
        }
    }

    private static LogEventPropertyValue Redactar(string nombre, LogEventPropertyValue valor)
    {
        return EsNombreSensible(nombre) ? new ScalarValue(Marca) : RedactarInterior(valor);
    }

    // Un DTO volcado con {@Dto} llega como StructureValue: si no se entrara en el, la
    // contrasena viajaria intacta dentro de un objeto cuyo nombre no delata nada.
    private static LogEventPropertyValue RedactarInterior(LogEventPropertyValue valor)
    {
        switch (valor)
        {
            case StructureValue estructura:
            {
                var propiedades = estructura
                    .Properties.Select(p => new LogEventProperty(p.Name, Redactar(p.Name, p.Value)))
                    .ToArray();
                return new StructureValue(propiedades, estructura.TypeTag);
            }

            case DictionaryValue diccionario:
            {
                var elementos = diccionario.Elements.Select(par =>
                    KeyValuePair.Create(
                        par.Key,
                        Redactar(par.Key.Value?.ToString() ?? string.Empty, par.Value)
                    )
                );
                return new DictionaryValue(elementos);
            }

            case SequenceValue secuencia:
            {
                return new SequenceValue(secuencia.Elements.Select(RedactarInterior).ToArray());
            }

            default:
                return valor;
        }
    }
}
