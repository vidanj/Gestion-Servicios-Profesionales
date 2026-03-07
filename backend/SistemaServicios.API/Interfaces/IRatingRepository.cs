using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IRatingRepository
{
    public Task<Rating> CreateAsync(Rating rating);

    public Task<IEnumerable<Rating>> GetByProfessionalIdAsync(Guid professionalId);

    // Para evitar que un cliente califique dos veces el mismo servicio
    public Task<bool> ExistsRatingForRequestAsync(int requestId, Guid clientId);
}
