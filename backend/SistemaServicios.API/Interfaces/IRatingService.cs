using SistemaServicios.API.DTOs.Ratings;

namespace SistemaServicios.API.Interfaces;

public interface IRatingService
{
    public Task<RatingDto> CreateRatingAsync(Guid clientId, CreateRatingDto dto);

    public Task<double> GetProfessionalAverageRatingAsync(Guid professionalId);

    public Task<IEnumerable<RatingDto>> GetProfessionalRatingsAsync(Guid professionalId);
}
