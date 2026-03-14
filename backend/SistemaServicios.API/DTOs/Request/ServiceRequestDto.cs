using SistemaServicios.API.Models;

namespace SistemaServicios.API.DTOs.Requests;

public class ServiceRequestDto
{
    public int Id { get; set; }

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public Guid ProfessionalId { get; set; }

    public string ProfessionalName { get; set; } = string.Empty;

    public int ServiceId { get; set; }

    public string ServiceTitle { get; set; } = string.Empty;

    public decimal QuotedPrice { get; set; }

    public RequestStatus Status { get; set; }

    public string? Description { get; set; }

    public DateTime RequestDate { get; set; }

    public DateTime? ScheduledDate { get; set; }

    public DateTime? CompletionDate { get; set; }
}
