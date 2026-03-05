using SistemaServicios.API.DTOs.Admin;

namespace SistemaServicios.API.Interfaces;

public interface IBackupService
{
    public Task<BackupResponseDto> GenerateBackupAsync();
}
