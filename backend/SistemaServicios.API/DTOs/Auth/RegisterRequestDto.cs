using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(6)]
    public required string Password { get; set; }

    [Required]
    [MaxLength(255)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(255)]
    public required string LastName { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}
