using SistemaServicios.API.DTOs.Pricing;

namespace SistemaServicios.API.Interfaces;

public interface IPriceEstimatorService
{
    public PriceEstimateResponseDto CalculateEstimate(PriceEstimateRequestDto request);
}
