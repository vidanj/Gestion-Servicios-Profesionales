using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.DTOs.Services;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Tags("Módulo de Servicios")]
public class ServicesController : ControllerBase
{
    private readonly IServiceService _serviceService;

    public ServicesController(IServiceService serviceService)
    {
        _serviceService = serviceService;
    }

    /// <summary>Listado público de servicios activos con filtros opcionales.</summary>
    [HttpGet]
    public async Task<ActionResult> GetServices(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] int? categoryId = null,
        [FromQuery] Guid? professionalId = null
    )
    {
        try
        {
            var (services, totalCount) = await _serviceService.GetServicesAsync(
                page,
                size,
                categoryId,
                professionalId
            );
            return Ok(new { data = services, totalCount, page, size });
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }

    /// <summary>Detalle de un servicio por ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceDto>> GetService(int id)
    {
        try
        {
            var service = await _serviceService.GetServiceByIdAsync(id);
            return Ok(service);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }

    /// <summary>Servicios propios del profesionista autenticado (activos e inactivos).</summary>
    [Authorize(Roles = "Professional")]
    [HttpGet("my")]
    public async Task<ActionResult> GetMyServices(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10
    )
    {
        try
        {
            var professionalId = GetCurrentUserId();
            if (professionalId is null)
            {
                return Unauthorized(new { message = "Token inválido o usuario no autenticado." });
            }

            var (services, totalCount) = await _serviceService.GetMyServicesAsync(
                professionalId.Value,
                page,
                size
            );
            return Ok(new { data = services, totalCount, page, size });
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }

    /// <summary>Crear un nuevo servicio (solo Profesionista).</summary>
    [Authorize(Roles = "Professional")]
    [HttpPost]
    public async Task<ActionResult<ServiceDto>> CreateService(CreateServiceDto dto)
    {
        try
        {
            var professionalId = GetCurrentUserId();
            if (professionalId is null)
            {
                return Unauthorized(new { message = "Token inválido o usuario no autenticado." });
            }

            var created = await _serviceService.CreateServiceAsync(professionalId.Value, dto);
            return CreatedAtAction(nameof(GetService), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }

    /// <summary>Editar un servicio. El Profesionista solo puede editar los suyos; el Admin puede editar cualquiera.</summary>
    [Authorize(Roles = "Professional,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServiceDto>> UpdateService(int id, UpdateServiceDto dto)
    {
        try
        {
            var (requesterId, requesterRole) = GetRequesterInfo();
            if (requesterId is null)
            {
                return Unauthorized(new { message = "Token inválido o usuario no autenticado." });
            }

            var updated = await _serviceService.UpdateServiceAsync(
                id,
                requesterId.Value,
                requesterRole!,
                dto
            );
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }

    /// <summary>Activar o desactivar un servicio.</summary>
    [Authorize(Roles = "Professional,Admin")]
    [HttpPatch("{id:int}/toggle")]
    public async Task<ActionResult<ServiceDto>> ToggleService(int id)
    {
        try
        {
            var (requesterId, requesterRole) = GetRequesterInfo();
            if (requesterId is null)
            {
                return Unauthorized(new { message = "Token inválido o usuario no autenticado." });
            }

            var result = await _serviceService.ToggleActiveAsync(
                id,
                requesterId.Value,
                requesterRole!
            );
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }

    /// <summary>Eliminar (desactivar) un servicio. El Profesionista solo puede eliminar los suyos; el Admin puede eliminar cualquiera.</summary>
    [Authorize(Roles = "Professional,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        try
        {
            var (requesterId, requesterRole) = GetRequesterInfo();
            if (requesterId is null)
            {
                return Unauthorized(new { message = "Token inválido o usuario no autenticado." });
            }

            await _serviceService.DeleteServiceAsync(id, requesterId.Value, requesterRole!);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }

    private Guid? GetCurrentUserId()
    {
        string? claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out Guid id) ? id : null;
    }

    private (Guid? id, string? role) GetRequesterInfo()
    {
        string? claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        string? role = User.FindFirst(ClaimTypes.Role)?.Value;
        return Guid.TryParse(claim, out Guid id) ? (id, role) : (null, null);
    }
}
