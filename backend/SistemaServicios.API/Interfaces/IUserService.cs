using SistemaServicios.API.DTOs;

namespace SistemaServicios.API.Interfaces;

public interface IUserService
{
    public Task<(IEnumerable<UserDto> users, int totalCount)> GetAllUsersAsync(int page, int size);

    public Task<UserDto?> GetUserByIdAsync(Guid id);

    public Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);

    public Task<bool> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);

    public Task<bool> SoftDeleteUserAsync(Guid id);
}
