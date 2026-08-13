using SistemaServicios.API.DTOs.Admin;

namespace SistemaServicios.API.Interfaces;

/// <summary>
/// Registro del estado de los trabajos de respaldo. Es singleton y vive en memoria:
/// el estado se pierde al reiniciar, lo que es aceptable porque el respaldo en sí
/// queda en disco y se lista con el endpoint de respaldos.
/// </summary>
public interface IBackupJobTracker
{
    /// <summary>
    /// Devuelve el trabajo activo si ya hay uno, o crea uno nuevo. El segundo elemento
    /// indica si se creó: evita que dos peticiones lancen dos pg_dump concurrentes.
    /// </summary>
    public (BackupJobDto job, bool created) StartOrGetActive();

    public BackupJobDto? Find(Guid id);

    public void MarkRunning(Guid id);

    public void MarkCompleted(Guid id, string fileName, long fileSizeBytes);

    public void MarkFailed(Guid id, string reason);

    /// <summary>
    /// Marca como fallidos los trabajos sin terminar. Se llama al apagar el proceso
    /// para que ninguno quede en EnProceso para siempre.
    /// </summary>
    public void FailActiveJobs(string reason);
}
