using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SistemaServicios.API.Extensions;

/// <summary>
/// Escribe el resultado de las sondas como JSON.
/// Deliberadamente no serializa la excepción ni los datos del origen: la respuesta es
/// anónima y filtrar la cadena de conexión o el host de la base ahí sería una fuga.
/// El detalle queda en el log.
/// </summary>
public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds),
            checks = report
                .Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds),
                    description = entry.Value.Description,
                })
                .ToList(),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
