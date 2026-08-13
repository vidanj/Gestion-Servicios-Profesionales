using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using SistemaServicios.API.DTOs;
using SistemaServicios.API.DTOs.Profile;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class ProfileServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<IFileStorage> _mockFileStorage;
    private readonly UserService _service;
    private readonly Mock<IUserLogService> _mockLogService = null!;
    private readonly User _usuario;

    public ProfileServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _mockFileStorage = new Mock<IFileStorage>();

        _mockLogService = new Mock<IUserLogService>();
        _mockLogService
            .Setup(l => l.CreateLogAsync(It.IsAny<CreateUserLogDto>()))
            .ReturnsAsync(new UserLogDto());
        _service = new UserService(
            _mockRepo.Object,
            _mockFileStorage.Object,
            _mockLogService.Object
        );

        _usuario = new User
        {
            Id = Guid.NewGuid(),
            Email = "juan@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FirstName = "Juan",
            LastName = "Pérez",
            Role = UserRole.Client,
            PhoneNumber = "6641234567",
            Status = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    // ─── UpdateOwnProfileAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateOwnProfileAsyncUsuarioExistenteActualizaCamposYRetornaDto()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(_usuario.Id)).ReturnsAsync(_usuario);
        var dto = new UpdateProfileDto
        {
            FirstName = "Carlos",
            LastName = "García",
            PhoneNumber = "6649876543",
        };

        // Act
        var resultado = await _service.UpdateOwnProfileAsync(_usuario.Id, dto);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.FirstName.Should().Be("Carlos");
        resultado.LastName.Should().Be("García");
        resultado.PhoneNumber.Should().Be("6649876543");
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOwnProfileAsyncUsuarioNoEncontradoRetornaNull()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var dto = new UpdateProfileDto { FirstName = "Carlos", LastName = "García" };

        // Act
        var resultado = await _service.UpdateOwnProfileAsync(Guid.NewGuid(), dto);

        // Assert
        resultado.Should().BeNull();
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOwnProfileAsyncPhoneNullSePermite()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(_usuario.Id)).ReturnsAsync(_usuario);
        var dto = new UpdateProfileDto
        {
            FirstName = "Juan",
            LastName = "Pérez",
            PhoneNumber = null,
        };

        // Act
        var resultado = await _service.UpdateOwnProfileAsync(_usuario.Id, dto);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.PhoneNumber.Should().BeNull();
    }

    // ─── ChangePasswordAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsyncContrasenaCorrectaActualizaHashRetornaTrue()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(_usuario.Id)).ReturnsAsync(_usuario);
        var hashOriginal = _usuario.PasswordHash;
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NuevaPassword456!",
            ConfirmNewPassword = "NuevaPassword456!",
        };

        // Act
        var resultado = await _service.ChangePasswordAsync(_usuario.Id, dto);

        // Assert
        resultado.Should().BeTrue();
        _usuario.PasswordHash.Should().NotBe(hashOriginal);
        BCrypt.Net.BCrypt.Verify("NuevaPassword456!", _usuario.PasswordHash).Should().BeTrue();
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsyncContrasenaIncorrectaLanzaInvalidOperationException()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(_usuario.Id)).ReturnsAsync(_usuario);
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "ContrasenaEquivocada",
            NewPassword = "NuevaPassword456!",
            ConfirmNewPassword = "NuevaPassword456!",
        };

        // Act
        var act = () => _service.ChangePasswordAsync(_usuario.Id, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*incorrecta*");
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsyncUsuarioNoEncontradoRetornaFalse()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NuevaPassword456!",
            ConfirmNewPassword = "NuevaPassword456!",
        };

        // Act
        var resultado = await _service.ChangePasswordAsync(Guid.NewGuid(), dto);

        // Assert
        resultado.Should().BeFalse();
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    // ─── UpdateProfileImageAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfileImageAsyncImagenValidaGuardaArchivoYActualizaUrl()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(_usuario.Id)).ReturnsAsync(_usuario);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.Length).Returns(500_000); // 500 KB
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));

        const string urlAlmacenada = "/api/Files/8a1f0c2e-0000-4000-8000-000000000001";
        _mockFileStorage
            .Setup(s =>
                s.SaveForOwnerAsync(
                    _usuario.Id,
                    It.IsAny<Stream>(),
                    "image/jpeg",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(urlAlmacenada);

        // Act
        var resultado = await _service.UpdateProfileImageAsync(_usuario.Id, mockFile.Object);

        // Assert: la URL la decide el almacenamiento, no el servicio (issue #125)
        resultado.Should().NotBeNull();
        resultado!.ProfileImageUrl.Should().Be(urlAlmacenada);
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileImageAsyncMimeNoPermitidoLanzaInvalidOperationException()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(_usuario.Id)).ReturnsAsync(_usuario);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("image/gif");
        mockFile.Setup(f => f.Length).Returns(100_000);

        // Act
        var act = () => _service.UpdateProfileImageAsync(_usuario.Id, mockFile.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*JPG o PNG*");
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfileImageAsyncImagenDemasiadoGrandeLanzaInvalidOperationException()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(_usuario.Id)).ReturnsAsync(_usuario);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("image/png");
        mockFile.Setup(f => f.Length).Returns(3 * 1024 * 1024); // 3 MB > 2 MB límite

        // Act
        var act = () => _service.UpdateProfileImageAsync(_usuario.Id, mockFile.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*2 MB*");
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfileImageAsyncImagenPngValidaGuardaConExtensionPng()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(_usuario.Id)).ReturnsAsync(_usuario);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("image/png");
        mockFile.Setup(f => f.Length).Returns(300_000); // 300 KB — dentro del límite
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));

        _mockFileStorage
            .Setup(s =>
                s.SaveForOwnerAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("/api/Files/8a1f0c2e-0000-4000-8000-000000000002");

        // Act
        var resultado = await _service.UpdateProfileImageAsync(_usuario.Id, mockFile.Object);

        // Assert: el tipo png llega al almacenamiento; ya no viaja en la extensión
        resultado.Should().NotBeNull();
        _mockFileStorage.Verify(
            s =>
                s.SaveForOwnerAsync(
                    _usuario.Id,
                    It.IsAny<Stream>(),
                    "image/png",
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateProfileImageAsyncUsuarioNoEncontradoRetornaNull()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.Length).Returns(100_000);

        // Act
        var resultado = await _service.UpdateProfileImageAsync(Guid.NewGuid(), mockFile.Object);

        // Assert
        resultado.Should().BeNull();
        _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }
}
