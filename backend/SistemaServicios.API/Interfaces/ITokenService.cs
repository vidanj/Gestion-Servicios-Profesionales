using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface ITokenService
{
    public string CreateToken(User user);
}
