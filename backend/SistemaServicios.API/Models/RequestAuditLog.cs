using System.ComponentModel.DataAnnotations;
using SistemaServicios.API.Enums;

namespace SistemaServicios.API.Models;

public class RequestAuditLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ServiceRequestId { get; set; }

    public RequestStatus? OldStatus { get; set; }

    [Required]
    public RequestStatus NewStatus { get; set; }

    [Required]
    public int ChangedBy { get; set; } // El ID del usuario que hizo el cambio

    [MaxLength(500)]
    public string? Comments { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
