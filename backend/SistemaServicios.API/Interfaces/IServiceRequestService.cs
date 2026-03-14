using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IServiceRequestService
{
    public Task<ServiceRequestDto> CreateRequestAsync(Guid clientId, CreateServiceRequestDto dto);

    public Task<ServiceRequestDto> GetRequestByIdAsync(
        int id,
        Guid requesterId,
        string requesterRole
    );

    public Task<(IEnumerable<ServiceRequestDto> requests, int totalCount)> GetMyRequestsAsync(
        Guid clientId,
        int page,
        int size
    );

    public Task<(
        IEnumerable<ServiceRequestDto> requests,
        int totalCount
    )> GetProfessionalRequestsAsync(Guid professionalId, int page, int size);

    public Task<ServiceRequestDto> UpdateStatusAsync(
        int requestId,
        RequestStatus newStatus,
        Guid requesterId,
        string requesterRole
    );
}
