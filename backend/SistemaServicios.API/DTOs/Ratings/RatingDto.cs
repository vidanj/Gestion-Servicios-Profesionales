namespace SistemaServicios.API.DTOs.Ratings;

public class RatingDto
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public Guid ClientId { get; set; }

    public Guid ProfessionalId { get; set; }

    public int Score { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
