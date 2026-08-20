namespace SistemaServicios.API.DTOs.Auth;

public record ResetPasswordRequestDto(string Token, string NewPassword);
