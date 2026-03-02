using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Repositories
{
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

        public async Task<IEnumerable<Rating>> GetByProfessionalIdAsync(Guid professionalId)
        {
            return await _context.Ratings
                .Where(r => r.ProfessionalId == professionalId)
                .ToListAsync();
        }

        public async Task<bool> ExistsRatingForRequestAsync(int requestId, Guid clientId)
        {
            return await _context.Ratings.AnyAsync(r => r.RequestId == requestId && r.ClientId == clientId);
        }
    }
}