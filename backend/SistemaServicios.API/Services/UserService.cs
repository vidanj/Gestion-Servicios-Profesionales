using SistemaServicios.API.DTOs;
using SistemaServicios.API.DTOs.Profile;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

public class UserService : IUserService
{
    private const long MaxImageSizeBytes = 2 * 1024 * 1024; // 2 MB

    private static readonly string[] AllowedImageMimeTypes = ["image/jpeg", "image/png"];

    private readonly IUserRepository _userRepository;
    private readonly IWebHostEnvironment _env;

    public UserService(IUserRepository userRepository, IWebHostEnvironment env)
    {
        _userRepository = userRepository;
        _env = env;
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

    public async Task<UserDto?> UpdateOwnProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.PhoneNumber = dto.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateUserAsync(user);
        return MapToDto(user);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("La contraseña actual es incorrecta.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateUserAsync(user);
        return true;
    }

    public async Task<UserDto?> UpdateProfileImageAsync(Guid userId, IFormFile foto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        if (!AllowedImageMimeTypes.Contains(foto.ContentType))
        {
            throw new InvalidOperationException("Solo se permiten imágenes en formato JPG o PNG.");
        }

        if (foto.Length > MaxImageSizeBytes)
        {
            throw new InvalidOperationException("La imagen no puede superar los 2 MB.");
        }

        var ext = foto.ContentType == "image/png" ? ".png" : ".jpg";
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{userId}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await foto.CopyToAsync(stream);
        }

        user.ProfileImageUrl = $"/uploads/avatars/{fileName}";
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateUserAsync(user);
        return MapToDto(user);
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
            ProfileImageUrl = u.ProfileImageUrl,
            CreatedAt = u.CreatedAt,
        };
}
