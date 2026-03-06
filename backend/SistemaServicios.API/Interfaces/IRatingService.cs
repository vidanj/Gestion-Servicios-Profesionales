using SistemaServicios.API.DTOs.Ratings;

namespace SistemaServicios.API.Interfaces
{
    public interface IRatingService
    {
        Task<RatingDto> CreateRatingAsync(Guid clientId, CreateRatingDto dto);

        Task<double> GetProfessionalAverageRatingAsync(Guid professionalId);
        Task<IEnumerable<RatingDto>> GetProfessionalRatingsAsync(Guid professionalId);
    }
}
