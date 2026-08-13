using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

/// <summary>
/// Guarda los archivos en wwwroot. Conserva el comportamiento previo a la migración
/// a base de datos y solo debe usarse en desarrollo: el disco del contenedor es
/// efímero y no se comparte entre réplicas.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private static readonly Dictionary<string, string> ExtensionByContentType = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
    };

    private readonly IWebHostEnvironment _env;

    public LocalFileStorage(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveForOwnerAsync(
        Guid ownerUserId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(content);

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        _ = Directory.CreateDirectory(uploadsDir);

        var extension = ExtensionByContentType.TryGetValue(contentType, out var known)
            ? known
            : ".bin";

        // El nombre depende del dueño, así que una subida nueva sobrescribe la anterior.
        var fileName = $"{ownerUserId}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await content.CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/avatars/{fileName}";
    }

    public Task<StoredFileContent?> GetAsync(
        Guid fileId,
        CancellationToken cancellationToken = default
    )
    {
        // Los archivos locales se sirven como estáticos desde wwwroot, no por el
        // endpoint de descarga; no hay nada que resolver por identificador.
        return Task.FromResult<StoredFileContent?>(null);
    }
}
