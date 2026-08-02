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
    private readonly Mock<IEmailService> _mockEmail;
    private readonly Mock<ILoginPinStore> _mockPinStore;
    private readonly AuthService _authService;

    // Usuario base reutilizable en las pruebas
    private readonly User _usuarioActivo;

    public AuthServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _mockToken = new Mock<ITokenService>();
        _mockEmail = new Mock<IEmailService>();
        _mockPinStore = new Mock<ILoginPinStore>();
        _authService = new AuthService(
            _mockRepo.Object,
            _mockToken.Object,
            _mockEmail.Object,
            _mockPinStore.Object
        );

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
    public async Task LoginAsyncCredencialesValidasRetornaAuthResponse()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockToken.Setup(t => t.CreateToken(_usuarioActivo)).Returns("token-jwt-falso");

        var dto = new LoginRequestDto { Email = "juan@test.com", Password = "Password123!" };

        // Act
        var resultado = await _authService.LoginAsync(dto);

        // Assert
        _ = resultado.Token.Should().Be("token-jwt-falso");
        _ = resultado.Email.Should().Be("juan@test.com");
        _ = resultado.FirstName.Should().Be("Juan");
        _ = resultado.LastName.Should().Be("Pérez");
        _ = resultado.Role.Should().Be("Client");
    }

    [Fact]
    public async Task LoginAsyncEmailNoExisteLanzaUnauthorizedAccessException()
    {
        // Arrange: el repositorio devuelve null (usuario no encontrado)
        _ = _mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var dto = new LoginRequestDto { Email = "noexiste@test.com", Password = "cualquier" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(dto)
        );

        _ = ex.Message.Should().Be("Credenciales inválidas.");
    }

    [Fact]
    public async Task LoginAsyncCuentaDesactivadaLanzaUnauthorizedAccessException()
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
            Status = false, // <-- cuenta desactivada
        };

        _ = _mockRepo
            .Setup(r => r.GetByEmailAsync("inactivo@test.com"))
            .ReturnsAsync(usuarioInactivo);

        var dto = new LoginRequestDto { Email = "inactivo@test.com", Password = "Password123!" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(dto)
        );

        _ = ex.Message.Should().Be("La cuenta está desactivada.");
    }

    [Fact]
    public async Task LoginAsyncPasswordIncorrectoLanzaUnauthorizedAccessException()
    {
        // Arrange: el usuario existe pero la contraseña no coincide
        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);

        var dto = new LoginRequestDto { Email = "juan@test.com", Password = "PasswordEquivocado!" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(dto)
        );

        _ = ex.Message.Should().Be("Credenciales inválidas.");
    }

    [Fact]
    public async Task LoginAsyncCredencialesValidasLlamaCreateTokenUnaVez()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockToken.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token");

        var dto = new LoginRequestDto { Email = "juan@test.com", Password = "Password123!" };

        // Act
        _ = await _authService.LoginAsync(dto);

        // Assert: se generó exactamente un token
        _mockToken.Verify(t => t.CreateToken(_usuarioActivo), Times.Once);
    }

    [Fact]
    public async Task RequestLoginPinAsyncUsuarioActivoEnvioEmailYGuardaPin()
    {
        // Arrange
        LoginPinChallenge? pinGuardado = null;

        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockEmail
            .Setup(e => e.SendLoginPinEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _ = _mockPinStore
            .Setup(s => s.Save(It.IsAny<string>(), It.IsAny<LoginPinChallenge>()))
            .Callback<string, LoginPinChallenge>((_, challenge) => pinGuardado = challenge);

        var dto = new LoginPinRequestDto { Email = "juan@test.com" };

        // Act
        await _authService.RequestLoginPinAsync(dto);

        // Assert
        _mockEmail.Verify(
            e => e.SendLoginPinEmailAsync("juan@test.com", It.IsAny<string>()),
            Times.Once
        );
        _ = pinGuardado.Should().NotBeNull();
        _ = pinGuardado!.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
        _ = pinGuardado.FailedAttempts.Should().Be(0);
    }

    [Fact]
    public async Task VerifyLoginPinAsyncPinCorrectoRetornaAuthResponse()
    {
        // Arrange
        var challenge = new LoginPinChallenge
        {
            PinHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            RequestedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            FailedAttempts = 0,
        };

        _ = _mockPinStore.Setup(s => s.Get("juan@test.com")).Returns(challenge);
        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockToken.Setup(t => t.CreateToken(_usuarioActivo)).Returns("token-pin");

        var dto = new VerifyLoginPinDto { Email = "juan@test.com", Pin = "123456" };

        // Act
        var resultado = await _authService.VerifyLoginPinAsync(dto);

        // Assert
        _ = resultado.Token.Should().Be("token-pin");
        _mockPinStore.Verify(s => s.Remove("juan@test.com"), Times.Once);
    }

    [Fact]
    public async Task VerifyLoginPinAsyncPinIncorrectoLanzaUnauthorizedAccessException()
    {
        // Arrange
        var challenge = new LoginPinChallenge
        {
            PinHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            RequestedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            FailedAttempts = 0,
        };

        _ = _mockPinStore.Setup(s => s.Get("juan@test.com")).Returns(challenge);

        var dto = new VerifyLoginPinDto { Email = "juan@test.com", Pin = "654321" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.VerifyLoginPinAsync(dto)
        );

        _ = ex.Message.Should().Be("PIN incorrecto.");
        _ = challenge.FailedAttempts.Should().Be(1);
    }

    [Fact]
    public async Task VerifyLoginPinAsyncPinExpiradoLanzaInvalidOperationException()
    {
        // Arrange
        var challenge = new LoginPinChallenge
        {
            PinHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            RequestedAtUtc = DateTime.UtcNow.AddMinutes(-11),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
            FailedAttempts = 0,
        };

        _ = _mockPinStore.Setup(s => s.Get("juan@test.com")).Returns(challenge);

        var dto = new VerifyLoginPinDto { Email = "juan@test.com", Pin = "123456" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.VerifyLoginPinAsync(dto)
        );

        _ = ex.Message.Should().Be("El PIN expiró. Solicita uno nuevo.");
        _mockPinStore.Verify(s => s.Remove("juan@test.com"), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────
    // RegisterAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsyncDatosValidosRetornaAuthResponse()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _ = _mockRepo.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _ = _mockToken.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token-registro");

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
        _ = resultado.Token.Should().Be("token-registro");
        _ = resultado.Email.Should().Be("nuevo@test.com");
        _ = resultado.FirstName.Should().Be("Carlos");
        _ = resultado.LastName.Should().Be("García");
        _ = resultado.Role.Should().Be("Client");
    }

    [Fact]
    public async Task RegisterAsyncEmailDuplicadoLanzaInvalidOperationException()
    {
        // Arrange: el email ya existe en la base de datos
        _ = _mockRepo.Setup(r => r.EmailExistsAsync("duplicado@test.com")).ReturnsAsync(true);

        var dto = new RegisterRequestDto
        {
            Email = "duplicado@test.com",
            Password = "Password123!",
            FirstName = "Pedro",
            LastName = "Ruiz",
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.RegisterAsync(dto)
        );

        _ = ex.Message.Should().Be("El correo ya está registrado.");
    }

    [Fact]
    public async Task RegisterAsyncDatosValidosGuardaPasswordHasheada()
    {
        // Arrange
        User? usuarioGuardado = null;

        _ = _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _ = _mockRepo
            .Setup(r => r.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioGuardado = u)
            .ReturnsAsync((User u) => u);
        _ = _mockToken.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token");

        var dto = new RegisterRequestDto
        {
            Email = "hash@test.com",
            Password = "Password123!",
            FirstName = "María",
            LastName = "Torres",
        };

        // Act
        _ = await _authService.RegisterAsync(dto);

        // Assert: la contraseña nunca se guarda en texto plano
        _ = usuarioGuardado.Should().NotBeNull();
        _ = usuarioGuardado!.PasswordHash.Should().NotBe("Password123!");
        _ = BCrypt
            .Net.BCrypt.Verify("Password123!", usuarioGuardado.PasswordHash)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task RegisterAsyncDatosValidosNormalizaEmail()
    {
        // Arrange
        User? usuarioGuardado = null;

        _ = _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _ = _mockRepo
            .Setup(r => r.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioGuardado = u)
            .ReturnsAsync((User u) => u);
        _ = _mockToken.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token");

        var dto = new RegisterRequestDto
        {
            Email = "  USUARIO@TEST.COM  ", // con espacios y mayúsculas
            Password = "Password123!",
            FirstName = "  Luis  ",
            LastName = "  Vega  ",
        };

        // Act
        _ = await _authService.RegisterAsync(dto);

        // Assert: email en minúsculas y sin espacios, nombre sin espacios
        _ = usuarioGuardado!.Email.Should().Be("usuario@test.com");
        _ = usuarioGuardado.FirstName.Should().Be("Luis");
        _ = usuarioGuardado.LastName.Should().Be("Vega");
    }

    [Fact]
    public async Task RegisterAsyncDatosValidosAsignaIdYFechas()
    {
        // Arrange
        User? usuarioGuardado = null;
        var antes = DateTime.UtcNow;

        _ = _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _ = _mockRepo
            .Setup(r => r.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioGuardado = u)
            .ReturnsAsync((User u) => u);
        _ = _mockToken.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token");

        var dto = new RegisterRequestDto
        {
            Email = "fechas@test.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
        };

        // Act
        _ = await _authService.RegisterAsync(dto);

        // Assert: Id generado y fechas en UTC
        _ = usuarioGuardado!.Id.Should().NotBe(Guid.Empty);
        _ = usuarioGuardado.CreatedAt.Should().BeOnOrAfter(antes);
        _ = usuarioGuardado.UpdatedAt.Should().BeOnOrAfter(antes);
    }

    [Fact]
    public async Task RegisterAsyncConPhoneNumberGuardaPhoneNumberTrimmeado()
    {
        // Arrange: PhoneNumber con espacios al inicio y al final
        User? usuarioGuardado = null;

        _ = _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _ = _mockRepo
            .Setup(r => r.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioGuardado = u)
            .ReturnsAsync((User u) => u);
        _ = _mockToken.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token");

        var dto = new RegisterRequestDto
        {
            Email = "telefono@test.com",
            Password = "Password123!",
            FirstName = "Ana",
            LastName = "Gómez",
            PhoneNumber = "  +504 9999-8888  ", // con espacios al inicio y al final
        };

        // Act
        _ = await _authService.RegisterAsync(dto);

        // Assert: PhoneNumber guardado sin espacios extra
        _ = usuarioGuardado.Should().NotBeNull();
        _ = usuarioGuardado!.PhoneNumber.Should().Be("+504 9999-8888");
    }
}
