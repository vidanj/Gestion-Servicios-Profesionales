using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.DTOs.Ratings;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Controllers
{
    [Authorize] // Protege para que solo usuarios logueados puedan calificar
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Módulo de Calificaciones")]
    public class RatingsController : ControllerBase
    {
        private readonly IRatingService _ratingService;

        public RatingsController(IRatingService ratingService)
        {
            _ratingService = ratingService;
        }

        [HttpPost]
        public async Task<ActionResult<RatingDto>> CreateRating(CreateRatingDto dto)
        {
            try
            {
                // Extraer el ID del cliente directamente del Token JWT
                var clientIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (
                    string.IsNullOrEmpty(clientIdClaim)
                    || !Guid.TryParse(clientIdClaim, out Guid clientId)
                )
                {
                    return Unauthorized(
                        new { message = "Token inválido o usuario no autenticado." }
                    );
                }

                var result = await _ratingService.CreateRatingAsync(clientId, dto);
                return CreatedAtAction(
                    nameof(GetProfessionalRatings),
                    new { professionalId = result.ProfessionalId },
                    result
                );
            }
            catch (InvalidOperationException ex)
            {
                // Atrapa el error de si ya calificó este servicio antes
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error interno del servidor.", error = ex.Message }
                );
            }
        }

        [AllowAnonymous] // Cualquier persona (incluso sin login) puede ver las reseñas
        [HttpGet("professional/{professionalId}")]
        public async Task<ActionResult<IEnumerable<RatingDto>>> GetProfessionalRatings(
            Guid professionalId
        )
        {
            var ratings = await _ratingService.GetProfessionalRatingsAsync(professionalId);
            return Ok(ratings);
        }

        [AllowAnonymous]
        [HttpGet("professional/{professionalId}/average")]
        public async Task<ActionResult<double>> GetProfessionalAverage(Guid professionalId)
        {
            var average = await _ratingService.GetProfessionalAverageRatingAsync(professionalId);
            return Ok(new { ProfessionalId = professionalId, AverageScore = average });
        }
    }
}
