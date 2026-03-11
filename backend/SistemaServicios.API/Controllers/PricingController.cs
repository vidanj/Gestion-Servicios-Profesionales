using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.DTOs.Pricing;
using SistemaServicios.API.Services;

namespace SistemaServicios.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class PricingController : ControllerBase
{
    private readonly PriceEstimatorService _priceEstimatorService = new();

    [HttpPost("estimate")]
    public ActionResult<PriceEstimateResponseDto> Estimate(
        [FromBody] PriceEstimateRequestDto request
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = _priceEstimatorService.CalculateEstimate(request);
        return Ok(result);
    }
}
