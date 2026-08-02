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

    /// <summary>Solicita el envío de un PIN de acceso al correo registrado.</summary>
    [HttpPost("login-pin/request")]
    public async Task<IActionResult> RequestLoginPin([FromBody] LoginPinRequestDto dto)
    {
        try
        {
            await _authService.RequestLoginPinAsync(dto);

            return Ok(
                new
                {
                    message =
                        "Si el correo está registrado y la cuenta está activa, recibirás un PIN en breve.",
                }
            );
        }
        catch (Exception)
        {
            return StatusCode(
                500,
                new { message = "No se pudo enviar el PIN. Intenta nuevamente en unos minutos." }
            );
        }
    }

    /// <summary>Reenvía un PIN nuevo por correo para el inicio de sesión.</summary>
    [HttpPost("login-pin/resend")]
    public async Task<IActionResult> ResendLoginPin([FromBody] LoginPinRequestDto dto) =>
        await RequestLoginPin(dto);

    /// <summary>Valida el PIN recibido por correo y devuelve un JWT.</summary>
    [HttpPost("login-pin/verify")]
    public async Task<IActionResult> VerifyLoginPin([FromBody] VerifyLoginPinDto dto)
    {
        try
        {
            var result = await _authService.VerifyLoginPinAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        catch (Exception)
        {
            return StatusCode(
                500,
                new { message = "Error al procesar la solicitud. Intenta más tarde." }
            );
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
