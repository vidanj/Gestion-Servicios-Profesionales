using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.DTOs;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserLogsController : ControllerBase
{
    private readonly IUserLogService _service;

    public UserLogsController(IUserLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int size = 50,
        [FromQuery] LogStatus? status = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] LogAction? action = null
    )
    {
        var (logs, total) = await _service.GetLogsAsync(page, size, status, userId, action);
        return Ok(
            new
            {
                Total = total,
                Page = page,
                Size = size,
                Data = logs,
            }
        );
    }

    [HttpPost]
    public async Task<ActionResult<UserLogDto>> CreateLog(CreateUserLogDto dto)
    {
        var log = await _service.CreateLogAsync(dto);
        return CreatedAtAction(nameof(GetLogs), new { id = log.Id }, log);
    }
}
