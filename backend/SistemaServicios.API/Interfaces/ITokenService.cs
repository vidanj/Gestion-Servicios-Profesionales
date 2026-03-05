using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}
