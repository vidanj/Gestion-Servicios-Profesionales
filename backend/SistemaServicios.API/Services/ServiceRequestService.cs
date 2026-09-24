using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IServiceRequestRepository _repository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IMetricasDeNegocio _metricas;

    public ServiceRequestService(
        IServiceRequestRepository repository,
        IServiceRepository serviceRepository,
        IMetricasDeNegocio metricas
    )
    {
        _repository = repository;
        _serviceRepository = serviceRepository;
        _metricas = metricas;
    }

    public async Task<ServiceRequestDto> CreateRequestAsync(
        Guid clientId,
        CreateServiceRequestDto dto
    )
    {
        var service = await _serviceRepository.GetByIdAsync(dto.ServiceId);
        if (service is null || !service.IsActive)
        {
            // No es un error del sistema: es un cliente pidiendo un servicio dado de baja.
            // Si esta cuenta crece, el catálogo que ve el cliente dejó de coincidir con el
            // que acepta el servidor.
            _metricas.SolicitudCreada(creada: false);
            throw new KeyNotFoundException(
                $"El servicio con ID {dto.ServiceId} no existe o no está disponible."
            );
        }

        var request = new Request
        {
            ClientId = clientId,
            ProfessionalId = service.ProfessionalId,
            ServiceId = service.Id,
            QuotedPrice = service.BasePrice,
            Description = dto.Description,
            ScheduledDate = dto.ScheduledDate,
            Status = RequestStatus.Pending,
            RequestDate = DateTime.UtcNow,
        };

        var created = await _repository.CreateAsync(request);

        _metricas.SolicitudCreada(creada: true);
        return (await _repository.GetRequestDtoByIdAsync(created.Id))!;
    }

    public async Task<ServiceRequestDto> GetRequestByIdAsync(
        int id,
        Guid requesterId,
        string requesterRole
    )
    {
        var request = await _repository.GetRequestDtoByIdAsync(id);
        if (request is null)
        {
            throw new KeyNotFoundException($"Solicitud con ID {id} no encontrada.");
        }

        var isOwner = request.ClientId == requesterId || request.ProfessionalId == requesterId;
        if (requesterRole != "Admin" && !isOwner)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para consultar esta solicitud."
            );
        }

        return request;
    }

    public async Task<(IEnumerable<ServiceRequestDto> requests, int totalCount)> GetMyRequestsAsync(
        Guid clientId,
        int page,
        int size
    ) => await _repository.GetDtosByClientIdAsync(clientId, page, size);

    public async Task<(
        IEnumerable<ServiceRequestDto> requests,
        int totalCount
    )> GetProfessionalRequestsAsync(Guid professionalId, int page, int size) =>
        await _repository.GetDtosByProfessionalIdAsync(professionalId, page, size);

    public async Task<ServiceRequestDto> UpdateStatusAsync(
        int requestId,
        RequestStatus newStatus,
        Guid requesterId,
        string requesterRole
    )
    {
        var request = await _repository.GetByIdAsync(requestId);
        if (request is null)
        {
            throw new KeyNotFoundException("Solicitud no encontrada.");
        }

        if (requesterRole != "Admin" && request.ProfessionalId != requesterId)
        {
            _metricas.CambioDeEstado(
                request.Status,
                newStatus,
                ResultadoDeCambioDeEstado.NoAutorizada
            );
            throw new UnauthorizedAccessException(
                "Solo el profesionista asignado o un Admin puede cambiar el estado."
            );
        }

        if (!IsValidTransition(request.Status, newStatus))
        {
            // Un volumen apreciable de transiciones rechazadas no es tráfico hostil:
            // significa que la interfaz está ofreciendo transiciones que el servidor no
            // admite. Es un defecto de producto que ninguna métrica técnica mostraría.
            _metricas.CambioDeEstado(
                request.Status,
                newStatus,
                ResultadoDeCambioDeEstado.Rechazada
            );
            throw new InvalidOperationException(
                $"Transición de estado inválida: {request.Status} → {newStatus}."
            );
        }

        // Se cuenta con el estado de origen todavía sin sobrescribir; después de la
        // asignación de abajo, origen y destino serían el mismo valor.
        _metricas.CambioDeEstado(request.Status, newStatus, ResultadoDeCambioDeEstado.Aceptada);

        request.Status = newStatus;

        if (newStatus == RequestStatus.Completed)
        {
            request.CompletionDate = DateTime.UtcNow;
        }

        await _repository.UpdateAsync(request);
        return (await _repository.GetRequestDtoByIdAsync(request.Id))!;
    }

    private static bool IsValidTransition(RequestStatus current, RequestStatus next)
    {
        if (current == RequestStatus.Pending)
        {
            return next == RequestStatus.Accepted || next == RequestStatus.Cancelled;
        }

        if (current == RequestStatus.Accepted)
        {
            return next == RequestStatus.InProgress || next == RequestStatus.Cancelled;
        }

        if (current == RequestStatus.InProgress)
        {
            return next == RequestStatus.Completed || next == RequestStatus.Cancelled;
        }

        return false;
    }
}
