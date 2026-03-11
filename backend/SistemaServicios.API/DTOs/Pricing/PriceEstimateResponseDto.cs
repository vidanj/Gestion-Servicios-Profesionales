namespace SistemaServicios.API.DTOs.Pricing;

public class PriceEstimateResponseDto
{
    public decimal BasePrice { get; set; }

    public decimal ComplexityModifierPercent { get; set; }

    public decimal UrgencyModifierPercent { get; set; }

    public decimal RevisionsModifierPercent { get; set; }

    public decimal PrioritySupportFee { get; set; }

    public decimal WeekendDeliveryFee { get; set; }

    public decimal EstimatedTotal { get; set; }

    public List<string> Notes { get; set; } = new();
}
