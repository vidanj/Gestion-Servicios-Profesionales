using SistemaServicios.API.DTOs.Auth;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

// Fat Model: toda la lógica de negocio de autenticación vive aquí.
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    public AuthService(
        IUserRepository userRepo,
        ITokenService tokenService,
        IEmailService emailService
    )
    {
        _userRepo = userRepo;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user =
            await _userRepo.GetByEmailAsync(dto.Email)
            ?? throw new UnauthorizedAccessException("Credenciales inválidas.");

        if (!user.Status)
        {
            throw new UnauthorizedAccessException("La cuenta está desactivada.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        if (await _userRepo.EmailExistsAsync(dto.Email))
        {
            throw new InvalidOperationException("El correo ya está registrado.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email.ToLower(System.Globalization.CultureInfo.CurrentCulture).Trim(),
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

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto dto)
    {
        var user =
            await _userRepo.GetByEmailAsync(dto.Email)
            ?? throw new InvalidOperationException(
                "Si el correo está registrado, recibirás tu nueva contraseña en breve."
            );

        if (!user.Status)
        {
            throw new InvalidOperationException("La cuenta está desactivada.");
        }

        var newPassword = GenerateSecurePassword();

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 10);
        await _userRepo.UpdateUserAsync(user);

        await _emailService.SendPasswordResetEmailAsync(user.Email, newPassword);
    }

    private static string GenerateSecurePassword()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$";
        var bytes = new byte[12];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
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
