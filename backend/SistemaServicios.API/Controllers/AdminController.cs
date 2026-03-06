using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Controllers;

// Solo accesible para usuarios con rol Admin
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IBackupService _backupService;

    public AdminController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    /// <summary>
    /// Genera un respaldo de la base de datos y lo guarda en la carpeta backups/.
    /// Requiere rol Admin.
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
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
