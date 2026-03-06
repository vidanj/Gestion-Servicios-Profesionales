namespace SistemaServicios.API.DTOs.Admin;

public class BackupResponseDto
{
    public string FileName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public long FileSizeBytes { get; set; }
}
