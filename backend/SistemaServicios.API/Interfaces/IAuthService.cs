using SistemaServicios.API.DTOs.Auth;

namespace SistemaServicios.API.Interfaces;

public interface IAuthService
{
    public Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);

    public Task RequestLoginPinAsync(LoginPinRequestDto dto);

    public Task<AuthResponseDto> VerifyLoginPinAsync(VerifyLoginPinDto dto);

    public Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);

    public Task ForgotPasswordAsync(ForgotPasswordRequestDto dto);
}
