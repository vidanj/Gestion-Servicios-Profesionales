using SistemaServicios.API.DTOs.Auth;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services
{
    // Fat Model: toda la lógica de negocio de autenticación vive aquí.
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
            var user = await _userRepo.GetByEmailAsync(dto.Email)
                ?? throw new UnauthorizedAccessException("Credenciales inválidas.");

            if (!user.Status)
                throw new UnauthorizedAccessException("La cuenta está desactivada.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Credenciales inválidas.");

            return BuildAuthResponse(user);
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
        {
            if (await _userRepo.EmailExistsAsync(dto.Email))
                throw new InvalidOperationException("El correo ya está registrado.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Role = dto.Role,
                PhoneNumber = dto.PhoneNumber?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _userRepo.CreateAsync(user);

            return BuildAuthResponse(user);
        }

        private AuthResponseDto BuildAuthResponse(User user) => new()
        {
            Token = _tokenService.CreateToken(user),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
        };
    }
}
