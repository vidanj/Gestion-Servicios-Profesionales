using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Auth;

public class LoginPinRequestDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}