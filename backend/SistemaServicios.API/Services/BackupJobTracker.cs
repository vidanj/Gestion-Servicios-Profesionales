using System.Collections.Concurrent;
using SistemaServicios.API.DTOs.Admin;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

public class BackupJobTracker : IBackupJobTracker
{
    private readonly ConcurrentDictionary<Guid, BackupJobDto> _jobs = new();

    // La comprobación de "hay uno activo" y la creación deben ser atómicas entre sí:
    // sin el candado, dos peticiones simultáneas crearían dos trabajos.
    private readonly Lock _gate = new();

    public (BackupJobDto job, bool created) StartOrGetActive()
    {
        lock (_gate)
        {
            var activo = _jobs.Values.FirstOrDefault(j =>
                j.Status is BackupJobStatus.Pendiente or BackupJobStatus.EnProceso
            );

            if (activo is not null)
            {
                return (activo, false);
            }

            var nuevo = new BackupJobDto
            {
                Id = Guid.NewGuid(),
                Status = BackupJobStatus.Pendiente,
                CreatedAt = DateTime.UtcNow,
            };

            _jobs[nuevo.Id] = nuevo;
            return (nuevo, true);
        }
    }

    public BackupJobDto? Find(Guid id) => _jobs.TryGetValue(id, out var job) ? job : null;

    public void MarkRunning(Guid id)
    {
        if (_jobs.TryGetValue(id, out var job))
        {
            job.Status = BackupJobStatus.EnProceso;
        }
    }

    public void MarkCompleted(Guid id, string fileName, long fileSizeBytes)
    {
        if (_jobs.TryGetValue(id, out var job))
        {
            job.Status = BackupJobStatus.Completado;
            job.FileName = fileName;
            job.FileSizeBytes = fileSizeBytes;
            job.FinishedAt = DateTime.UtcNow;
        }
    }

    public void MarkFailed(Guid id, string reason)
    {
        if (_jobs.TryGetValue(id, out var job))
        {
            job.Status = BackupJobStatus.Fallido;
            job.Error = reason;
            job.FinishedAt = DateTime.UtcNow;
        }
    }

    public void FailActiveJobs(string reason)
    {
        lock (_gate)
        {
            foreach (
                var job in _jobs.Values.Where(j =>
                    j.Status is BackupJobStatus.Pendiente or BackupJobStatus.EnProceso
                )
            )
            {
                job.Status = BackupJobStatus.Fallido;
                job.Error = reason;
                job.FinishedAt = DateTime.UtcNow;
            }
        }
    }
}
