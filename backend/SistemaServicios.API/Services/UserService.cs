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

    public async Task<(IEnumerable<UserDto> users, int totalCount)> GetAllUsersAsync(
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

    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        // Validar unicidad de email
        var existingUser = await _userRepository.GetUserByEmailAsync(createUserDto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("El correo ya está registrado.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = createUserDto.Email,
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            PhoneNumber = createUserDto.PhoneNumber,
            Role = createUserDto.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password), // Encriptación básica
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _userRepository.AddUserAsync(user);
        return MapToDto(user);
    }

    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        user.FirstName = updateUserDto.FirstName;
        user.LastName = updateUserDto.LastName;
        user.PhoneNumber = updateUserDto.PhoneNumber;
        user.Role = updateUserDto.Role;
        user.Status = updateUserDto.Status;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateUserAsync(user);
        return true;
    }

    public async Task<bool> SoftDeleteUserAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        // Borrado lógico
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
