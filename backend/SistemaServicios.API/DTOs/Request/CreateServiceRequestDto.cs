using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Requests;

public class CreateServiceRequestDto
{
    [Required]
    public int ClientId { get; set; }

    [Required]
    public int ProfessionalId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
}
