using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

/// <summary>
/// Consume la cola de trabajos en segundo plano. Cada trabajo se ejecuta en su propio
/// ámbito de dependencias, porque el de la petición HTTP ya no existe cuando corre.
/// </summary>
public partial class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskDispatcher _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBackupJobTracker _jobTracker;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(
        IBackgroundTaskDispatcher queue,
        IServiceScopeFactory scopeFactory,
        IBackupJobTracker jobTracker,
        ILogger<QueuedHostedService> logger
    )
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _jobTracker = jobTracker;
        _logger = logger;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Sin esto, un respaldo interrumpido por el apagado quedaría en EnProceso
        // indefinidamente y el administrador no podría lanzar otro.
        _jobTracker.FailActiveJobs("El servicio se detuvo antes de completar el trabajo.");
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<IServiceProvider, CancellationToken, ValueTask> workItem;

            try
            {
                workItem = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Apagado normal del proceso.
                break;
            }

            using var scope = _scopeFactory.CreateScope();

            try
            {
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Un trabajo que falla no debe tumbar al consumidor: se registra y se sigue.
                LogFalloDeTrabajo(ex);
            }
        }
    }

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Error,
        Message = "Falló un trabajo en segundo plano"
    )]
    private partial void LogFalloDeTrabajo(Exception exception);
}
