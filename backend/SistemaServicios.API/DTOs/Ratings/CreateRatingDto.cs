using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Ratings
{
    public class CreateRatingDto
    {
        [Required(ErrorMessage = "El ID de la solicitud es obligatorio.")]
        public int RequestId { get; set; }

        [Required(ErrorMessage = "El ID del profesional es obligatorio.")]
        public Guid ProfessionalId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "La calificación debe estar estrictamente entre 1 y 5.")]
        public int Score { get; set; }

        [MaxLength(500, ErrorMessage = "El comentario no puede exceder los 500 caracteres.")]
        public string? Comment { get; set; }
    }
}