using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace SistemaServicios.API.Extensions;

/// <summary>
/// Configuración del middleware de cabeceras reenviadas.
/// Vive aparte para que las pruebas ejerciten exactamente la misma configuración que
/// usa la aplicación, y no una copia que puede divergir sin que nadie lo note.
/// </summary>
public static class ForwardedHeadersConfiguration
{
    /// <summary>
    /// Número de proxies de confianza por defecto: Cloudflare y el edge de Render.
    /// El valor por defecto del framework es 1, que con esta topología devolvería la
    /// dirección de Cloudflare en lugar de la del cliente.
    /// </summary>
    public const int DefaultForwardLimit = 2;

    public static void Configure(
        ForwardedHeadersOptions options,
        string? forwardLimitRaw,
        string? knownNetworksRaw
    )
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // ForwardLimit es la defensa real: el middleware toma las N entradas más a la
        // derecha de X-Forwarded-For y descarta las de la izquierda, que son las que un
        // cliente puede anteponer. Un valor mayor que los saltos reales empezaría a
        // confiar en datos que controla quien llama.
        options.ForwardLimit = ParseForwardLimit(forwardLimitRaw);

        // En una plataforma gestionada no se pueden enumerar las direcciones del proxy,
        // y con las listas por defecto (solo loopback) el middleware no haría nada.
        // Se vacían a propósito; quien pueda enumerarlas las aporta por configuración.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var network in ParseKnownNetworks(knownNetworksRaw))
        {
            options.KnownNetworks.Add(network);
        }
    }

    private static int ParseForwardLimit(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultForwardLimit;
        }

        // Un valor inválido no debe degradar en silencio a "confiar en todo".
        return
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            && value > 0
            ? value
            : DefaultForwardLimit;
    }

    private static IEnumerable<IPNetwork> ParseKnownNetworks(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            yield break;
        }

        foreach (
            var entry in raw.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
        )
        {
            var partes = entry.Split('/');
            if (
                partes.Length == 2
                && IPAddress.TryParse(partes[0], out var direccion)
                && int.TryParse(
                    partes[1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var prefijo
                )
            )
            {
                yield return new IPNetwork(direccion, prefijo);
            }
        }
    }
}
