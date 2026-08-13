using SistemaServicios.API.DTOs.Admin;

namespace SistemaServicios.API.Interfaces;

public interface IBackupService
{
    /// <summary>Genera un volcado de la base de datos con pg_dump.</summary>
    public Task<BackupResponseDto> GenerateBackupAsync();

    /// <summary>
    /// Lista los respaldos disponibles, del mas reciente al mas antiguo.
    /// Devuelve lista vacia si el directorio aun no existe.
    /// </summary>
    public IReadOnlyList<BackupResponseDto> ListBackups();

    /// <summary>
    /// Abre un respaldo para lectura.
    /// Devuelve <c>null</c> si el nombre no es valido o el archivo no existe: el
    /// llamador no debe poder distinguir ambos casos, para no revelar que rutas existen.
    /// </summary>
    public Stream? OpenBackup(string fileName);
}
