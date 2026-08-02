using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Auth;

public class VerifyLoginPinDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression(@"^[0-9]{6}$")]
    public required string Pin { get; set; }
}