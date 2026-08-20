using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.DTOs.Ratings;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly AppDbContext _context;

    public RatingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Rating> CreateAsync(Rating rating)
    {
        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();
        return rating;
    }

    public async Task<IEnumerable<RatingDto>> GetProfessionalRatingDtosAsync(Guid professionalId) =>
        await _context
            .Ratings.Where(r => r.ProfessionalId == professionalId)
            .Select(r => new RatingDto
            {
                Id = r.Id,
                RequestId = r.RequestId,
                ClientId = r.ClientId,
                ProfessionalId = r.ProfessionalId,
                Score = r.Score,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync();

    public async Task<IEnumerable<int>> GetProfessionalScoresAsync(Guid professionalId) =>
        await _context
            .Ratings.Where(r => r.ProfessionalId == professionalId)
            .Select(r => r.Score)
            .ToListAsync();

    public async Task<bool> ExistsRatingForRequestAsync(int requestId, Guid clientId)
    {
        return await _context.Ratings.AnyAsync(r =>
            r.RequestId == requestId && r.ClientId == clientId
        );
    }
}
