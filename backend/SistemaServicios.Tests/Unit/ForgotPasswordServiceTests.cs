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
    // ForgotPasswordAsync — flujo exitoso y mitigación DoS
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsyncEmailExistenteCompletaFlujoPrincipal()
    {
        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _ = _mockEmail
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dto = new ForgotPasswordRequestDto { Email = "juan@test.com" };

        var act = async () => await _authService.ForgotPasswordAsync(dto);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ForgotPasswordAsyncNoModificaPasswordHashActual()
    {
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

        await _authService.ForgotPasswordAsync(dto);

        // Assert: No se altera la contraseña actual para prevenir DoS
        _ = usuarioActualizado.Should().NotBeNull();
        _ = usuarioActualizado!.PasswordHash.Should().Be(hashOriginal);
        _ = usuarioActualizado.PasswordResetToken.Should().NotBeNullOrEmpty();
        _ = usuarioActualizado.ResetTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ForgotPasswordAsyncGeneraTokenYLoEnviaPorEmail()
    {
        string? tokenEnviado = null;
        User? usuarioActualizado = null;

        _ = _mockRepo.Setup(r => r.GetByEmailAsync("juan@test.com")).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo
            .Setup(r => r.UpdateUserAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioActualizado = u)
            .Returns(Task.CompletedTask);
        _ = _mockEmail
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, token) => tokenEnviado = token)
            .Returns(Task.CompletedTask);

        var dto = new ForgotPasswordRequestDto { Email = "juan@test.com" };

        await _authService.ForgotPasswordAsync(dto);

        _ = tokenEnviado.Should().NotBeNullOrEmpty();
        _ = usuarioActualizado!.PasswordResetToken.Should().Be(tokenEnviado);
    }

    // ─────────────────────────────────────────────────────────────
    // ForgotPasswordAsync — protección contra enumeración
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsyncEmailNoRegistradoTerminaEnSilencio()
    {
        _ = _mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var dto = new ForgotPasswordRequestDto { Email = "noexiste@test.com" };

        var ex = await Record.ExceptionAsync(() => _authService.ForgotPasswordAsync(dto));

        _ = ex.Should().BeNull();
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _mockEmail.Verify(
            e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ForgotPasswordAsyncCuentaDesactivadaTerminaEnSilencio()
    {
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

        var ex = await Record.ExceptionAsync(() => _authService.ForgotPasswordAsync(dto));

        _ = ex.Should().BeNull();
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _mockEmail.Verify(
            e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    // ─────────────────────────────────────────────────────────────
    // ResetPasswordAsync — validación y cambio de clave
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsyncTokenValidoActualizaPasswordHashYLimpiaToken()
    {
        var usuarioConToken = new User
        {
            Id = Guid.NewGuid(),
            Email = "juan@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("PasswordVieja123!"),
            FirstName = "Juan",
            LastName = "Pérez",
            Role = UserRole.Client,
            Status = true,
            PasswordResetToken = "token_valido_123",
            ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(10),
        };

        User? usuarioGuardado = null;
        _ = _mockRepo
            .Setup(r => r.GetByResetTokenAsync("token_valido_123"))
            .ReturnsAsync(usuarioConToken);
        _ = _mockRepo
            .Setup(r => r.UpdateUserAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioGuardado = u)
            .Returns(Task.CompletedTask);

        var dto = new ResetPasswordRequestDto("token_valido_123", "NuevaPasswordSegura456!");

        await _authService.ResetPasswordAsync(dto);

        _ = usuarioGuardado.Should().NotBeNull();
        _ = usuarioGuardado!.PasswordResetToken.Should().BeNull();
        _ = usuarioGuardado.ResetTokenExpiresAt.Should().BeNull();
        _ = BCrypt
            .Net.BCrypt.Verify("NuevaPasswordSegura456!", usuarioGuardado.PasswordHash)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsyncTokenExpiradoLanzaInvalidOperationException()
    {
        var usuarioTokenVencido = new User
        {
            Id = Guid.NewGuid(),
            Email = "juan@test.com",
            PasswordHash = "hash",
            FirstName = "Juan",
            LastName = "Pérez",
            Role = UserRole.Client,
            Status = true,
            PasswordResetToken = "token_vencido",
            ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(-5), // Expirado
        };

        _ = _mockRepo
            .Setup(r => r.GetByResetTokenAsync("token_vencido"))
            .ReturnsAsync(usuarioTokenVencido);

        var dto = new ResetPasswordRequestDto("token_vencido", "NuevaPassword123!");

        var act = () => _authService.ResetPasswordAsync(dto);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*inválido o ha expirado*");
    }

    [Fact]
    public async Task ResetPasswordAsyncTokenInexistenteLanzaInvalidOperationException()
    {
        _ = _mockRepo
            .Setup(r => r.GetByResetTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var dto = new ResetPasswordRequestDto("token_fantasma", "NuevaPassword123!");

        var act = () => _authService.ResetPasswordAsync(dto);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*inválido o ha expirado*");
    }
}
