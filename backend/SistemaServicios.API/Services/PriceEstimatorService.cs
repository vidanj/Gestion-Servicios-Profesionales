using SistemaServicios.API.DTOs.Pricing;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

public class PriceEstimatorService : IPriceEstimatorService
{
    private const decimal MediumComplexityPercent = 0.15m;
    private const decimal HighComplexityPercent = 0.30m;

    private const decimal UrgentPercent = 0.20m;
    private const decimal ExpressPercent = 0.40m;

    private const decimal RevisionPercentPerUnit = 0.05m;
    private const decimal PrioritySupportFixedFee = 35m;
    private const decimal WeekendDeliveryFixedFee = 20m;

    public PriceEstimateResponseDto CalculateEstimate(PriceEstimateRequestDto request)
    {
        // Validación simple para no romper cálculos si llega texto inesperado.
        var complexity = request.ComplexityLevel.Trim().ToLowerInvariant();
        var urgency = request.UrgencyLevel.Trim().ToLowerInvariant();

        var complexityPercent = complexity switch
        {
            "media" => MediumComplexityPercent,
            "alta" => HighComplexityPercent,
            _ => 0m,
        };

        var urgencyPercent = urgency switch
        {
            "urgente" => UrgentPercent,
            "express" => ExpressPercent,
            _ => 0m,
        };

        var revisionsPercent = request.ExtraRevisions * RevisionPercentPerUnit;
        var supportFee = request.IncludePrioritySupport ? PrioritySupportFixedFee : 0m;
        var weekendFee = request.IncludeWeekendDelivery ? WeekendDeliveryFixedFee : 0m;

        // Multiplicadores porcentuales aplicados sobre el precio base.
        var percentageAmount =
            request.BasePrice * (complexityPercent + urgencyPercent + revisionsPercent);
        var estimatedTotal = request.BasePrice + percentageAmount + supportFee + weekendFee;

        return new PriceEstimateResponseDto
        {
            BasePrice = request.BasePrice,
            ComplexityModifierPercent = complexityPercent,
            UrgencyModifierPercent = urgencyPercent,
            RevisionsModifierPercent = revisionsPercent,
            PrioritySupportFee = supportFee,
            WeekendDeliveryFee = weekendFee,
            EstimatedTotal = decimal.Round(estimatedTotal, 2),
            Notes = BuildNotes(complexity, urgency, request.ExtraRevisions),
        };
    }

    private static List<string> BuildNotes(string complexity, string urgency, int extraRevisions)
    {
        var notes = new List<string>
        {
            $"Complejidad elegida: {complexity}.",
            $"Urgencia elegida: {urgency}.",
            $"Revisiones extra solicitadas: {extraRevisions}.",
            "Este cálculo es estimado y puede cambiar según negociación final.",
        };

        return notes;
    }
}
