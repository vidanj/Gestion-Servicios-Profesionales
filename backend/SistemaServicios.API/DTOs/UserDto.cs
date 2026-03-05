using SistemaServicios.API.Models;

namespace SistemaServicios.API.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public UserRole Role { get; set; }
    public string? PhoneNumber { get; set; }
    public decimal AverageRating { get; set; }
    public bool Status { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
