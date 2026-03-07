using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.DTOs.Auth;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Controllers;

// Skinny Controller: solo delega al servicio y mapea respuestas HTTP.
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Inicia sesión y devuelve un JWT.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>Registra un nuevo usuario y devuelve un JWT.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto);
            return StatusCode(201, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Devuelve los datos del usuario autenticado (requiere JWT).</summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var firstName = User.FindFirst("firstName")?.Value;
        var lastName = User.FindFirst("lastName")?.Value;

        return Ok(
            new
            {
                userId,
                email,
                role,
                firstName,
                lastName,
            }
        );
    }

    /// <summary>
    /// Genera una nueva contraseña y la envía al correo registrado.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        try
        {
            await _authService.ForgotPasswordAsync(dto);

            return Ok(
                new
                {
                    message = "Si el correo está registrado, recibirás tu nueva contraseña en breve.",
                }
            );
        }
        catch (InvalidOperationException ex)
        {
            return Ok(new { message = ex.Message }); // 200 intencional: no revelar existencia del email
        }
        catch (Exception)
        {
            return StatusCode(
                500,
                new { message = "Error al procesar la solicitud. Intenta más tarde." }
            );
        }
    }
}
