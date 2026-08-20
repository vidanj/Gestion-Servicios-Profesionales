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

    /// <summary>
    /// Traza de la petición en la que ocurrió la acción, para enlazar esta entrada con
    /// las líneas de log de operación que dejó esa misma petición.
    /// </summary>
    /// <remarks>
    /// Es el puente entre las dos bitácoras: esta tabla responde <i>quién hizo qué</i> y
    /// el log de stdout responde <i>qué pasó por dentro</i>. Sin este campo son dos
    /// mundos incomunicados y, ante una reclamación, hay que adivinar qué líneas del log
    /// corresponden a la acción registrada aquí.
    /// Es anulable porque no toda escritura ocurre dentro de una petición —una tarea en
    /// segundo plano no tiene traza— y porque las filas anteriores a esta columna no la
    /// tienen. Un valor inventado sería peor que su ausencia.
    /// </remarks>
    [MaxLength(32)]
    public string? TraceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
