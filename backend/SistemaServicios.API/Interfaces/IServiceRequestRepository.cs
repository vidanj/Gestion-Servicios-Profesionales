using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IServiceRequestRepository
{
    public Task<Request> CreateAsync(Request request);

    public Task<Request?> GetByIdAsync(int id);

    public Task UpdateAsync(Request request);

    public Task<ServiceRequestDto?> GetRequestDtoByIdAsync(int id);

    public Task<(IEnumerable<ServiceRequestDto> requests, int totalCount)> GetDtosByClientIdAsync(
        Guid clientId,
        int page,
        int size
    );

    public Task<(
        IEnumerable<ServiceRequestDto> requests,
        int totalCount
    )> GetDtosByProfessionalIdAsync(Guid professionalId, int page, int size);
}
