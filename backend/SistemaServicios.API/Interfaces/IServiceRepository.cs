using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IServiceRepository
{
    public Task<(IEnumerable<Service> services, int totalCount)> GetAllAsync(
        int page,
        int size,
        int? categoryId,
        Guid? professionalId
    );

    public Task<Service?> GetByIdAsync(int id);

    public Task<(IEnumerable<Service> services, int totalCount)> GetByProfessionalIdAsync(
        Guid professionalId,
        int page,
        int size
    );

    public Task<Service> CreateAsync(Service service);

    public Task<Service> UpdateAsync(Service service);

    public Task<bool> ExistsAsync(int id);
}
