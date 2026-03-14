using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IServiceRequestRepository _repository;
    private readonly IServiceRepository _serviceRepository;

    public ServiceRequestService(
        IServiceRequestRepository repository,
        IServiceRepository serviceRepository
    )
    {
        _repository = repository;
        _serviceRepository = serviceRepository;
    }

    public async Task<ServiceRequestDto> CreateRequestAsync(
        Guid clientId,
        CreateServiceRequestDto dto
    )
    {
        var service = await _serviceRepository.GetByIdAsync(dto.ServiceId);
        if (service is null || !service.IsActive)
        {
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

        var withIncludes = await _repository.GetByIdAsync(created.Id);
        return MapToDto(withIncludes!);
    }

    public async Task<ServiceRequestDto> GetRequestByIdAsync(
        int id,
        Guid requesterId,
        string requesterRole
    )
    {
        var request = await _repository.GetByIdAsync(id);
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

        return MapToDto(request);
    }

    public async Task<(IEnumerable<ServiceRequestDto> requests, int totalCount)> GetMyRequestsAsync(
        Guid clientId,
        int page,
        int size
    )
    {
        var (requests, totalCount) = await _repository.GetByClientIdAsync(clientId, page, size);
        return (requests.Select(MapToDto), totalCount);
    }

    public async Task<(
        IEnumerable<ServiceRequestDto> requests,
        int totalCount
    )> GetProfessionalRequestsAsync(Guid professionalId, int page, int size)
    {
        var (requests, totalCount) = await _repository.GetByProfessionalIdAsync(
            professionalId,
            page,
            size
        );
        return (requests.Select(MapToDto), totalCount);
    }

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
            throw new UnauthorizedAccessException(
                "Solo el profesionista asignado o un Admin puede cambiar el estado."
            );
        }

        if (!IsValidTransition(request.Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Transición de estado inválida: {request.Status} → {newStatus}."
            );
        }

        request.Status = newStatus;

        if (newStatus == RequestStatus.Completed)
        {
            request.CompletionDate = DateTime.UtcNow;
        }

        await _repository.UpdateAsync(request);

        var withIncludes = await _repository.GetByIdAsync(request.Id);
        return MapToDto(withIncludes!);
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

    private static ServiceRequestDto MapToDto(Request r) =>
        new()
        {
            Id = r.Id,
            ClientId = r.ClientId,
            ClientName = r.Client is not null
                ? $"{r.Client.FirstName} {r.Client.LastName}"
                : string.Empty,
            ProfessionalId = r.ProfessionalId,
            ProfessionalName = r.Professional is not null
                ? $"{r.Professional.FirstName} {r.Professional.LastName}"
                : string.Empty,
            ServiceId = r.ServiceId,
            ServiceTitle = r.Service?.Title ?? string.Empty,
            QuotedPrice = r.QuotedPrice,
            Status = r.Status,
            Description = r.Description,
            RequestDate = r.RequestDate,
            ScheduledDate = r.ScheduledDate,
            CompletionDate = r.CompletionDate,
        };
}
