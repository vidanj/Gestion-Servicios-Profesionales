using SistemaServicios.API.DTOs;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<(IEnumerable<UserDto> Users, int TotalCount)> GetAllUsersAsync(
        int page,
        int size
    )
    {
        var (users, total) = await _userRepository.GetUsersAsync(page, size);
        var dtos = users.Select(u => MapToDto(u));
        return (dtos, total);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        // Validar unicidad de email
        var existingUser = await _userRepository.GetUserByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new Exception("El correo ya estÃ¡ registrado.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            Role = dto.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), // EncriptaciÃ³n bÃ¡sica
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _userRepository.AddUserAsync(user);
        return MapToDto(user);
    }

    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return false;

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.PhoneNumber = dto.PhoneNumber;
        user.Role = dto.Role;
        user.Status = dto.Status;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateUserAsync(user);
        return true;
    }

    public async Task<bool> SoftDeleteUserAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return false;

        // Borrado lÃ³gico
        user.Status = false;
        await _userRepository.UpdateUserAsync(user);
        return true;
    }

    private static UserDto MapToDto(User u) =>
        new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Role = u.Role,
            PhoneNumber = u.PhoneNumber,
            AverageRating = u.AverageRating,
            Status = u.Status,
            CreatedAt = u.CreatedAt,
        };
}
