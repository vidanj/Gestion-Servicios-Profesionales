namespace SistemaServicios.API.Extensions;

/// <summary>
/// Rutas que consultan la plataforma y el contenedor, no las personas.
/// </summary>
/// <remarks>
/// La lista vive en un solo sitio porque la usan dos cosas distintas y separarlas
/// llevaría a que una se actualizara sin la otra: el log de peticiones las excluye para no
/// ahogarse en ruido de sondas, y la redirección a HTTPS las esquiva porque el sondeo
/// interno de Render llega sin <c>X-Forwarded-Proto</c> y el middleware acabaría avisando
/// de que no puede determinar el puerto.
/// </remarks>
public static class RutasDeSondeo
{
    private static readonly string[] Prefijos = ["/health"];

    /// <remarks>
    /// La raíz se compara de forma exacta y <b>nunca</b> por prefijo:
    /// <c>StartsWithSegments("/")</c> casaría con todas las rutas del sistema. Aplicado al
    /// log dejaría la aplicación muda, y aplicado a la redirección la desactivaría entera.
    /// </remarks>
    private static readonly string[] Exactas = ["/"];

    /// <summary>Indica si la ruta la consulta la plataforma y no un usuario.</summary>
    public static bool Es(PathString ruta)
    {
        if (
            Array.Exists(
                Exactas,
                exacta => string.Equals(ruta.Value, exacta, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return true;
        }

        // StartsWithSegments respeta los límites de segmento, así que "/healthcheck" no
        // queda cubierto por el prefijo "/health".
        return Array.Exists(Prefijos, prefijo => ruta.StartsWithSegments(prefijo));
    }
}
