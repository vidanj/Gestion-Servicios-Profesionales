using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Auth;

public record ResetPasswordRequestDto(
    [Required] string Token,
    [Required]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        string NewPassword
);
