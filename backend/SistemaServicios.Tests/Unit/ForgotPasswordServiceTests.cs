using FluentAssertions;
using Moq;
using SistemaServicios.API.DTOs.Auth;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class ForgotPasswordServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<ITokenService> _mockToken;
    private readonly Mock<IEmailService> _mockEmail;
    private readonly AuthService _authService;

    // Usuario base reutilizable en las pruebas
    private readonly User _usuarioActivo;

    public ForgotPasswordServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _mockToken = new Mock<ITokenService>();
        _mockEmail = new Mock<IEmailService>();
        _authService = new AuthService(_mockRepo.Object, _mockToken.Object, _mockEmail.Object);

        _usuarioActivo = new User
        {
            Id = Guid.NewGuid(),
            Email = "juan@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("PasswordOriginal123!"),
            FirstName = "Juan",
            LastName = "Pérez",
            Role = UserRole.Client,
            Status = true,
        };
    }

    // ─────────────────────────────────────────────────────────────
    // ForgotPasswordAsync — flujo exitoso
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsyncEmailExistenteCompletaFlujoPrincipal()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _ = _mockEmail
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dto = new ForgotPasswordRequestDto { Email = "juan@test.com" };

        // Act
        var act = async () => await _authService.ForgotPasswordAsync(dto);

        // Assert: no lanza excepción
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ForgotPasswordAsyncLlamaUpdateUserAsyncUnaVez()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _ = _mockEmail
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dto = new ForgotPasswordRequestDto { Email = "juan@test.com" };

        // Act
        await _authService.ForgotPasswordAsync(dto);

        // Assert: el repositorio recibió exactamente una llamada a Update
        _mockRepo.Verify(r => r.UpdateUserAsync(_usuarioActivo), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsyncLlamaEnvioEmailUnaVez()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _ = _mockEmail
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dto = new ForgotPasswordRequestDto { Email = "juan@test.com" };

        // Act
        await _authService.ForgotPasswordAsync(dto);

        // Assert: se envió exactamente un email al correo correcto
        _mockEmail.Verify(
            e => e.SendPasswordResetEmailAsync("juan@test.com", It.IsAny<string>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ForgotPasswordAsyncActualizaPasswordHashEnElUsuario()
    {
        // Arrange
        var hashOriginal = _usuarioActivo.PasswordHash;
        User? usuarioActualizado = null;

        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo
            .Setup(r => r.UpdateUserAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioActualizado = u)
            .Returns(Task.CompletedTask);
        _ = _mockEmail
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dto = new ForgotPasswordRequestDto { Email = "juan@test.com" };

        // Act
        await _authService.ForgotPasswordAsync(dto);

        // Assert: el hash cambió (nueva contraseña generada)
        _ = usuarioActualizado.Should().NotBeNull();
        _ = usuarioActualizado!.PasswordHash.Should().NotBe(hashOriginal);
    }

    [Fact]
    public async Task ForgotPasswordAsyncNuevaPasswordEsHashDeBcrypt()
    {
        // Arrange
        string? passwordEnviada = null;
        User? usuarioActualizado = null;

        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo
            .Setup(r => r.UpdateUserAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioActualizado = u)
            .Returns(Task.CompletedTask);
        _ = _mockEmail
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, pwd) => passwordEnviada = pwd)
            .Returns(Task.CompletedTask);

        var dto = new ForgotPasswordRequestDto { Email = "juan@test.com" };

        // Act
        await _authService.ForgotPasswordAsync(dto);

        // Assert: la contraseña guardada en DB es el hash BCrypt de la que se envió por email
        _ = passwordEnviada.Should().NotBeNullOrEmpty();
        _ = BCrypt
            .Net.BCrypt.Verify(passwordEnviada!, usuarioActualizado!.PasswordHash)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task ForgotPasswordAsyncNuevaPasswordNoEsTextoPlano()
    {
        // Arrange
        User? usuarioActualizado = null;

        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo
            .Setup(r => r.UpdateUserAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioActualizado = u)
            .Returns(Task.CompletedTask);
        _ = _mockEmail
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dto = new ForgotPasswordRequestDto { Email = "juan@test.com" };

        // Act
        await _authService.ForgotPasswordAsync(dto);

        // Assert: el hash guardado empieza con $2 (prefijo BCrypt)
        _ = usuarioActualizado!.PasswordHash.Should().StartWith("$2");
    }

    // ─────────────────────────────────────────────────────────────
    // ForgotPasswordAsync — errores esperados
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsyncEmailNoRegistradoLanzaInvalidOperationException()
    {
        // Arrange: el repositorio devuelve null (usuario no encontrado)
        _ = _mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var dto = new ForgotPasswordRequestDto { Email = "noexiste@test.com" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.ForgotPasswordAsync(dto)
        );

        _ = ex
            .Message.Should()
            .Be("Si el correo está registrado, recibirás tu nueva contraseña en breve.");
    }

    [Fact]
    public async Task ForgotPasswordAsyncEmailNoRegistradoNuncaLlamaUpdateNiEmail()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var dto = new ForgotPasswordRequestDto { Email = "noexiste@test.com" };

        // Act
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.ForgotPasswordAsync(dto)
        );

        // Assert: nunca se actualiza la DB ni se envía email
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _mockEmail.Verify(
            e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ForgotPasswordAsyncCuentaDesactivadaLanzaInvalidOperationException()
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
            Status = false,
        };

        _ = _mockRepo
            .Setup(r => r.GetByEmailAsync("inactivo@test.com"))
            .ReturnsAsync(usuarioInactivo);

        var dto = new ForgotPasswordRequestDto { Email = "inactivo@test.com" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.ForgotPasswordAsync(dto)
        );

        _ = ex.Message.Should().Be("La cuenta está desactivada.");
    }

    [Fact]
    public async Task ForgotPasswordAsyncCuentaDesactivadaNuncaLlamaUpdateNiEmail()
    {
        // Arrange
        var usuarioInactivo = new User
        {
            Id = Guid.NewGuid(),
            Email = "inactivo@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FirstName = "Ana",
            LastName = "López",
            Role = UserRole.Client,
            Status = false,
        };

        _ = _mockRepo
            .Setup(r => r.GetByEmailAsync("inactivo@test.com"))
            .ReturnsAsync(usuarioInactivo);

        var dto = new ForgotPasswordRequestDto { Email = "inactivo@test.com" };

        // Act
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.ForgotPasswordAsync(dto)
        );

        // Assert: nada se ejecuta después de detectar cuenta desactivada
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _mockEmail.Verify(
            e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }
}
