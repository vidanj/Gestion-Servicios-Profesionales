using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Requests;

public class CreateServiceRequestDto
{
    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public Guid ProfessionalId { get; set; }

    [Required]
    public int ServiceId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
}
