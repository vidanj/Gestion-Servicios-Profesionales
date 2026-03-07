using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Enums;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IServiceRequestService
{
    public Task<ServiceRequest> CreateRequestAsync(CreateServiceRequestDto requestDto);

    public Task<ServiceRequest> UpdateStatusAsync(
        int requestId,
        Enums.RequestStatus newStatus,
        int changedByUserId,
        string? comments
    );
}
