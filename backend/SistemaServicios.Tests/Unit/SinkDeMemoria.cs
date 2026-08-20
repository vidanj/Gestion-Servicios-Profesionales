using Serilog.Core;
using Serilog.Events;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Sink que guarda los eventos en memoria para poder afirmar sobre ellos.
/// </summary>
/// <remarks>
/// Se escribe aquí en lugar de añadir un paquete de terceros: son diez líneas y evita una
/// dependencia más en un proyecto que ya arrastra 69 alertas de Dependabot.
/// </remarks>
public sealed class SinkDeMemoria : ILogEventSink
{
    private readonly List<LogEvent> _eventos;
    private readonly Lock _candado = new();

    public SinkDeMemoria(List<LogEvent> eventos)
    {
        _eventos = eventos;
    }

    public void Emit(LogEvent logEvent)
    {
        // Serilog puede emitir desde varios hilos; sin el candado la lista se corrompe y
        // el fallo aparecería como una prueba intermitente, que es lo peor de diagnosticar.
        lock (_candado)
        {
            _eventos.Add(logEvent);
        }
    }
}
