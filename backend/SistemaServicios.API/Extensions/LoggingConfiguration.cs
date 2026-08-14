using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using SistemaServicios.API.Logging;

namespace SistemaServicios.API.Extensions;

/// <summary>
/// Configuración de Serilog, extraída para que las pruebas ejerciten la misma que usa la
/// aplicación en lugar de una copia que puede divergir sin que nadie se entere.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>Rutas que no generan un evento de petición.</summary>
    /// <remarks>
    /// Las sondas del contenedor (issue #123) consultan <c>/health/ready</c> cada 30 s. Sin
    /// esta exclusión, el log de producción sería mayoritariamente ruido de sondas y el
    /// coste de retención se lo llevaría algo que no informa de nada: si la sonda falla, se
    /// nota porque el contenedor se reinicia, no porque haya una línea de log.
    /// </remarks>
    private static readonly string[] RutasSilenciadas = ["/health"];

    public static void Configure(
        LoggerConfiguration logger,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        _ = logger
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "SistemaServicios.API")
            .Enrich.WithProperty("Environment", environment.EnvironmentName)
            .Enrich.With<ActivityCorrelationEnricher>();

        // La redacción va la última a propósito: así ve también las propiedades que
        // añaden los enrichers anteriores y las del LogContext.
        _ = logger.Enrich.With<SecretRedactionEnricher>();

        // JSON compacto a stdout y nunca a archivo. El contenedor es efímero: un sink de
        // archivo perdería los logs en cada redespliegue y reintroduciría la dependencia
        // del sistema de archivos local que el issue #125 quitó del almacenamiento.
        // Render, Docker y cualquier agregador recogen stdout sin configurar nada.
        _ = logger.WriteTo.Console(new CompactJsonFormatter());
    }

    /// <summary>Decide si una petición merece su propio evento de log.</summary>
    public static bool DebeRegistrarPeticion(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return !Array.Exists(
            RutasSilenciadas,
            ruta => context.Request.Path.StartsWithSegments(ruta)
        );
    }

    /// <summary>
    /// Nivel del evento de una petición: eleva los fallos y silencia las sondas.
    /// </summary>
    /// <remarks>
    /// Las sondas se descartan devolviendo <c>Verbose</c> y no con un filtro aparte, porque
    /// así es como Serilog deja fuera un evento: el nivel mínimo configurado es Information
    /// en producción y Debug en desarrollo, de modo que Verbose nunca llega a un sink. Un
    /// filtro propio duplicaría el mecanismo y podría discrepar de la configuración.
    /// </remarks>
    public static LogEventLevel NivelDePeticion(HttpContext context, Exception? ex)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (ex is not null || context.Response.StatusCode >= 500)
        {
            return LogEventLevel.Error;
        }

        if (!DebeRegistrarPeticion(context))
        {
            return LogEventLevel.Verbose;
        }

        return context.Response.StatusCode >= 400
            ? LogEventLevel.Warning
            : LogEventLevel.Information;
    }
}
