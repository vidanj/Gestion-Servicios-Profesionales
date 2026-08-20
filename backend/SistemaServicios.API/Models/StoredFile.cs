using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models;

/// <summary>
/// Archivo binario guardado en la base de datos.
/// Vive en su propia tabla y no como columna de User para que las consultas de
/// usuarios no arrastren los bytes de la imagen en cada lectura.
/// </summary>
public class StoredFile
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Dueño del archivo. Permite reemplazar el avatar borrando por usuario, sin
    /// tener que deducir el identificador anterior desde la URL.
    /// </summary>
    public Guid OwnerUserId { get; set; }

    [ForeignKey(nameof(OwnerUserId))]
    public User? Owner { get; set; }

    public byte[] Content { get; set; } = [];

    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
