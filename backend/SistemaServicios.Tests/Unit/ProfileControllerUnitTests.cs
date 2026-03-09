using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SistemaServicios.API.Controllers;
using SistemaServicios.API.DTOs;
using SistemaServicios.API.DTOs.Profile;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Pruebas unitarias del ProfileController que verifican las ramas que los tests de integración
/// no pueden alcanzar: el caso userId == null (ClaimsPrincipal sin NameIdentifier).
/// En integración, el middleware JWT devuelve 401 antes de llegar al controlador, por lo que
/// esas ramas solo se pueden cubrir instanciando el controlador directamente.
/// </summary>
public class ProfileControllerUnitTests
{
    private readonly Mock<IUserService> _mockService;
    private readonly ProfileController _controllerSinClaim;
    private readonly ProfileController _controllerConClaim;
    private readonly Guid _userId = Guid.NewGuid();

    // UserDto de retorno usado en mocks de servicio
    private readonly UserDto _userDto;

    public ProfileControllerUnitTests()
    {
        _mockService = new Mock<IUserService>();

        _userDto = new UserDto
        {
            Id = _userId,
            Email = "test@test.com",
            FirstName = "Juan",
            LastName = "Pérez",
            Role = UserRole.Client,
            Status = true,
            CreatedAt = DateTime.UtcNow,
        };

        // Controlador con un ClaimsPrincipal que NO tiene el claim NameIdentifier
        // → GetCurrentUserId() retorna null → cada action devuelve Unauthorized()
        _controllerSinClaim = BuildController(includeClaim: false);

        // Controlador con un ClaimsPrincipal que SÍ tiene NameIdentifier
        // → permite probar ramas de servicio retornando null (NotFound)
        _controllerConClaim = BuildController(includeClaim: true);
    }

    private ProfileController BuildController(bool includeClaim)
    {
        var claims = new List<Claim>();
        if (includeClaim)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, _userId.ToString()));
        }

        var identity = new ClaimsIdentity(claims, includeClaim ? "test" : string.Empty);
        var principal = new ClaimsPrincipal(identity);

        var controller = new ProfileController(_mockService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            },
        };
        return controller;
    }

    // ─── GetProfile — rama userId == null ────────────────────────────────────

    [Fact]
    public async Task GetProfileSinClaimNameIdentifierRetorna401()
    {
        var result = await _controllerSinClaim.GetProfile();

        result.Result.Should().BeOfType<UnauthorizedResult>();
        _mockService.Verify(s => s.GetUserByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    // ─── UpdateProfile — rama userId == null ─────────────────────────────────

    [Fact]
    public async Task UpdateProfileSinClaimNameIdentifierRetorna401()
    {
        var dto = new UpdateProfileDto { FirstName = "X", LastName = "Y" };

        var result = await _controllerSinClaim.UpdateProfile(dto);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        _mockService.Verify(
            s => s.UpdateOwnProfileAsync(It.IsAny<Guid>(), It.IsAny<UpdateProfileDto>()),
            Times.Never
        );
    }

    // ─── ChangePassword — rama userId == null ────────────────────────────────

    [Fact]
    public async Task ChangePasswordSinClaimNameIdentifierRetorna401()
    {
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NuevaPassword456!",
            ConfirmNewPassword = "NuevaPassword456!",
        };

        var result = await _controllerSinClaim.ChangePassword(dto);

        result.Should().BeOfType<UnauthorizedResult>();
        _mockService.Verify(
            s => s.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordDto>()),
            Times.Never
        );
    }

    // ─── UploadProfileImage — rama userId == null ────────────────────────────

    [Fact]
    public async Task UploadProfileImageSinClaimNameIdentifierRetorna401()
    {
        var mockFile = new Mock<IFormFile>();

        var result = await _controllerSinClaim.UploadProfileImage(mockFile.Object);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        _mockService.Verify(
            s => s.UpdateProfileImageAsync(It.IsAny<Guid>(), It.IsAny<IFormFile>()),
            Times.Never
        );
    }

    // ─── Ramas de servicio accesibles solo desde unit tests ──────────────────

    [Fact]
    public async Task GetProfileServicioRetornaNullRetorna404()
    {
        _ = _mockService
            .Setup(s => s.GetUserByIdAsync(_userId))
            .ReturnsAsync((UserDto?)null);

        var result = await _controllerConClaim.GetProfile();

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateProfileServicioRetornaNullRetorna404()
    {
        _ = _mockService
            .Setup(s =>
                s.UpdateOwnProfileAsync(It.IsAny<Guid>(), It.IsAny<UpdateProfileDto>())
            )
            .ReturnsAsync((UserDto?)null);

        var dto = new UpdateProfileDto { FirstName = "X", LastName = "Y" };
        var result = await _controllerConClaim.UpdateProfile(dto);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ChangePasswordServicioRetornaFalseRetorna404()
    {
        _ = _mockService
            .Setup(s => s.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordDto>()))
            .ReturnsAsync(false);

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NuevaPassword456!",
            ConfirmNewPassword = "NuevaPassword456!",
        };
        var result = await _controllerConClaim.ChangePassword(dto);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ChangePasswordServicioLanzaExcepcionRetorna400ConMensaje()
    {
        _ = _mockService
            .Setup(s => s.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordDto>()))
            .ThrowsAsync(new InvalidOperationException("La contraseña actual es incorrecta."));

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Equivocada123!",
            NewPassword = "NuevaPassword456!",
            ConfirmNewPassword = "NuevaPassword456!",
        };
        var result = await _controllerConClaim.ChangePassword(dto);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new { message = "La contraseña actual es incorrecta." });
    }

    [Fact]
    public async Task UploadProfileImageServicioRetornaNullRetorna404()
    {
        var mockFile = new Mock<IFormFile>();
        _ = _mockService
            .Setup(s => s.UpdateProfileImageAsync(It.IsAny<Guid>(), It.IsAny<IFormFile>()))
            .ReturnsAsync((UserDto?)null);

        var result = await _controllerConClaim.UploadProfileImage(mockFile.Object);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UploadProfileImageServicioLanzaExcepcionRetorna400ConMensaje()
    {
        var mockFile = new Mock<IFormFile>();
        _ = _mockService
            .Setup(s => s.UpdateProfileImageAsync(It.IsAny<Guid>(), It.IsAny<IFormFile>()))
            .ThrowsAsync(
                new InvalidOperationException("Solo se permiten imágenes en formato JPG o PNG.")
            );

        var result = await _controllerConClaim.UploadProfileImage(mockFile.Object);

        var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should()
            .BeEquivalentTo(
                new { message = "Solo se permiten imágenes en formato JPG o PNG." }
            );
    }
}
