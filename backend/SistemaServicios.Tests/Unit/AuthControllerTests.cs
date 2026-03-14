using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SistemaServicios.API.Controllers;
using SistemaServicios.API.DTOs.Auth;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Pruebas unitarias del AuthController.
/// Cubren el mapeo HTTP de Login/Register y las ramas null-conditional de Me().
/// El flujo completo con JWT real se verifica en las pruebas de integración.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new AuthController(_mockAuthService.Object);
    }

    // JsonSerializer por defecto escapa caracteres no-ASCII (á → \u00E1).
    // UnsafeRelaxedJsonEscaping los deja como están para que las aserciones
    // de texto en español funcionen correctamente.
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static string ToJson(object? value) => JsonSerializer.Serialize(value, JsonOpts);

    /// <summary>
    /// Configura el ClaimsPrincipal del controller con los claims indicados.
    /// Permite llamar a Me() directamente sin pasar por el middleware JWT.
    /// </summary>
    private void SetUserContext(params (string type, string value)[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(c => new Claim(c.type, c.value)),
            "TestAuth"
        );
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
    }

    // ─────────────────────────────────────────────────────────────
    // Login — mapeo HTTP
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginExitosoRetorna200()
    {
        // Arrange
        _ = _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(new AuthResponseDto { Token = "jwt", Email = "u@test.com" });

        // Act
        var result = await _controller.Login(
            new LoginRequestDto { Email = "u@test.com", Password = "pass" }
        );

        // Assert
        _ = result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task LoginCredencialesInvalidasRetorna401ConMensaje()
    {
        // Arrange
        _ = _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Credenciales inválidas."));

        // Act
        var result = await _controller.Login(
            new LoginRequestDto { Email = "x@test.com", Password = "mal" }
        );

        // Assert
        var objectResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        _ = objectResult.StatusCode.Should().Be(401);
        var json = ToJson(objectResult.Value);
        _ = json.Should().Contain("Credenciales inválidas.");
    }

    // ─────────────────────────────────────────────────────────────
    // Register — mapeo HTTP
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterExitosoRetorna201()
    {
        // Arrange
        _ = _mockAuthService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequestDto>()))
            .ReturnsAsync(new AuthResponseDto { Token = "jwt", Email = "nuevo@test.com" });

        // Act
        var result = await _controller.Register(
            new RegisterRequestDto
            {
                Email = "nuevo@test.com",
                Password = "pass",
                FirstName = "A",
                LastName = "B",
            }
        );

        // Assert
        _ = result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task RegisterEmailDuplicadoRetorna400ConMensaje()
    {
        // Arrange
        _ = _mockAuthService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("El correo ya está registrado."));

        // Act
        var result = await _controller.Register(
            new RegisterRequestDto
            {
                Email = "dup@test.com",
                Password = "pass",
                FirstName = "A",
                LastName = "B",
            }
        );

        // Assert
        var objectResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        _ = objectResult.StatusCode.Should().Be(400);
        var json = ToJson(objectResult.Value);
        _ = json.Should().Contain("El correo ya está registrado.");
    }

    // ─────────────────────────────────────────────────────────────
    // Me — ramas null-conditional ?.Value
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void MeConTodosLosClaimsRetornaOkConLosDatos()
    {
        // Arrange: ClaimsPrincipal con los cinco claims que lee el método.
        // Cubre la rama "claim presente" (no-null) de cada operador ?. en Me().
        SetUserContext(
            (ClaimTypes.NameIdentifier, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            (ClaimTypes.Email, "test@test.com"),
            (ClaimTypes.Role, "Client"),
            ("firstName", "Juan"),
            ("lastName", "Pérez")
        );

        // Act
        var result = _controller.Me();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = ToJson(ok.Value);
        _ = json.Should().Contain("test@test.com");
        _ = json.Should().Contain("Juan");
        _ = json.Should().Contain("Pérez");
        _ = json.Should().Contain("Client");
    }

    [Fact]
    public void MeSinNingunClaimRetornaOkConValoresNulos()
    {
        // Arrange: ClaimsPrincipal vacío → cada FindFirst devuelve null.
        // Cubre la rama "claim ausente" (null) de cada operador ?. en Me().
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()),
            },
        };

        // Act
        var result = _controller.Me();

        // Assert: el método devuelve 200 incluso sin claims (valores null en el body)
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        _ = ok.StatusCode.Should().Be(200);
        var json = ToJson(ok.Value);
        _ = json.Should().Contain("null");
    }

    // ─────────────────────────────────────────────────────────────
    // Register — Role
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterConRoleProfesionalRetorna201()
    {
        // Arrange
        _ = _mockAuthService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequestDto>()))
            .ReturnsAsync(
                new AuthResponseDto
                {
                    Token = "jwt",
                    Email = "pro@test.com",
                    Role = "Professional",
                }
            );

        // Act
        var result = await _controller.Register(
            new RegisterRequestDto
            {
                Email = "pro@test.com",
                Password = "pass",
                FirstName = "Ana",
                LastName = "Torres",
                Role = UserRole.Professional,
            }
        );

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Which;
        _ = objectResult.StatusCode.Should().Be(201);
        var json = ToJson(objectResult.Value);
        _ = json.Should().Contain("Professional");
    }

    [Fact]
    public async Task RegisterSinRoleUsaClientePorDefectoRetorna201()
    {
        // Arrange
        _ = _mockAuthService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequestDto>()))
            .ReturnsAsync(
                new AuthResponseDto
                {
                    Token = "jwt",
                    Email = "cliente@test.com",
                    Role = "Client",
                }
            );

        // Act
        var result = await _controller.Register(
            new RegisterRequestDto
            {
                Email = "cliente@test.com",
                Password = "pass",
                FirstName = "Luis",
                LastName = "Vega",
                // Role no especificado → default Client
            }
        );

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Which;
        _ = objectResult.StatusCode.Should().Be(201);
        var json = ToJson(objectResult.Value);
        _ = json.Should().Contain("Client");
    }

    [Fact]
    public async Task RegisterErrorInesperadoRetorna500()
    {
        // Arrange: cualquier excepción no controlada
        _ = _mockAuthService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequestDto>()))
            .ThrowsAsync(new NotSupportedException("DB caída"));

        // Act
        var result = await _controller.Register(
            new RegisterRequestDto
            {
                Email = "error@test.com",
                Password = "pass",
                FirstName = "A",
                LastName = "B",
                Role = UserRole.Professional,
            }
        );

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        _ = objectResult.StatusCode.Should().Be(500);
    }
}
