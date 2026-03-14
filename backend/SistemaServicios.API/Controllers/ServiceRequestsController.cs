using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Interfaces;
using StatusEnum = SistemaServicios.API.Models.RequestStatus;

namespace SistemaServicios.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestService _requestService;

    public ServiceRequestsController(IServiceRequestService requestService)
    {
        _requestService = requestService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateServiceRequestDto requestDto)
    {
        try
        {
            var result = await _requestService.CreateRequestAsync(requestDto);
            return Ok(result);
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

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Professional,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] StatusEnum newStatus)
    {
        try
        {
            var result = await _requestService.UpdateStatusAsync(id, newStatus);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
