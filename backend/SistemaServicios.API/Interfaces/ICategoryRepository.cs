using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface ICategoryRepository
{
    public Task<IEnumerable<Category>> GetAllActiveAsync();

    public Task<Category?> GetByIdAsync(int id);

    public Task<bool> ExistsAsync(int id, bool mustBeActive = false);
}
