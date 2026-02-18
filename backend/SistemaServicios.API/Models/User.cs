using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models
{
    public enum UserRole
    {
        Admin = 0,
        Client = 1,
        Professional = 2
    }

    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        [MaxLength(255)]
        public required string FirstName { get; set; }
        [MaxLength(255)]
        public required string LastName { get; set; }
        public UserRole Role { get; set; } = UserRole.Client;
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AverageRating { get; set; } = 0;
        public bool Status { get; set; } = true;
        [MaxLength(2048)]
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}