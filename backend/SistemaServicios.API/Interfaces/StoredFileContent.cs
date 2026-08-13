namespace SistemaServicios.API.Interfaces;

/// <summary>
/// Contenido de un archivo recuperado del almacenamiento.
/// </summary>
public record StoredFileContent(byte[] Content, string ContentType);
