using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Pricing;

public class PriceEstimateRequestDto
{
    [Range(1, 1000000)]
    public decimal BasePrice { get; set; }

    [Required]
    public required string ComplexityLevel { get; set; }

    [Required]
    public required string UrgencyLevel { get; set; }

    [Range(0, 10)]
    public int ExtraRevisions { get; set; }

    public bool IncludePrioritySupport { get; set; }

    public bool IncludeWeekendDelivery { get; set; }
}
