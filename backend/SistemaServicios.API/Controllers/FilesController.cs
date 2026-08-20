using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorage _fileStorage;

    public FilesController(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// Sirve un archivo almacenado. Anónimo: los avatares se muestran en el catálogo
    /// público, que se consulta sin iniciar sesión.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFile(Guid id, CancellationToken cancellationToken)
    {
        var file = await _fileStorage.GetAsync(id, cancellationToken);

        if (file is null)
        {
            return NotFound();
        }

        // Cacheable de forma agresiva porque el identificador cambia en cada subida:
        // reemplazar el avatar produce una URL nueva, así que no hay que invalidar nada.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        return File(file.Content, file.ContentType);
    }
}
