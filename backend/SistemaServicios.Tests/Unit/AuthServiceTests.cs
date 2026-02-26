using FluentAssertions;
using Moq;
using SistemaServicios.API.DTOs.Auth;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<ITokenService> _mockToken;
    private readonly AuthService _authService;

    // Usuario base reutilizable en las pruebas
    private readonly User _usuarioActivo;

    public AuthServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _mockToken = new Mock<ITokenService>();
        _authService = new AuthService(_mockRepo.Object, _mockToken.Object);

        _usuarioActivo = new User
        {
            Id = Guid.NewGuid(),
            Email = "juan@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FirstName = "Juan",
            LastName = "Pérez",
            Role = UserRole.Client,
            Status = true,
        };
    }

    // ─────────────────────────────────────────────────────────────
    // LoginAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_CredencialesValidas_RetornaAuthResponse()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com"))
                 .ReturnsAsync(_usuarioActivo);
        _mockToken.Setup(t => t.CreateToken(_usuarioActivo))
                  .Returns("token-jwt-falso");

        var dto = new LoginRequestDto { Email = "juan@test.com", Password = "Password123!" };

        // Act
        var resultado = await _authService.LoginAsync(dto);

        // Assert
        resultado.Token.Should().Be("token-jwt-falso");
        resultado.Email.Should().Be("juan@test.com");
        resultado.FirstName.Should().Be("Juan");
        resultado.LastName.Should().Be("Pérez");
        resultado.Role.Should().Be("Client");
    }

    [Fact]
    public async Task LoginAsync_EmailNoExiste_LanzaUnauthorizedAccessException()
    {
        // Arrange: el repositorio devuelve null (usuario no encontrado)
        _mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                 .ReturnsAsync((User?)null);

        var dto = new LoginRequestDto { Email = "noexiste@test.com", Password = "cualquier" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(dto));

        ex.Message.Should().Be("Credenciales inválidas.");
    }

    [Fact]
    public async Task LoginAsync_CuentaDesactivada_LanzaUnauthorizedAccessException()
    {
        // Arrange: usuario con Status = false
        var usuarioInactivo = new User
        {
            Id = Guid.NewGuid(),
            Email = "inactivo@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FirstName = "Ana",
            LastName = "López",
            Role = UserRole.Client,
            Status = false,   // <-- cuenta desactivada
        };

        _mockRepo.Setup(r => r.GetByEmailAsync("inactivo@test.com"))
                 .ReturnsAsync(usuarioInactivo);

        var dto = new LoginRequestDto { Email = "inactivo@test.com", Password = "Password123!" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(dto));

        ex.Message.Should().Be("La cuenta está desactivada.");
    }

    [Fact]
    public async Task LoginAsync_PasswordIncorrecto_LanzaUnauthorizedAccessException()
    {
        // Arrange: el usuario existe pero la contraseña no coincide
        _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com"))
                 .ReturnsAsync(_usuarioActivo);

        var dto = new LoginRequestDto { Email = "juan@test.com", Password = "PasswordEquivocado!" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(dto));

        ex.Message.Should().Be("Credenciales inválidas.");
    }

    [Fact]
    public async Task LoginAsync_CredencialesValidas_LlamaCreateTokenUnaVez()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com"))
                 .ReturnsAsync(_usuarioActivo);
        _mockToken.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token");

        var dto = new LoginRequestDto { Email = "juan@test.com", Password = "Password123!" };

        // Act
        await _authService.LoginAsync(dto);

        // Assert: se generó exactamente un token
        _mockToken.Verify(t => t.CreateToken(_usuarioActivo), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────
    // RegisterAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_DatosValidos_RetornaAuthResponse()
    {
        // Arrange
        _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>()))
                 .ReturnsAsync(false);
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<User>()))
                 .ReturnsAsync((User u) => u);
        _mockToken.Setup(t => t.CreateToken(It.IsAny<User>()))
                  .Returns("token-registro");

        var dto = new RegisterRequestDto
        {
            Email = "nuevo@test.com",
            Password = "Password123!",
            FirstName = "Carlos",
            LastName = "García",
        };

        // Act
        var resultado = await _authService.RegisterAsync(dto);

        // Assert
        resultado.Token.Should().Be("token-registro");
        resultado.Email.Should().Be("nuevo@test.com");
        resultado.FirstName.Should().Be("Carlos");
        resultado.LastName.Should().Be("García");
        resultado.Role.Should().Be("Client");
    }

    [Fact]
    public async Task RegisterAsync_EmailDuplicado_LanzaInvalidOperationException()
    {
        // Arrange: el email ya existe en la base de datos
        _mockRepo.Setup(r => r.EmailExistsAsync("duplicado@test.com"))
                 .ReturnsAsync(true);

        var dto = new RegisterRequestDto
        {
            Email = "duplicado@test.com",
            Password = "Password123!",
            FirstName = "Pedro",
            LastName = "Ruiz",
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(dto));

        ex.Message.Should().Be("El correo ya está registrado.");
    }

    [Fact]
    public async Task RegisterAsync_DatosValidos_GuardaPasswordHasheada()
    {
        // Arrange
        User? usuarioGuardado = null;

        _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<User>()))
                 .Callback<User>(u => usuarioGuardado = u)
                 .ReturnsAsync((User u) => u);
        _mockToken.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token");

        var dto = new RegisterRequestDto
        {
            Email = "hash@test.com",
            Password = "Password123!",
            FirstName = "María",
            LastName = "Torres",
        };

        // Act
        await _authService.RegisterAsync(dto);

        // Assert: la contraseña nunca se guarda en texto plano
        usuarioGuardado.Should().NotBeNull();
        usuarioGuardado!.PasswordHash.Should().NotBe("Password123!");
        BCrypt.Net.BCrypt.Verify("Password123!", usuarioGuardado.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_DatosValidos_NormalizaEmail()
    {
        // Arrange
        User? usuarioGuardado = null;

        _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<User>()))
                 .Callback<User>(u => usuarioGuardado = u)
                 .ReturnsAsync((User u) => u);
        _mockToken.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token");

        var dto = new RegisterRequestDto
        {
            Email = "  USUARIO@TEST.COM  ",   // con espacios y mayúsculas
            Password = "Password123!",
            FirstName = "  Luis  ",
            LastName = "  Vega  ",
        };

        // Act
        await _authService.RegisterAsync(dto);

        // Assert: email en minúsculas y sin espacios, nombre sin espacios
        usuarioGuardado!.Email.Should().Be("usuario@test.com");
        usuarioGuardado.FirstName.Should().Be("Luis");
        usuarioGuardado.LastName.Should().Be("Vega");
    }

    [Fact]
    public async Task RegisterAsync_DatosValidos_AsignaIdYFechas()
    {
        // Arrange
        User? usuarioGuardado = null;
        var antes = DateTime.UtcNow;

        _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<User>()))
                 .Callback<User>(u => usuarioGuardado = u)
                 .ReturnsAsync((User u) => u);
        _mockToken.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token");

        var dto = new RegisterRequestDto
        {
            Email = "fechas@test.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
        };

        // Act
        await _authService.RegisterAsync(dto);

        // Assert: Id generado y fechas en UTC
        usuarioGuardado!.Id.Should().NotBe(Guid.Empty);
        usuarioGuardado.CreatedAt.Should().BeOnOrAfter(antes);
        usuarioGuardado.UpdatedAt.Should().BeOnOrAfter(antes);
    }

    [Fact]
    public async Task RegisterAsync_ConPhoneNumber_GuardaPhoneNumberTrimmeado()
    {
        // Arrange: PhoneNumber con espacios al inicio y al final
        User? usuarioGuardado = null;

        _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<User>()))
                 .Callback<User>(u => usuarioGuardado = u)
                 .ReturnsAsync((User u) => u);
        _mockToken.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token");

        var dto = new RegisterRequestDto
        {
            Email       = "telefono@test.com",
            Password    = "Password123!",
            FirstName   = "Ana",
            LastName    = "Gómez",
            PhoneNumber = "  +504 9999-8888  ",   // con espacios al inicio y al final
        };

        // Act
        await _authService.RegisterAsync(dto);

        // Assert: PhoneNumber guardado sin espacios extra
        usuarioGuardado.Should().NotBeNull();
        usuarioGuardado!.PhoneNumber.Should().Be("+504 9999-8888");
    }
}
