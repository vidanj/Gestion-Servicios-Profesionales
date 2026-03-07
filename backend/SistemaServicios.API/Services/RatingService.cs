using SistemaServicios.API.DTOs.Ratings;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

public class RatingService : IRatingService
{
    private readonly IRatingRepository _ratingRepository;

    public RatingService(IRatingRepository ratingRepository)
    {
        _ratingRepository = ratingRepository;
    }

    public async Task<RatingDto> CreateRatingAsync(Guid clientId, CreateRatingDto dto)
    {
        // Validación de negocio: Evitar que el cliente califique dos veces el mismo trabajo
        bool alreadyRated = await _ratingRepository.ExistsRatingForRequestAsync(
            dto.RequestId,
            clientId
        );
        if (alreadyRated)
        {
            throw new InvalidOperationException("Ya has calificado este servicio anteriormente.");
        }

        var rating = new Rating
        {
            RequestId = dto.RequestId,
            ProfessionalId = dto.ProfessionalId,
            ClientId = clientId,
            Score = dto.Score,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow,
        };

        var savedRating = await _ratingRepository.CreateAsync(rating);

        return new RatingDto
        {
            Id = savedRating.Id,
            RequestId = savedRating.RequestId,
            ClientId = savedRating.ClientId,
            ProfessionalId = savedRating.ProfessionalId,
            Score = savedRating.Score,
            Comment = savedRating.Comment,
            CreatedAt = savedRating.CreatedAt,
        };
    }

    public async Task<double> GetProfessionalAverageRatingAsync(Guid professionalId)
    {
        var ratings = await _ratingRepository.GetByProfessionalIdAsync(professionalId);

        if (!ratings.Any())
        {
            return 0;
        }

        return Math.Round(ratings.Average(r => r.Score), 1);
    }

    public async Task<IEnumerable<RatingDto>> GetProfessionalRatingsAsync(Guid professionalId)
    {
        var ratings = await _ratingRepository.GetByProfessionalIdAsync(professionalId);
        return ratings.Select(r => new RatingDto
        {
            Id = r.Id,
            RequestId = r.RequestId,
            ClientId = r.ClientId,
            ProfessionalId = r.ProfessionalId,
            Score = r.Score,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt,
        });
    }
}
