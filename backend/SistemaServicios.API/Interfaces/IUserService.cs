using SistemaServicios.API.DTOs;
using SistemaServicios.API.DTOs.Profile;

namespace SistemaServicios.API.Interfaces;

public interface IUserService
{
    public Task<(IEnumerable<UserDto> users, int totalCount)> GetAllUsersAsync(int page, int size);

    public Task<UserDto?> GetUserByIdAsync(Guid id);

    public Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);

    public Task<bool> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);

    public Task<bool> SoftDeleteUserAsync(Guid id);

    public Task<UserDto?> UpdateOwnProfileAsync(Guid userId, UpdateProfileDto dto);

    public Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);

    public Task<UserDto?> UpdateProfileImageAsync(Guid userId, IFormFile foto);

    public Task<IEnumerable<UserRegistrationStatDto>> GetRegistrationsByDateAsync(int days);
}
