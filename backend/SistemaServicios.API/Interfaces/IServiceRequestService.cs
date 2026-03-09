using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IServiceRequestService
{
    public Task<Request> CreateRequestAsync(CreateServiceRequestDto requestDto);

    public Task<Request> UpdateStatusAsync(int requestId, RequestStatus newStatus);
}
