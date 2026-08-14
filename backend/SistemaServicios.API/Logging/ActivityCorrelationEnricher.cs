using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace SistemaServicios.API.Logging;

/// <summary>
/// Añade a cada línea de log el identificador de la traza activa.
/// </summary>
/// <remarks>
/// Es la propiedad que convierte un montón de líneas sueltas en la historia de una
/// petición concreta: sin ella, ante un incidente hay que adivinar qué líneas pertenecen
/// al usuario que se quejó. ASP.NET Core ya crea la <see cref="Activity"/> por petición,
/// así que aquí solo se lee; no se genera ningún identificador propio, porque uno inventado
/// no casaría con el que exporta OpenTelemetry y se perdería la correlación entre logs y
/// trazas, que es justo lo que se busca.
/// </remarks>
public sealed class ActivityCorrelationEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        var activity = Activity.Current;

        // Fuera de una petición —tareas en segundo plano, arranque— no hay traza y no
        // se inventa: una propiedad vacía haría creer que la correlación existe.
        if (activity is null)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString())
        );
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString())
        );
    }
}
