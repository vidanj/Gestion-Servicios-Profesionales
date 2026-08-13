namespace SistemaServicios.API.Interfaces;

/// <summary>
/// Cola de trabajos en segundo plano. Existe para sacar del ciclo petición-respuesta
/// las operaciones de duración impredecible (pg_dump, SMTP) que hoy bloquean al usuario.
/// </summary>
public interface IBackgroundTaskDispatcher
{
    /// <summary>
    /// Encola un trabajo. Recibe un <see cref="IServiceProvider"/> propio porque se
    /// ejecuta fuera del ámbito de la petición HTTP y no puede reutilizar sus
    /// dependencias con ámbito.
    /// </summary>
    public ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, ValueTask> workItem);

    /// <summary>Espera hasta que haya un trabajo disponible y lo devuelve.</summary>
    public ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken
    );
}
