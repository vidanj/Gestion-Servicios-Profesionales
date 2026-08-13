using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

/// <summary>
/// Guarda los archivos en PostgreSQL. Al vivir en la base y no en el disco del
/// contenedor, los avatares sobreviven a los despliegues y son visibles desde
/// cualquier réplica, que es lo que impedía el escalado horizontal.
/// </summary>
public class DbFileStorage : IFileStorage
{
    private readonly AppDbContext _context;

    public DbFileStorage(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> SaveForOwnerAsync(
        Guid ownerUserId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(content);

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        // Se borra lo anterior del mismo dueño: sin esto, cada cambio de avatar
        // dejaría una fila huérfana con su binario dentro.
        var previous = await _context
            .StoredFiles.Where(f => f.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);

        if (previous.Count > 0)
        {
            _context.StoredFiles.RemoveRange(previous);
        }

        var stored = new StoredFile
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Content = bytes,
            ContentType = contentType,
            SizeBytes = bytes.LongLength,
            CreatedAt = DateTime.UtcNow,
        };

        _ = await _context.StoredFiles.AddAsync(stored, cancellationToken);
        _ = await _context.SaveChangesAsync(cancellationToken);

        // Ruta relativa: el frontend la concatena a la URL base de la API.
        return $"/api/Files/{stored.Id}";
    }

    public async Task<StoredFileContent?> GetAsync(
        Guid fileId,
        CancellationToken cancellationToken = default
    )
    {
        var file = await _context
            .StoredFiles.AsNoTracking()
            .Where(f => f.Id == fileId)
            .Select(f => new StoredFileContent(f.Content, f.ContentType))
            .FirstOrDefaultAsync(cancellationToken);

        return file;
    }
}
