using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IServiceRequestRepository
{
    public Task<Request> CreateAsync(Request request);

    public Task<Request?> GetByIdAsync(int id);

    public Task<(IEnumerable<Request> requests, int totalCount)> GetByClientIdAsync(
        Guid clientId,
        int page,
        int size
    );

    public Task<(IEnumerable<Request> requests, int totalCount)> GetByProfessionalIdAsync(
        Guid professionalId,
        int page,
        int size
    );

    public Task UpdateAsync(Request request);
}
