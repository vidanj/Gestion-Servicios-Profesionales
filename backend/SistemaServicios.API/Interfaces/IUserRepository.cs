using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IUserRepository
{
    public Task<(IEnumerable<User> users, int totalCount)> GetUsersAsync(int pageNumber, int pageSize);

    public Task<User?> GetByIdAsync(Guid id);

    public Task<User?> GetUserByEmailAsync(string email);

    public Task<User> AddUserAsync(User user);

    public Task UpdateUserAsync(User user);

    public Task<User?> GetByEmailAsync(string email);

    public Task<bool> EmailExistsAsync(string email);

    public Task<User> CreateAsync(User user);
}
