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
    private readonly ILogger<AdminController> _logger;

    public AdminController(IBackupService backupService, ILogger<AdminController> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    /// <summary>
    /// Genera un respaldo de la base de datos. Requiere rol Admin.
    /// </summary>
    [HttpPost("backup")]
    public async Task<IActionResult> CreateBackup()
    {
        try
        {
            var result = await _backupService.GenerateBackupAsync();
            return StatusCode(201, result);
        }
        catch (InvalidOperationException ex)
        {
            // El detalle va al log, no a la respuesta: ex.Message arrastra rutas
            // internas del contenedor y la salida cruda de pg_dump.
            LogFalloDeRespaldo(ex);
            return StatusCode(
                500,
                new { message = "No se pudo generar el respaldo de la base de datos." }
            );
        }
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
