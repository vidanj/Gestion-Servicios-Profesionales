namespace SistemaServicios.API.DTOs.Admin;

public enum BackupJobStatus
{
    Pendiente = 0,
    EnProceso = 1,
    Completado = 2,
    Fallido = 3,
}

/// <summary>
/// Estado de un trabajo de respaldo. Sustituye a la espera síncrona: el cliente recibe
/// el identificador y consulta el avance en lugar de mantener la petición abierta.
/// </summary>
public class BackupJobDto
{
    public Guid Id { get; set; }

    public BackupJobStatus Status { get; set; }

    /// <summary>Nombre del archivo generado. Solo con estado Completado.</summary>
    public string? FileName { get; set; }

    public long? FileSizeBytes { get; set; }

    /// <summary>Motivo del fallo, apto para mostrarse al administrador.</summary>
    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? FinishedAt { get; set; }
}
