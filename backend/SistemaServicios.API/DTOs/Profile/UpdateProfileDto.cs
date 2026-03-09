using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Profile;

public class UpdateProfileDto
{
    [Required]
    [MaxLength(255)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(255)]
    public required string LastName { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}
