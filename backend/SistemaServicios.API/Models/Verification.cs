using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models;

public enum DocumentType
{
    TituloProfesional = 1,
    CedulaProfesional = 2,
    Maestria = 3,
    Doctorado = 4,
    Especialidad = 5,
    Diplomado = 6,
    Certificacion = 7,
    CartaPasante = 8,
    CertificadoIdioma = 9,
    LicenciaTecnica = 10,
}

public enum VerificationStatus
{
    Pending = 0,
    Verified = 1,
    Rejected = 2,
}

public class Verification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid ProfessionalId { get; set; }
    public User? Professional { get; set; }

    [Required]
    public DocumentType DocumentType { get; set; }

    [Required]
    [MaxLength(2048)]
    public string DocumentUrl { get; set; } = string.Empty;
    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

    [MaxLength(2048)]
    public string ExternalReference { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
