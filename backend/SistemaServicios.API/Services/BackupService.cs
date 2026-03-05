using System.Diagnostics;
using SistemaServicios.API.DTOs.Admin;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

public class BackupService : IBackupService
{
    private readonly string _backupDir;

    public BackupService()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, ".env")))
        {
            dir = dir.Parent;
        }

        var repoRoot = dir?.FullName ?? Directory.GetCurrentDirectory();
        _backupDir = Path.Combine(repoRoot, "backups");
        Directory.CreateDirectory(_backupDir);
    }

    public async Task<BackupResponseDto> GenerateBackupAsync()
    {
        var host =
            Environment.GetEnvironmentVariable("DB_HOST")
            ?? throw new InvalidOperationException("DB_HOST no definido");
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var database =
            Environment.GetEnvironmentVariable("DB_NAME")
            ?? throw new InvalidOperationException("DB_NAME no definido");
        var username =
            Environment.GetEnvironmentVariable("DB_USER")
            ?? throw new InvalidOperationException("DB_USER no definido");
        var password =
            Environment.GetEnvironmentVariable("DB_PASSWORD")
            ?? throw new InvalidOperationException("DB_PASSWORD no definido");

        var timestamp = DateTime.Now.ToString(
            "yyyyMMdd_HHmm",
            System.Globalization.CultureInfo.InvariantCulture
        );
        var fileName = $"backup_{timestamp}.sql";
        var filePath = Path.Combine(_backupDir, fileName);

        var startInfo = new ProcessStartInfo
        {
            FileName = "pg_dump",
            Arguments =
                $"--host={host} --port={port} --username={username} --dbname={database} --format=plain --no-owner --no-acl --file=\"{filePath}\"",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Environment = { ["PGPASSWORD"] = password },
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new InvalidOperationException(
                "pg_dump no encontrado. Verifica que PostgreSQL esté instalado y que su carpeta bin esté en el PATH del sistema.",
                ex
            );
        }

        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"pg_dump falló (código {process.ExitCode}): {stderr}"
            );
        }

        var fileInfo = new FileInfo(filePath);

        return new BackupResponseDto
        {
            FileName = fileName,
            CreatedAt = fileInfo.CreationTimeUtc,
            FileSizeBytes = fileInfo.Length,
        };
    }
}
