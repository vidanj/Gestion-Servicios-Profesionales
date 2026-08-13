using System.Threading.Channels;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

/// <summary>
/// Cola en memoria sobre <see cref="Channel{T}"/>. No añade infraestructura externa,
/// con la contrapartida de que los trabajos pendientes se pierden si el proceso muere.
/// Es aceptable para respaldos y correos de recuperación, que el usuario puede repetir.
/// </summary>
public class BackgroundTaskDispatcher : IBackgroundTaskDispatcher
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, ValueTask>> _queue;

    public BackgroundTaskDispatcher(int capacity = 100)
    {
        // Espera en vez de descartar cuando la cola se llena: perder un respaldo o un
        // correo en silencio sería peor que tardar un momento en aceptarlo.
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
        };

        _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, ValueTask>>(
            options
        );
    }

    public async ValueTask EnqueueAsync(
        Func<IServiceProvider, CancellationToken, ValueTask> workItem
    )
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken
    )
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
