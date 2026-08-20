using System.Security.Cryptography;
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
        var user = await _userRepo.GetByEmailAsync(dto.Email);

        // Homogeneizamos el error: Si no existe el usuario, o si la contraseña falla,
        // o si está desactivado, siempre devolvemos el mismo mensaje genérico.
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        if (!user.Status)
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        if (await _userRepo.EmailExistsAsync(dto.Email))
        {
            // Nota: En registro es un estándar aceptado indicar si el correo está en uso
            // por motivos de usabilidad, pero si se requiere máxima seguridad estricta,
            // se usaría un flujo de confirmación por correo. Lo dejaremos así para no romper el flujo.
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
        var user = await _userRepo.GetByEmailAsync(dto.Email);

        // Cuenta inexistente y cuenta desactivada se tratan igual y en silencio para evitar enumeración.
        if (user is null || !user.Status)
        {
            // Hash de descarte para mitigar ataques de temporización (timing attacks)
            _ = BCrypt.Net.BCrypt.HashPassword(GenerateSecureResetToken(), workFactor: 10);
            return;
        }

        var resetToken = GenerateSecureResetToken();
        user.PasswordResetToken = resetToken;
        user.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateUserAsync(user);

        // Despacha el correo con el token temporal sin tocar la contraseña actual
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
        {
            throw new InvalidOperationException(
                "El token de recuperación es inválido o ha expirado."
            );
        }

        var user = await _userRepo.GetByResetTokenAsync(dto.Token);

        if (
            user is null
            || !user.Status
            || user.ResetTokenExpiresAt is null
            || user.ResetTokenExpiresAt < DateTime.UtcNow
        )
        {
            throw new InvalidOperationException(
                "El token de recuperación es inválido o ha expirado."
            );
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.ResetTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateUserAsync(user);
    }

    private static string GenerateSecureResetToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
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
