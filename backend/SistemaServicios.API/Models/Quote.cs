using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models
{
    public class Quote
    {
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }
        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }

        [Required]
        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public User? Client { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedPrice { get; set; }

        public int EstimatedDuration { get; set; } // Horas o días estimados
        
        public string? Description { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected

        public DateTime? ValidUntil { get; set; } // Fecha límite de la cotización
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}