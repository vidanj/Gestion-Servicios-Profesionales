using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Interfaces;
using StatusEnum = SistemaServicios.API.Models.RequestStatus;

namespace SistemaServicios.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Módulo de Solicitudes")]
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestService _requestService;

    public ServiceRequestsController(IServiceRequestService requestService)
    {
        _requestService = requestService;
    }

    /// <summary>Crear una nueva solicitud de servicio. El ClientId se extrae del JWT.</summary>
    [Authorize(Roles = "Client,Professional")]
    [HttpPost]
    public async Task<ActionResult<ServiceRequestDto>> CreateRequest(
        [FromBody] CreateServiceRequestDto dto
    )
    {
        try
        {
            var clientId = GetCurrentUserId();
            if (clientId is null)
            {
                return Unauthorized(new { message = "Token inválido o usuario no autenticado." });
            }

            var result = await _requestService.CreateRequestAsync(clientId.Value, dto);
            return CreatedAtAction(nameof(GetRequest), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }

    /// <summary>Obtener el detalle de una solicitud. Solo el cliente dueño, el profesional asignado o Admin.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceRequestDto>> GetRequest(int id)
    {
        try
        {
            var (requesterId, requesterRole) = GetRequesterInfo();
            if (requesterId is null)
            {
                return Unauthorized(new { message = "Token inválido o usuario no autenticado." });
            }

            var result = await _requestService.GetRequestByIdAsync(
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

    /// <summary>Solicitudes realizadas por el usuario autenticado (Cliente o Profesionista).</summary>
    [Authorize(Roles = "Client,Professional")]
    [HttpGet("my")]
    public async Task<ActionResult> GetMyRequests(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10
    )
    {
        try
        {
            var clientId = GetCurrentUserId();
            if (clientId is null)
            {
                return Unauthorized(new { message = "Token inválido o usuario no autenticado." });
            }

            var (requests, totalCount) = await _requestService.GetMyRequestsAsync(
                clientId.Value,
                page,
                size
            );
            return Ok(
                new
                {
                    data = requests,
                    totalCount,
                    page,
                    size,
                }
            );
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }

    /// <summary>Solicitudes recibidas por el profesionista autenticado.</summary>
    [Authorize(Roles = "Professional")]
    [HttpGet("professional")]
    public async Task<ActionResult> GetProfessionalRequests(
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

            var (requests, totalCount) = await _requestService.GetProfessionalRequestsAsync(
                professionalId.Value,
                page,
                size
            );
            return Ok(
                new
                {
                    data = requests,
                    totalCount,
                    page,
                    size,
                }
            );
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }

    /// <summary>Actualizar el estado de una solicitud. Solo el profesionista asignado o Admin.</summary>
    [Authorize(Roles = "Professional,Admin")]
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ServiceRequestDto>> UpdateStatus(
        int id,
        [FromQuery] StatusEnum newStatus
    )
    {
        try
        {
            var (requesterId, requesterRole) = GetRequesterInfo();
            if (requesterId is null)
            {
                return Unauthorized(new { message = "Token inválido o usuario no autenticado." });
            }

            var result = await _requestService.UpdateStatusAsync(
                id,
                newStatus,
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
