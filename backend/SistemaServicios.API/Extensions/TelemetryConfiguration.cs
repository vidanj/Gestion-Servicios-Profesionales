using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SistemaServicios.API.Telemetry;

namespace SistemaServicios.API.Extensions;

/// <summary>
/// Registro de OpenTelemetry para trazas y métricas.
/// </summary>
public static class TelemetryConfiguration
{
    /// <summary>Endpoint del colector. Sin él la aplicación arranca igual y no exporta.</summary>
    public const string VariableDeEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";

    private const string NombreDelServicio = "SistemaServicios.API";

    /// <summary>
    /// Indica si hay un colector configurado al que exportar.
    /// </summary>
    public static bool HayColectorConfigurado(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return !string.IsNullOrWhiteSpace(configuration[VariableDeEndpoint]);
    }

    /// <summary>
    /// Añade la instrumentación y, solo si hay colector, la exportación OTLP.
    /// </summary>
    /// <remarks>
    /// La instrumentación se registra <b>siempre</b>, haya colector o no, y esto es
    /// deliberado: es lo que garantiza que exista una <c>Activity</c> por petición y, con
    /// ella, el <c>TraceId</c> que el enricher de correlación escribe en cada línea de log.
    /// Sin instrumentación no habría traza que leer y los logs volverían a ser líneas
    /// sueltas imposibles de enlazar. La exportación, en cambio, sí es condicional: hoy no
    /// hay ningún colector desplegado, y apuntar a un endpoint inexistente solo produciría
    /// reintentos y ruido en el arranque.
    /// </remarks>
    public static IServiceCollection AddTelemetry(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var exportar = HayColectorConfigurado(configuration);

        _ = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(NombreDelServicio))
            .WithTracing(tracing =>
            {
                // El filtro deja fuera las sondas del contenedor: generarían una traza
                // cada 30 s sin aportar nada, mismo criterio que en el log de peticiones.
                // Y AddNpgsql da trazas a nivel de driver, que cubren la pregunta de
                // "dónde se fue el tiempo" sin recurrir al paquete de instrumentación de
                // EF Core, que sigue en preestreno y no cabe en un build que trata las
                // advertencias como errores.
                _ = tracing
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health")
                    )
                    .AddHttpClientInstrumentation()
                    .AddNpgsql();

                if (exportar)
                {
                    _ = tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                // AddRuntimeInstrumentation es estable, a diferencia del exportador de
                // Prometheus y de la instrumentación de EF Core. Ojo con los nombres: en
                // .NET 9 este paquete ya no recolecta por su cuenta ni emite
                // process.runtime.dotnet.*; lo que hace es registrar el medidor integrado
                // "System.Runtime", cuyos instrumentos se llaman dotnet.*. Casi todos los
                // tableros publicados que se encuentran buscando están escritos para
                // .NET 6-8 y usan los nombres viejos: copiarlos produce un tablero que se
                // aprovisiona sin ningún error y aparece permanentemente vacío.
                //
                // El medidor del limitador de peticiones tampoco viaja con
                // AddAspNetCoreInstrumentation: publica en uno propio que hay que pedir por
                // nombre. Sin esa línea no habría forma de ver cuántas peticiones rechaza
                // AuthLimiter, que es la señal que distingue "nadie entra" de "alguien está
                // probando contraseñas".
                //
                // La vista acota los tramos del histograma de respaldos: los de por defecto
                // están pensados para milisegundos de petición, y un respaldo que tarda
                // segundos o minutos caería entero en el último, dejando el percentil sin
                // ningún significado.
                _ = metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Microsoft.AspNetCore.RateLimiting")
                    .AddMeter(MetricasDeNegocio.NombreDelMedidor)
                    .AddView(
                        instrumentName: "gsp.respaldos.duracion",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = [1, 5, 15, 30, 60, 120, 300],
                        }
                    );

                if (exportar)
                {
                    _ = metrics.AddOtlpExporter();
                }
            });

        return services;
    }
}
