using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Services;

public class CreateServiceDto
{
    [Required(ErrorMessage = "La categoría es requerida.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "El título es requerido.")]
    [MaxLength(200, ErrorMessage = "El título no puede exceder 200 caracteres.")]
    public required string Title { get; set; }

    [MaxLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "El precio base es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio base debe ser mayor a 0.")]
    public decimal BasePrice { get; set; }

    [MaxLength(2048)]
    public string? ImageUrl { get; set; }
}
