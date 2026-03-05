using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models;

public enum RequestStatus
{
    Pending = 0,
    Accepted = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
}

public class Request
{
    public int Id { get; set; }

    [Required]
    public Guid ClientId { get; set; }
    public User? Client { get; set; }

    [Required]
    public Guid ProfessionalId { get; set; }
    public User? Professional { get; set; }

    [Required]
    public int ServiceId { get; set; }
    public Service? Service { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    [Column(TypeName = "decimal(18,2)")]
    public decimal QuotedPrice { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletionDate { get; set; }
}
