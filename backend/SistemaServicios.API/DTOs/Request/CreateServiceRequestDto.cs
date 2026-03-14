using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Requests;

public class CreateServiceRequestDto
{
    [Required(ErrorMessage = "El servicio es requerido.")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "La descripción es requerida.")]
    [MaxLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres.")]
    public required string Description { get; set; }

    public DateTime? ScheduledDate { get; set; }
}
