using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IServiceRequestRepository
{
    public Task<Request> CreateAsync(Request request);

    public Task<Request?> GetByIdAsync(int id);

    public Task UpdateAsync(Request request);
}
