using FluentAssertions;
using Moq;
using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Comprueba que el servicio de solicitudes cuenta las creaciones y las transiciones.
/// </summary>
public class ServiceRequestServiceMetricasTests
{
    private readonly Mock<IServiceRequestRepository> _solicitudes = new();
    private readonly Mock<IServiceRepository> _servicios = new();
    private readonly Mock<IMetricasDeNegocio> _metricas = new();
    private readonly ServiceRequestService _servicio;

    public ServiceRequestServiceMetricasTests()
    {
        _servicio = new ServiceRequestService(
            _solicitudes.Object,
            _servicios.Object,
            _metricas.Object
        );
    }

    [Fact]
    public async Task CrearSolicitudSobreServicioActivoCuentaLaCreacion()
    {
        _servicios.Setup(s => s.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(ServicioActivo());
        _solicitudes
            .Setup(r => r.CreateAsync(It.IsAny<Request>()))
            .ReturnsAsync(new Request { Id = 7 });
        _solicitudes
            .Setup(r => r.GetRequestDtoByIdAsync(7))
            .ReturnsAsync(new ServiceRequestDto { Id = 7 });

        _ = await _servicio.CreateRequestAsync(
            Guid.NewGuid(),
            new CreateServiceRequestDto { ServiceId = 1, Description = "Algo" }
        );

        _metricas.Verify(m => m.SolicitudCreada(true), Times.Once);
    }

    /// <summary>
    /// No es un error del sistema: es un cliente pidiendo un servicio dado de baja. Si esta
    /// cuenta crece, el catálogo que ve el cliente dejó de coincidir con el que acepta el
    /// servidor, y eso ninguna métrica técnica lo mostraría.
    /// </summary>
    [Fact]
    public async Task CrearSolicitudSobreServicioInactivoCuentaElRechazo()
    {
        var inactivo = ServicioActivo();
        inactivo.IsActive = false;
        _servicios.Setup(s => s.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(inactivo);

        var accion = () =>
            _servicio.CreateRequestAsync(
                Guid.NewGuid(),
                new CreateServiceRequestDto { ServiceId = 1, Description = "Algo" }
            );

        _ = await accion.Should().ThrowAsync<KeyNotFoundException>();
        _metricas.Verify(m => m.SolicitudCreada(false), Times.Once);
    }

    [Fact]
    public async Task TransicionValidaCuentaAceptadaConOrigenYDestino()
    {
        var profesional = Guid.NewGuid();
        _solicitudes
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(
                new Request
                {
                    Id = 5,
                    ProfessionalId = profesional,
                    Status = RequestStatus.Pending,
                }
            );
        _solicitudes
            .Setup(r => r.GetRequestDtoByIdAsync(5))
            .ReturnsAsync(new ServiceRequestDto { Id = 5 });

        _ = await _servicio.UpdateStatusAsync(
            5,
            RequestStatus.Accepted,
            profesional,
            "Professional"
        );

        _metricas.Verify(
            m =>
                m.CambioDeEstado(
                    RequestStatus.Pending,
                    RequestStatus.Accepted,
                    ResultadoDeCambioDeEstado.Aceptada
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Se cuenta con el estado de origen todavía sin sobrescribir. Si la métrica se emitiera
    /// después de asignar el nuevo estado, origen y destino serían el mismo valor y la serie
    /// no diría nada.
    /// </summary>
    [Fact]
    public async Task TransicionInvalidaCuentaRechazadaConLaTransicionIntentada()
    {
        var profesional = Guid.NewGuid();
        _solicitudes
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(
                new Request
                {
                    Id = 5,
                    ProfessionalId = profesional,
                    Status = RequestStatus.Completed,
                }
            );

        var accion = () =>
            _servicio.UpdateStatusAsync(5, RequestStatus.Pending, profesional, "Professional");

        _ = await accion.Should().ThrowAsync<InvalidOperationException>();
        _metricas.Verify(
            m =>
                m.CambioDeEstado(
                    RequestStatus.Completed,
                    RequestStatus.Pending,
                    ResultadoDeCambioDeEstado.Rechazada
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task CambioPedidoPorQuienNoDebeCuentaNoAutorizada()
    {
        _solicitudes
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(
                new Request
                {
                    Id = 5,
                    ProfessionalId = Guid.NewGuid(),
                    Status = RequestStatus.Pending,
                }
            );

        var accion = () =>
            _servicio.UpdateStatusAsync(5, RequestStatus.Accepted, Guid.NewGuid(), "Professional");

        _ = await accion.Should().ThrowAsync<UnauthorizedAccessException>();
        _metricas.Verify(
            m =>
                m.CambioDeEstado(
                    RequestStatus.Pending,
                    RequestStatus.Accepted,
                    ResultadoDeCambioDeEstado.NoAutorizada
                ),
            Times.Once
        );
    }

    private static Service ServicioActivo() =>
        new()
        {
            Id = 1,
            ProfessionalId = Guid.NewGuid(),
            Title = "Servicio de prueba",
            BasePrice = 100m,
            IsActive = true,
        };
}
