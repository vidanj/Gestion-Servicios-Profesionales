using SistemaServicios.API.DTOs;

namespace SistemaServicios.API.Interfaces
{
    public interface IUserService
    {
        Task<(IEnumerable<UserDto> Users, int TotalCount)> GetAllUsersAsync(int page, int size);
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<bool> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
        Task<bool> SoftDeleteUserAsync(Guid id);
    }
}