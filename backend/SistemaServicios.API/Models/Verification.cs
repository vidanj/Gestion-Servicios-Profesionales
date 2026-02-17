using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models
{
    public class Verification
    {
        public int Id { get; set; }

        [Required]
        public int ProfessionalId { get; set; }
        [ForeignKey("ProfessionalId")]
        public User? Professional { get; set; }

        public string VerificationType { get; set; } = string.Empty; // INE, Titulo, Certificado
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        
        public string? ExternalReference { get; set; } // Número de folio o ID del documento
        public string? DocumentUrl { get; set; } // Foto del documento

        public DateTime? VerifiedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}