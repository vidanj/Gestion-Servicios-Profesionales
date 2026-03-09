using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.DTOs;
using SistemaServicios.API.DTOs.Profile;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Devuelve el perfil del usuario autenticado.</summary>
    [HttpGet]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userService.GetUserByIdAsync(userId.Value);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>Actualiza nombre y teléfono del usuario autenticado.</summary>
    [HttpPut]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var updated = await _userService.UpdateOwnProfileAsync(userId.Value, dto);
        if (updated == null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    /// <summary>Cambia la contraseña del usuario autenticado verificando la actual.</summary>
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _userService.ChangePasswordAsync(userId.Value, dto);
            if (!result)
            {
                return NotFound();
            }

            return Ok(new { message = "Contraseña actualizada correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Sube o reemplaza la foto de perfil del usuario autenticado.</summary>
    [HttpPost("foto")]
    public async Task<ActionResult<UserDto>> UploadProfileImage(IFormFile foto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var updated = await _userService.UpdateProfileImageAsync(userId.Value, foto);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
