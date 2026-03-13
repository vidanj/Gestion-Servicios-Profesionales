using FluentAssertions;
using SistemaServicios.API.DTOs.Pricing;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class PricingServiceTests
{
    private readonly PriceEstimatorService _service = new();

    // ─────────────────────────────────────────────────────────────
    // Complejidad
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateEstimateComplejidadBajaNoAplicaModificador()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 100m,
            ComplexityLevel = "baja",
            UrgencyLevel = "normal",
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.ComplexityModifierPercent.Should().Be(0m);
        _ = result.EstimatedTotal.Should().Be(100m);
    }

    [Fact]
    public void CalculateEstimateComplejidadMediaAplica15Porciento()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 100m,
            ComplexityLevel = "media",
            UrgencyLevel = "normal",
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.ComplexityModifierPercent.Should().Be(0.15m);
        _ = result.EstimatedTotal.Should().Be(115m);
    }

    [Fact]
    public void CalculateEstimateComplejidadAltaAplica30Porciento()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 100m,
            ComplexityLevel = "alta",
            UrgencyLevel = "normal",
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.ComplexityModifierPercent.Should().Be(0.30m);
        _ = result.EstimatedTotal.Should().Be(130m);
    }

    // ─────────────────────────────────────────────────────────────
    // Urgencia
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateEstimateUrgenciaNormalNoAplicaModificador()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 100m,
            ComplexityLevel = "baja",
            UrgencyLevel = "normal",
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.UrgencyModifierPercent.Should().Be(0m);
    }

    [Fact]
    public void CalculateEstimateUrgenciaUrgenteAplica20Porciento()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 100m,
            ComplexityLevel = "baja",
            UrgencyLevel = "urgente",
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.UrgencyModifierPercent.Should().Be(0.20m);
        _ = result.EstimatedTotal.Should().Be(120m);
    }

    [Fact]
    public void CalculateEstimateUrgenciaExpressAplica40Porciento()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 100m,
            ComplexityLevel = "baja",
            UrgencyLevel = "express",
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.UrgencyModifierPercent.Should().Be(0.40m);
        _ = result.EstimatedTotal.Should().Be(140m);
    }

    // ─────────────────────────────────────────────────────────────
    // Revisiones extra
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateEstimateSinRevisionesExtraNoAplicaModificador()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 100m,
            ComplexityLevel = "baja",
            UrgencyLevel = "normal",
            ExtraRevisions = 0,
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.RevisionsModifierPercent.Should().Be(0m);
    }

    [Fact]
    public void CalculateEstimateDosRevisionesExtraAplica10Porciento()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 100m,
            ComplexityLevel = "baja",
            UrgencyLevel = "normal",
            ExtraRevisions = 2,
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.RevisionsModifierPercent.Should().Be(0.10m);
        _ = result.EstimatedTotal.Should().Be(110m);
    }

    // ─────────────────────────────────────────────────────────────
    // Extras fijos
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateEstimateConSoportePrioritarioAgregaCargaFija35()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 100m,
            ComplexityLevel = "baja",
            UrgencyLevel = "normal",
            IncludePrioritySupport = true,
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.PrioritySupportFee.Should().Be(35m);
        _ = result.EstimatedTotal.Should().Be(135m);
    }

    [Fact]
    public void CalculateEstimateConEntregaFinDeSemanaAgregaCargaFija20()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 100m,
            ComplexityLevel = "baja",
            UrgencyLevel = "normal",
            IncludeWeekendDelivery = true,
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.WeekendDeliveryFee.Should().Be(20m);
        _ = result.EstimatedTotal.Should().Be(120m);
    }

    // ─────────────────────────────────────────────────────────────
    // Combinaciones
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateEstimateTodosLosModificadoresActivosCalculaTotalCorrecto()
    {
        // Arrange: base=200, alta(+30%)=60, express(+40%)=80, 2rev(+10%)=20, soporte=35, fds=20
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 200m,
            ComplexityLevel = "alta",
            UrgencyLevel = "express",
            ExtraRevisions = 2,
            IncludePrioritySupport = true,
            IncludeWeekendDelivery = true,
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.EstimatedTotal.Should().Be(415m); // 200 + 160 + 35 + 20
    }

    [Fact]
    public void CalculateEstimateSinModificadoresTotalIgualAlPrecioBase()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 250m,
            ComplexityLevel = "baja",
            UrgencyLevel = "normal",
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.EstimatedTotal.Should().Be(250m);
        _ = result.BasePrice.Should().Be(250m);
    }

    // ─────────────────────────────────────────────────────────────
    // Notes
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateEstimateSiempreDevuelveNotas()
    {
        // Arrange
        var request = new PriceEstimateRequestDto
        {
            BasePrice = 100m,
            ComplexityLevel = "media",
            UrgencyLevel = "urgente",
        };

        // Act
        var result = _service.CalculateEstimate(request);

        // Assert
        _ = result.Notes.Should().NotBeEmpty();
        _ = result.Notes.Should().Contain(n => n.Contains("media"));
        _ = result.Notes.Should().Contain(n => n.Contains("urgente"));
    }
}
