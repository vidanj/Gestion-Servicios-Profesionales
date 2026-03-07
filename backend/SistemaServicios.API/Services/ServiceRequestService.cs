using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using StatusEnum = SistemaServicios.API.Enums.RequestStatus;

namespace SistemaServicios.API.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IServiceRequestRepository _repository;

    public ServiceRequestService(IServiceRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceRequest> CreateRequestAsync(CreateServiceRequestDto requestDto)
    {
        var newRequest = new ServiceRequest
        {
            ClientId = requestDto.ClientId,
            ProfessionalId = requestDto.ProfessionalId,
            Title = requestDto.Title,
            Description = requestDto.Description,
            Status = (Models.RequestStatus)StatusEnum.Pending,
        };

        var createdRequest = await _repository.CreateAsync(newRequest);

        await _repository.AddAuditLogAsync(
            new RequestAuditLog
            {
                ServiceRequestId = createdRequest.Id,
                OldStatus = null,
                NewStatus = (Models.RequestStatus)StatusEnum.Pending,
                ChangedBy = requestDto.ClientId,
                Comments = "Solicitud creada por el cliente",
            }
        );

        return createdRequest;
    }

    public async Task<ServiceRequest> UpdateStatusAsync(
        int requestId,
        StatusEnum newStatus,
        int changedByUserId,
        string? comments
    )
    {
        var request =
            await _repository.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Solicitud no encontrada.");

        var oldStatus = (StatusEnum)request.Status;

        if (!IsValidTransition(oldStatus, newStatus))
        {
            throw new InvalidOperationException("Transición de estado inválida.");
        }

        request.Status = (Models.RequestStatus)newStatus;
        request.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(request);

        await _repository.AddAuditLogAsync(
            new RequestAuditLog
            {
                ServiceRequestId = requestId,
                OldStatus = (Models.RequestStatus?)oldStatus,
                NewStatus = (Models.RequestStatus)newStatus,
                ChangedBy = changedByUserId,
                Comments = comments,
            }
        );

        return request;
    }

    private static bool IsValidTransition(StatusEnum current, StatusEnum next)
    {
        if (current == StatusEnum.Pending)
        {
            return next == StatusEnum.Accepted
                || next == StatusEnum.Rejected
                || next == StatusEnum.Cancelled;
        }

        if (current == StatusEnum.Accepted)
        {
            return next == StatusEnum.Completed || next == StatusEnum.Cancelled;
        }

        return false;
    }
}
