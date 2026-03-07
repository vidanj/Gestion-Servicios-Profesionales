using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Interfaces;
using StatusEnum = SistemaServicios.API.Enums.RequestStatus;

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
        var result = await _requestService.CreateRequestAsync(requestDto);
        return Ok(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromQuery] StatusEnum newStatus,
        [FromQuery] int changedBy,
        [FromQuery] string? comments
    )
    {
        try
        {
            var result = await _requestService.UpdateStatusAsync(
                id,
                newStatus,
                changedBy,
                comments
            );
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
