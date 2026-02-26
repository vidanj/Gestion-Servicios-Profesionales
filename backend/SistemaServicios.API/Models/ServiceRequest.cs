using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaServicios.API.Enums; // <--- ¡ESTA LÍNEA ES LA CLAVE! 🔑

namespace SistemaServicios.API.Models
{
    public class ServiceRequest
    {
        [Key]
        public int Id { get; set; }

        // Quién solicita 
        [Required]
        public Guid ClientId { get; set; }
        
        // Relación con Usuario 
        [ForeignKey("ClientId")]
        public User? Client { get; set; }

        // Qué servicio solicita
        [Required]
        public int ServiceId { get; set; }
        
        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }

        // Estado actual de la solicitud
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        // Mensaje inicial del cliente 
        public string? ClientMessage { get; set; }

        // Notas del profesional 
        public string? ProfessionalNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}