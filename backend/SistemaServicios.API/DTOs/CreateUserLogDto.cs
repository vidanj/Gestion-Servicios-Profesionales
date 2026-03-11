using SistemaServicios.API.Models;

namespace SistemaServicios.API.DTOs;

public class CreateUserLogDto
{
    public Guid UserId { get; set; }

    public LogAction Action { get; set; }

    public string? Detail { get; set; }

    public LogStatus Status { get; set; } = LogStatus.Exitoso;
}
