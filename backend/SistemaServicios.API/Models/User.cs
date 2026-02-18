using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } // UUID (Identificador único universal)

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; } // Hash de contraseña

        [Required]
        public string FirstName { get; set; } // Nombre separado

        [Required]
        public string LastName { get; set; } // Apellido separado

        [Required]
        public string Role { get; set; } // Admin, Cliente, Profesional

        public string? PhoneNumber { get; set; }

        public decimal AverageRating { get; set; } = 0;

        public bool Status { get; set; } = true;

        public string? ProfileImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}