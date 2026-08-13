using System.Globalization;
using System.Text.RegularExpressions;
using SistemaServicios.API.DTOs.Admin;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

public partial class BackupService : IBackupService
{
    private readonly IProcessRunner _processRunner;
    private readonly string _backupDir;

    public BackupService(IProcessRunner processRunner, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        _processRunner = processRunner;

        // El destino se inyecta por configuracion (BACKUP_DIR). Antes se descubria
        // subiendo por el arbol de directorios hasta encontrar un .env, lo que en el
        // contenedor no encuentra nada y termina escribiendo en /app/backups, que
        // desaparece con el contenedor.
        _backupDir = config["BackupSettings:Directory"] is { Length: > 0 } configurado
            ? configurado
            : Path.Combine(Directory.GetCurrentDirectory(), "backups");
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

        // El directorio se crea aqui y no en el constructor: crear carpetas al
        // resolver la dependencia es un efecto secundario en tiempo de arranque.
        _ = Directory.CreateDirectory(_backupDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
        var fileName = $"backup_{timestamp}.sql";
        var filePath = Path.Combine(_backupDir, fileName);

        var result = await _processRunner.RunAsync(
            "pg_dump",
            $"--host={host} --port={port} --username={username} --dbname={database} "
                + $"--format=plain --no-owner --no-acl --file=\"{filePath}\"",
            new Dictionary<string, string>(StringComparer.Ordinal) { ["PGPASSWORD"] = password }
        );

        if (!result.ExecutableFound)
        {
            throw new InvalidOperationException(
                "pg_dump no encontrado. Verifica que PostgreSQL esté instalado y que su carpeta bin esté en el PATH del sistema."
            );
        }

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"pg_dump falló (código {result.ExitCode}): {result.StandardError}"
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

    public IReadOnlyList<BackupResponseDto> ListBackups()
    {
        if (!Directory.Exists(_backupDir))
        {
            return [];
        }

        return
        [
            .. new DirectoryInfo(_backupDir)
                .GetFiles("*.sql")
                .Where(f => NombreDeRespaldoValido().IsMatch(f.Name))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Select(f => new BackupResponseDto
                {
                    FileName = f.Name,
                    CreatedAt = f.CreationTimeUtc,
                    FileSizeBytes = f.Length,
                }),
        ];
    }

    public Stream? OpenBackup(string fileName)
    {
        // Capa 1 — lista blanca por patron exacto. Solo pasan nombres que este mismo
        // servicio pudo haber generado, lo que descarta separadores de ruta, "..",
        // rutas absolutas y flujos alternativos de datos (NTFS) sin enumerarlos.
        if (string.IsNullOrEmpty(fileName) || !NombreDeRespaldoValido().IsMatch(fileName))
        {
            return null;
        }

        // Capa 2 — verificacion canonica. Redundante frente a la capa 1, pero sostiene
        // la garantia si el patron se relaja en el futuro, y cubre el caso de que el
        // propio directorio de respaldos contenga un enlace simbolico hacia fuera.
        var baseDir = Path.GetFullPath(_backupDir);
        var fullPath = Path.GetFullPath(Path.Combine(baseDir, fileName));

        if (!fullPath.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return null;
        }

        return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
    }

    // Patron exacto de los nombres que produce GenerateBackupAsync: backup_yyyyMMdd_HHmm.sql
    [GeneratedRegex(@"^backup_\d{8}_\d{4}\.sql$", RegexOptions.CultureInvariant)]
    private static partial Regex NombreDeRespaldoValido();
}
