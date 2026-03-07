using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Auth;

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}
