using System.ComponentModel.DataAnnotations;

namespace SistemaServicios.API.DTOs.Profile;

public class ChangePasswordDto
{
    [Required]
    public required string CurrentPassword { get; set; }

    [Required]
    [MinLength(8)]
    public required string NewPassword { get; set; }

    [Required]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "La nueva contraseña y su confirmación no coinciden."
    )]
    public required string ConfirmNewPassword { get; set; }
}
