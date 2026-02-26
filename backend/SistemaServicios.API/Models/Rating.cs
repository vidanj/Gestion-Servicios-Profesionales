using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models
{
    public class Rating
    {
        [Key]
        public int Id { get; set; }

        // Puntuación de 1 a 5 estrellas
        [Range(1, 5)]
        public int Score { get; set; }

        public string? Comment { get; set; }

        // Quién califica (El Cliente)
        public Guid ClientId { get; set; }
        [ForeignKey("ClientId")]
        public User? Client { get; set; }

        // A quién califican (El Profesional)
        public Guid ProfessionalId { get; set; }
        [ForeignKey("ProfessionalId")]
        public User? Professional { get; set; }

        public int ServiceRequestId { get; set; }
        
        [ForeignKey("ServiceRequestId")]
        public ServiceRequest? ServiceRequest { get; set; }
    }
}