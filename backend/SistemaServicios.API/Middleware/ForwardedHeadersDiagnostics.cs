namespace SistemaServicios.API.Middleware;

/// <summary>
/// Registra la cadena de X-Forwarded-For recibida y la dirección que el middleware
/// resolvió a partir de ella. Solo en nivel Debug: existe para poder confirmar el
/// ForwardLimit correcto contra el despliegue real, que es la única forma de saber
/// cuántos saltos tiene la cadena de verdad.
/// </summary>
public partial class ForwardedHeadersDiagnostics
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ForwardedHeadersDiagnostics> _logger;

    public ForwardedHeadersDiagnostics(
        RequestDelegate next,
        ILogger<ForwardedHeadersDiagnostics> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // La comprobación evita trabajo en producción, donde el nivel habitual deja
        // este registro apagado.
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            await _next(context);
            return;
        }

        // La cadena se captura ANTES de que UseForwardedHeaders la procese: ese
        // middleware consume las entradas que aplica y deja solo el resto. Registrar
        // la cadena ya recortada haría creer que llegaron menos saltos de los reales,
        // que es justo el dato que se quiere medir aquí.
        var cadenaOriginal = context.Request.Headers["X-Forwarded-For"].ToString();

        await _next(context);

        LogCadenaRecibida(
            string.IsNullOrEmpty(cadenaOriginal) ? "(sin cabecera)" : cadenaOriginal,
            context.Connection.RemoteIpAddress?.ToString() ?? "(sin dirección)"
        );
    }

    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Debug,
        Message = "X-Forwarded-For recibido: {Cadena} | dirección resuelta: {Resuelta}"
    )]
    private partial void LogCadenaRecibida(string cadena, string resuelta);
}
