using SistemaServicios.API.DTOs.Auth;

namespace SistemaServicios.API.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    }
}
