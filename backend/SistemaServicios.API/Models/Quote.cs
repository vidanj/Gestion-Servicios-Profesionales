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
        public Guid ClientId { get; set; } 
        [ForeignKey("ClientId")]
        public User? Client { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedPrice { get; set; }

        public int EstimatedDuration { get; set; } 
        
        public string? Description { get; set; }
        public string Status { get; set; } = "Pending"; 

        public DateTime? ValidUntil { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}