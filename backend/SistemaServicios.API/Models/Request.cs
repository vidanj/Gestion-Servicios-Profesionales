using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models
{
    public class Request
    {
        public int Id { get; set; }

        // Relación: Quién solicita (El Cliente)
        [Required]
        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public User? Client { get; set; }

        // Relación: A quién contratan (El Profesional)
        [Required]
        public int ProfessionalId { get; set; }
        [ForeignKey("ProfessionalId")]
        public User? Professional { get; set; }

        // Relación: Qué servicio se contrató
        [Required]
        public int ServiceId { get; set; }
        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Completed, Cancelled

        [Column(TypeName = "decimal(18,2)")]
        public decimal QuotedPrice { get; set; } // Precio final acordado

        public string? Description { get; set; } // Notas del trabajo

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? ScheduledDate { get; set; } // Para cuándo es
        public DateTime? CompletionDate { get; set; } // Cuándo terminó
    }
}