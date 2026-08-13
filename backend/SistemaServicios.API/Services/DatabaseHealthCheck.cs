using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SistemaServicios.API.Data;

namespace SistemaServicios.API.Services;

/// <summary>
/// Comprueba que PostgreSQL está alcanzable. Es la diferencia entre "el proceso vive"
/// y "el proceso puede atender peticiones": sin esta comprobación, una instancia con la
/// base caída seguiría recibiendo tráfico y fallando petición a petición.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    // Sin tope, una base que no responde dejaría la sonda colgada y el orquestador
    // interpretaría el tiempo de espera agotado en lugar de un estado claro.
    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(3);

    private readonly AppDbContext _context;

    public DatabaseHealthCheck(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TiempoMaximo);

        try
        {
            return await _context.Database.CanConnectAsync(cts.Token)
                ? HealthCheckResult.Healthy("La base de datos responde.")
                : HealthCheckResult.Unhealthy("La base de datos no acepta conexiones.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                "La comprobación de la base de datos agotó el tiempo de espera."
            );
        }
        catch (Exception ex)
        {
            // Una sonda nunca debe propagar: hacerlo convertiría un fallo de dependencia
            // en un 500 sin diagnóstico. La excepción viaja al log, no a la respuesta.
            return HealthCheckResult.Unhealthy("La base de datos no está disponible.", ex);
        }
    }
}
