using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces
{
    public interface IUserRepository
    {
        Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(int pageNumber, int pageSize);
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByEmailAsync(string email); 
        Task<User> AddUserAsync(User user);
        Task UpdateUserAsync(User user);

        Task<User?> GetByEmailAsync(string email);
        Task<bool> EmailExistsAsync(string email);
        Task<User> CreateAsync(User user);
    }
}