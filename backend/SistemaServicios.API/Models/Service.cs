using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models
{
    public class Service
    {
        public int Id { get; set; }

        // Relación con el Profesional (Dueño del servicio) -> CAMBIADO A GUID
        [Required]
        public Guid ProfessionalId { get; set; }
        [ForeignKey("ProfessionalId")]
        public User? Professional { get; set; }

        // Relación con Categoría (Esta SE QUEDA como int, eso está bien)
        [Required]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}