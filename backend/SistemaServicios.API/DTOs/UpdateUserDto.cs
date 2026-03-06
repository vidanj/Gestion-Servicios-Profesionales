using System.ComponentModel.DataAnnotations;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.DTOs;

public class UpdateUserDto
{
    [Required]
    [MaxLength(255)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(255)]
    public required string LastName { get; set; }

    public UserRole Role { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public bool Status { get; set; }
}
