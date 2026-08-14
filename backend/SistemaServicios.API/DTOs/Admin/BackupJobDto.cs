using System.Text.Json.Serialization;

namespace SistemaServicios.API.DTOs.Admin;

/// <summary>
/// Estados de un trabajo de respaldo.
/// </summary>
/// <remarks>
/// El converter va sobre este enum y no en la configuración global de JSON a propósito.
/// Aplicarlo globalmente cambiaría también <c>UserRole</c>, <c>RequestStatus</c> y los
/// demás enums de la API, que hoy viajan como enteros y así los leen el login, el perfil
/// y el panel de usuarios. Ese cambio rompería esos módulos de golpe.
/// Sin el converter, el estado viajaba como número y el cliente, que compara contra
/// nombres, no reconocía nunca el estado final: la pantalla sondeaba hasta agotar el tope
/// mientras el respaldo ya estaba hecho.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<BackupJobStatus>))]
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
