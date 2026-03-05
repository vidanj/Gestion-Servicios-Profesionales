using SistemaServicios.API.DTOs.Auth;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

// Fat Model: toda la lÃ³gica de negocio de autenticaciÃ³n vive aquÃ­.
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepo, ITokenService tokenService)
    {
        _userRepo = userRepo;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user =
            await _userRepo.GetByEmailAsync(dto.Email)
            ?? throw new UnauthorizedAccessException("Credenciales invÃ¡lidas.");

        if (!user.Status)
            throw new UnauthorizedAccessException("La cuenta estÃ¡ desactivada.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales invÃ¡lidas.");

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        if (await _userRepo.EmailExistsAsync(dto.Email))
            throw new InvalidOperationException("El correo ya estÃ¡ registrado.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Role = UserRole.Client,
            PhoneNumber = dto.PhoneNumber?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _userRepo.CreateAsync(user);

        return BuildAuthResponse(user);
    }

    private AuthResponseDto BuildAuthResponse(User user) =>
        new()
        {
            Token = _tokenService.CreateToken(user),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
        };
}
