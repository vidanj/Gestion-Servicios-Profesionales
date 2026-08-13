using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Controllers;

// Solo accesible para usuarios con rol Admin
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public partial class AdminController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly IBackgroundTaskDispatcher _queue;
    private readonly IBackupJobTracker _jobTracker;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IBackupService backupService,
        IBackgroundTaskDispatcher queue,
        IBackupJobTracker jobTracker,
        ILogger<AdminController> logger
    )
    {
        _backupService = backupService;
        _queue = queue;
        _jobTracker = jobTracker;
        _logger = logger;
    }

    /// <summary>
    /// Encola la generación de un respaldo y responde de inmediato. Requiere rol Admin.
    /// </summary>
    [HttpPost("backup")]
    public async Task<IActionResult> CreateBackup()
    {
        var (job, created) = _jobTracker.StartOrGetActive();

        if (!created)
        {
            // Ya hay uno en curso: se devuelve ese en lugar de lanzar un segundo
            // pg_dump en paralelo contra la misma base.
            return Accepted(job);
        }

        await _queue.EnqueueAsync(
            async (serviceProvider, _) =>
            {
                var tracker = serviceProvider.GetRequiredService<IBackupJobTracker>();
                var backups = serviceProvider.GetRequiredService<IBackupService>();

                tracker.MarkRunning(job.Id);

                try
                {
                    var result = await backups.GenerateBackupAsync();
                    tracker.MarkCompleted(job.Id, result.FileName, result.FileSizeBytes);
                }
                catch (InvalidOperationException ex)
                {
                    // El detalle interno queda en el estado del trabajo y en el log,
                    // no en una respuesta HTTP.
                    tracker.MarkFailed(job.Id, "No se pudo generar el respaldo.");
                    LogFalloDeRespaldo(ex);
                }
            }
        );

        return Accepted(job);
    }

    /// <summary>
    /// Consulta el estado de un trabajo de respaldo. Requiere rol Admin.
    /// </summary>
    [HttpGet("backup/jobs/{id:guid}")]
    public IActionResult GetBackupJob(Guid id)
    {
        var job = _jobTracker.Find(id);
        return job is null ? NotFound(new { message = "El trabajo no existe." }) : Ok(job);
    }

    /// <summary>
    /// Lista los respaldos disponibles, del mas reciente al mas antiguo. Requiere rol Admin.
    /// </summary>
    [HttpGet("backups")]
    public IActionResult ListBackups()
    {
        return Ok(_backupService.ListBackups());
    }

    /// <summary>
    /// Descarga un respaldo por nombre de archivo. Requiere rol Admin.
    /// </summary>
    [HttpGet("backups/{fileName}")]
    public IActionResult DownloadBackup(string fileName)
    {
        var stream = _backupService.OpenBackup(fileName);

        if (stream is null)
        {
            // Mismo 404 para "nombre invalido" y "no existe": distinguirlos permitiria
            // sondear que rutas hay en el servidor.
            return NotFound(new { message = "El respaldo solicitado no existe." });
        }

        return File(stream, "application/octet-stream", fileName);
    }

    // Delegado generado en compilacion: CA1848 desaconseja LoggerExtensions.LogError directo.
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Error,
        Message = "Falló la generación del respaldo de base de datos"
    )]
    private partial void LogFalloDeRespaldo(Exception exception);
}
