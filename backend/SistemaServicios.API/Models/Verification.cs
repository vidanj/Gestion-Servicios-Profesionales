using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models
{
    public class Verification
    {
        public int Id { get; set; }

        // CAMBIO DE int A Guid AQUÍ:
        [Required]
        public Guid ProfessionalId { get; set; }
        
        [ForeignKey("ProfessionalId")]
        public User? Professional { get; set; }

        public string DocumentUrl { get; set; } = string.Empty; // URL de la INE, Título, etc.

        public string DocumentType { get; set; } = "INE"; // INE, Cedula, Antecedentes

        public bool IsVerified { get; set; } = false;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
    }
}