using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models
{
    public enum QuoteStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Expired = 3
    }

    public class Quote
    {
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }
        public Service? Service { get; set; } 

        [Required]
        public Guid ClientId { get; set; }
        public User? Client { get; set; }      

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedPrice { get; set; }
        public int EstimatedDuration { get; set; }
        [MaxLength(1000)]
        public string? Description { get; set; }
        public QuoteStatus Status { get; set; } = QuoteStatus.Pending;
        public DateTime? ValidUntil { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}