using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces
{
    public interface IRatingRepository
    {
        Task<Rating> CreateAsync(Rating rating);
        Task<IEnumerable<Rating>> GetByProfessionalIdAsync(Guid professionalId);

        // Para evitar que un cliente califique dos veces el mismo servicio
        Task<bool> ExistsRatingForRequestAsync(int requestId, Guid clientId);
    }
}
