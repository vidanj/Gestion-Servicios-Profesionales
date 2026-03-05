using SistemaServicios.API.DTOs.Auth;

namespace SistemaServicios.API.Interfaces;

public interface IAuthService
{
    public Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);

    public Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
}
