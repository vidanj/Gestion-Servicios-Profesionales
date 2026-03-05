using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models;

public class Rating
{
    public int Id { get; set; }

    [Required]
    public int RequestId { get; set; }
    public Request? Request { get; set; }

    [Required]
    public Guid ClientId { get; set; }
    public User? Client { get; set; }

    [Required]
    public Guid ProfessionalId { get; set; }
    public User? Professional { get; set; }

    [Range(1, 5)]
    public int Score { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
