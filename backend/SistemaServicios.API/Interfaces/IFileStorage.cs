namespace SistemaServicios.API.Interfaces;

/// <summary>
/// Almacenamiento de archivos binarios. Existe para que los servicios de negocio no
/// conozcan dónde viven los archivos: hoy es la base de datos, mañana puede ser un
/// object storage sin tocar a quien la consume.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Guarda el archivo y devuelve la ruta relativa con la que se sirve.
    /// Reemplaza cualquier archivo anterior del mismo dueño.
    /// </summary>
    public Task<string> SaveForOwnerAsync(
        Guid ownerUserId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Recupera un archivo por identificador. Devuelve <c>null</c> si no existe.
    /// </summary>
    public Task<StoredFileContent?> GetAsync(
        Guid fileId,
        CancellationToken cancellationToken = default
    );
}
