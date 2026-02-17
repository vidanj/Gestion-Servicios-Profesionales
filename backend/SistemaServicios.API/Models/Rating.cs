using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models
{
    public class Rating
    {
        public int Id { get; set; }

        // Relación: A qué solicitud pertenece esta calificación
        [Required]
        public int RequestId { get; set; }
        [ForeignKey("RequestId")]
        public Request? Request { get; set; }

        // Relación: Quién escribe la reseña (Cliente)
        [Required]
        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public User? Client { get; set; }

        // Relación: A quién están calificando (Profesional)
        [Required]
        public int ProfessionalId { get; set; }
        [ForeignKey("ProfessionalId")]
        public User? Professional { get; set; }

        [Range(1, 5)]
        public int Score { get; set; } // Estrellas (1 a 5)

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}