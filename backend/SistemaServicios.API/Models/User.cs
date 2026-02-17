using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Client"; // Client, Professional, Admin

        // Nuevos campos V2
        public string? PhoneNumber { get; set; }
        public decimal AverageRating { get; set; } = 0;
        public bool Status { get; set; } = true;
        public string? ProfileImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}