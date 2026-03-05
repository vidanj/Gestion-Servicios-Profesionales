using System.ComponentModel.DataAnnotations;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.DTOs;

public class CreateUserDto
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required, MinLength(6)]
    public required string Password { get; set; }

    [Required, MaxLength(255)]
    public required string FirstName { get; set; }

    [Required, MaxLength(255)]
    public required string LastName { get; set; }

    public UserRole Role { get; set; } = UserRole.Client;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}
