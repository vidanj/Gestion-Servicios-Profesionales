using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models;

public enum LogStatus
{
    Exitoso = 0,
    Alerta = 1,
    Error = 2,
}

public enum LogAction
{
    CambioContrasena = 0,
    ActualizacionRol = 1,
    IntentoAcceso = 2,
    ActualizacionPerfil = 3,
    ExportacionReportes = 4,
    SuspensionUsuario = 5,
    CreacionUsuario = 6,
    EliminacionUsuario = 7,
    CreacionServicio = 8,
    ActualizacionServicio = 9,
    EliminacionServicio = 10,
}

public class UserLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public LogAction Action { get; set; }

    [MaxLength(500)]
    public string? Detail { get; set; }

    public LogStatus Status { get; set; } = LogStatus.Exitoso;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
