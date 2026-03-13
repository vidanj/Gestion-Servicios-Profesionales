using SistemaServicios.API.Models;

namespace SistemaServicios.API.DTOs;

public class UserLogDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public LogAction Action { get; set; }

    public string? Detail { get; set; }

    public LogStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
