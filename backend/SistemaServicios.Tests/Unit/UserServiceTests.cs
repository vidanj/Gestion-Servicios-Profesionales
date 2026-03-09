using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using SistemaServicios.API.DTOs;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly UserService _userService;

    // Usuario base reutilizable en las pruebas
    private readonly User _usuarioActivo;

    public UserServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _mockEnv = new Mock<IWebHostEnvironment>();
        _userService = new UserService(_mockRepo.Object, _mockEnv.Object);

        _usuarioActivo = new User
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

    // ─────────────────────────────────────────────────────────────
    // GetAllUsersAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsersAsyncRetornaListaDeUsuarios()
    {
        // Arrange
        var usuarios = new List<User> { _usuarioActivo };
        _ = _mockRepo.Setup(r => r.GetUsersAsync(1, 10)).ReturnsAsync((usuarios, 1));

        // Act
        var (resultado, total) = await _userService.GetAllUsersAsync(1, 10);

        // Assert
        _ = resultado.Should().HaveCount(1);
        _ = total.Should().Be(1);
        _ = resultado.First().Email.Should().Be("juan@test.com");
    }

    [Fact]
    public async Task GetAllUsersAsyncListaVaciaRetornaCeroElementos()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetUsersAsync(1, 10)).ReturnsAsync((new List<User>(), 0));

        // Act
        var (resultado, total) = await _userService.GetAllUsersAsync(1, 10);

        // Assert
        _ = resultado.Should().BeEmpty();
        _ = total.Should().Be(0);
    }

    [Fact]
    public async Task GetAllUsersAsyncMapeaCorrectamenteADto()
    {
        // Arrange
        var usuarios = new List<User> { _usuarioActivo };
        _ = _mockRepo.Setup(r => r.GetUsersAsync(1, 10)).ReturnsAsync((usuarios, 1));

        // Act
        var (resultado, _) = await _userService.GetAllUsersAsync(1, 10);
        var dto = resultado.First();

        // Assert: todos los campos mapeados correctamente
        _ = dto.Id.Should().Be(_usuarioActivo.Id);
        _ = dto.Email.Should().Be(_usuarioActivo.Email);
        _ = dto.FirstName.Should().Be(_usuarioActivo.FirstName);
        _ = dto.LastName.Should().Be(_usuarioActivo.LastName);
        _ = dto.Role.Should().Be(_usuarioActivo.Role);
        _ = dto.PhoneNumber.Should().Be(_usuarioActivo.PhoneNumber);
        _ = dto.Status.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllUsersAsyncMapeaProfileImageUrlCuandoEsNoNula()
    {
        // Arrange: usuario con foto de perfil asignada
        _usuarioActivo.ProfileImageUrl = "/uploads/avatars/test.jpg";
        var usuarios = new List<User> { _usuarioActivo };
        _ = _mockRepo.Setup(r => r.GetUsersAsync(1, 10)).ReturnsAsync((usuarios, 1));

        // Act
        var (resultado, _) = await _userService.GetAllUsersAsync(1, 10);
        var dto = resultado.First();

        // Assert: el getter get_ProfileImageUrl() del modelo User queda cubierto con valor no nulo
        _ = dto.ProfileImageUrl.Should().Be("/uploads/avatars/test.jpg");
    }

    // ─────────────────────────────────────────────────────────────
    // GetUserByIdAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsyncUsuarioExisteRetornaDto()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);

        // Act
        var resultado = await _userService.GetUserByIdAsync(_usuarioActivo.Id);

        // Assert
        _ = resultado.Should().NotBeNull();
        _ = resultado!.Email.Should().Be("juan@test.com");
        _ = resultado.FirstName.Should().Be("Juan");
    }

    [Fact]
    public async Task GetUserByIdAsyncUsuarioNoExisteRetornaNull()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act
        var resultado = await _userService.GetUserByIdAsync(Guid.NewGuid());

        // Assert
        _ = resultado.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────
    // CreateUserAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsyncDatosValidosRetornaDto()
    {
        // Arrange
        _ = _mockRepo
            .Setup(r => r.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _ = _mockRepo.Setup(r => r.AddUserAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var dto = new CreateUserDto
        {
            Email = "nuevo@test.com",
            Password = "Password123!",
            FirstName = "Carlos",
            LastName = "García",
            Role = UserRole.Client,
        };

        // Act
        var resultado = await _userService.CreateUserAsync(dto);

        // Assert
        _ = resultado.Email.Should().Be("nuevo@test.com");
        _ = resultado.FirstName.Should().Be("Carlos");
        _ = resultado.LastName.Should().Be("García");
        _ = resultado.Role.Should().Be(UserRole.Client);
    }

    [Fact]
    public async Task CreateUserAsyncEmailDuplicadoLanzaInvalidOperationException()
    {
        // Arrange: email ya existe
        _ = _mockRepo
            .Setup(r => r.GetUserByEmailAsync("duplicado@test.com"))
            .ReturnsAsync(_usuarioActivo);

        var dto = new CreateUserDto
        {
            Email = "duplicado@test.com",
            Password = "Password123!",
            FirstName = "Pedro",
            LastName = "Ruiz",
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.CreateUserAsync(dto)
        );

        _ = ex.Message.Should().Be("El correo ya está registrado.");
    }

    [Fact]
    public async Task CreateUserAsyncGuardaPasswordHasheada()
    {
        // Arrange
        User? usuarioGuardado = null;

        _ = _mockRepo
            .Setup(r => r.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _ = _mockRepo
            .Setup(r => r.AddUserAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioGuardado = u)
            .ReturnsAsync((User u) => u);

        var dto = new CreateUserDto
        {
            Email = "hash@test.com",
            Password = "Password123!",
            FirstName = "María",
            LastName = "Torres",
        };

        // Act
        _ = await _userService.CreateUserAsync(dto);

        // Assert: password nunca en texto plano
        _ = usuarioGuardado.Should().NotBeNull();
        _ = usuarioGuardado!.PasswordHash.Should().NotBe("Password123!");
        _ = BCrypt
            .Net.BCrypt.Verify("Password123!", usuarioGuardado.PasswordHash)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task CreateUserAsyncAsignaIdYFechas()
    {
        // Arrange
        User? usuarioGuardado = null;
        var antes = DateTime.UtcNow;

        _ = _mockRepo
            .Setup(r => r.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _ = _mockRepo
            .Setup(r => r.AddUserAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioGuardado = u)
            .ReturnsAsync((User u) => u);

        var dto = new CreateUserDto
        {
            Email = "fechas@test.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
        };

        // Act
        _ = await _userService.CreateUserAsync(dto);

        // Assert
        _ = usuarioGuardado!.Id.Should().NotBe(Guid.Empty);
        _ = usuarioGuardado.CreatedAt.Should().BeOnOrAfter(antes);
        _ = usuarioGuardado.UpdatedAt.Should().BeOnOrAfter(antes);
    }

    [Fact]
    public async Task CreateUserAsyncConRolProfesionalAsignaRolCorrecto()
    {
        // Arrange
        User? usuarioGuardado = null;

        _ = _mockRepo
            .Setup(r => r.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _ = _mockRepo
            .Setup(r => r.AddUserAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioGuardado = u)
            .ReturnsAsync((User u) => u);

        var dto = new CreateUserDto
        {
            Email = "pro@test.com",
            Password = "Password123!",
            FirstName = "Ana",
            LastName = "Torres",
            Role = UserRole.Professional,
        };

        // Act
        _ = await _userService.CreateUserAsync(dto);

        // Assert
        _ = usuarioGuardado!.Role.Should().Be(UserRole.Professional);
    }

    // ─────────────────────────────────────────────────────────────
    // UpdateUserAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsyncUsuarioExisteActualizaYRetornaTrue()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var dto = new UpdateUserDto
        {
            FirstName = "JuanEditado",
            LastName = "PérezEditado",
            PhoneNumber = "6649999999",
            Role = UserRole.Professional,
            Status = true,
        };

        // Act
        var resultado = await _userService.UpdateUserAsync(_usuarioActivo.Id, dto);

        // Assert
        _ = resultado.Should().BeTrue();
        _ = _usuarioActivo.FirstName.Should().Be("JuanEditado");
        _ = _usuarioActivo.LastName.Should().Be("PérezEditado");
        _ = _usuarioActivo.Role.Should().Be(UserRole.Professional);
    }

    [Fact]
    public async Task UpdateUserAsyncUsuarioNoExisteRetornaFalse()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var dto = new UpdateUserDto
        {
            FirstName = "X",
            LastName = "Y",
            Role = UserRole.Client,
            Status = true,
        };

        // Act
        var resultado = await _userService.UpdateUserAsync(Guid.NewGuid(), dto);

        // Assert
        _ = resultado.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserAsyncActualizaFechaUpdatedAt()
    {
        // Arrange
        var antes = DateTime.UtcNow;
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var dto = new UpdateUserDto
        {
            FirstName = "Juan",
            LastName = "Pérez",
            Role = UserRole.Client,
            Status = true,
        };

        // Act
        _ = await _userService.UpdateUserAsync(_usuarioActivo.Id, dto);

        // Assert
        _ = _usuarioActivo.UpdatedAt.Should().BeOnOrAfter(antes);
    }

    // ─────────────────────────────────────────────────────────────
    // SoftDeleteUserAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteUserAsyncUsuarioExisteDesactivaYRetornaTrue()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var resultado = await _userService.SoftDeleteUserAsync(_usuarioActivo.Id);

        // Assert: borrado lógico — Status = false, no se elimina de DB
        _ = resultado.Should().BeTrue();
        _ = _usuarioActivo.Status.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteUserAsyncUsuarioNoExisteRetornaFalse()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act
        var resultado = await _userService.SoftDeleteUserAsync(Guid.NewGuid());

        // Assert
        _ = resultado.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteUserAsyncLlamaUpdateUserAsyncUnaVez()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        _ = await _userService.SoftDeleteUserAsync(_usuarioActivo.Id);

        // Assert: se persistió exactamente una vez
        _mockRepo.Verify(r => r.UpdateUserAsync(_usuarioActivo), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────
    // CreateUserAsync – rol Admin (cubre UserRole.Admin = 0 en el modelo)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsyncConRolAdminAsignaRolCorrecto()
    {
        // Arrange
        User? usuarioGuardado = null;

        _ = _mockRepo
            .Setup(r => r.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _ = _mockRepo
            .Setup(r => r.AddUserAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioGuardado = u)
            .ReturnsAsync((User u) => u);

        var dto = new CreateUserDto
        {
            Email = "admin@test.com",
            Password = "Password123!",
            FirstName = "Super",
            LastName = "Admin",
            Role = UserRole.Admin,
        };

        // Act
        _ = await _userService.CreateUserAsync(dto);

        // Assert
        _ = usuarioGuardado!.Role.Should().Be(UserRole.Admin);
    }

    // ─────────────────────────────────────────────────────────────
    // UpdateOwnProfileAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateOwnProfileAsyncUsuarioExisteActualizaYRetornaDto()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var dto = new SistemaServicios.API.DTOs.Profile.UpdateProfileDto
        {
            FirstName = "Carlos",
            LastName = "García",
            PhoneNumber = "6649876543",
        };

        // Act
        var resultado = await _userService.UpdateOwnProfileAsync(_usuarioActivo.Id, dto);

        // Assert
        _ = resultado.Should().NotBeNull();
        _ = resultado!.FirstName.Should().Be("Carlos");
        _ = resultado.LastName.Should().Be("García");
        _ = resultado.PhoneNumber.Should().Be("6649876543");
    }

    [Fact]
    public async Task UpdateOwnProfileAsyncUsuarioNoExisteRetornaNull()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var dto = new SistemaServicios.API.DTOs.Profile.UpdateProfileDto
        {
            FirstName = "X",
            LastName = "Y",
        };

        // Act
        var resultado = await _userService.UpdateOwnProfileAsync(Guid.NewGuid(), dto);

        // Assert
        _ = resultado.Should().BeNull();
    }

    [Fact]
    public async Task UpdateOwnProfileAsyncActualizaFechaUpdatedAt()
    {
        // Arrange
        var antes = DateTime.UtcNow;
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var dto = new SistemaServicios.API.DTOs.Profile.UpdateProfileDto
        {
            FirstName = "Juan",
            LastName = "Pérez",
        };

        // Act
        _ = await _userService.UpdateOwnProfileAsync(_usuarioActivo.Id, dto);

        // Assert
        _ = _usuarioActivo.UpdatedAt.Should().BeOnOrAfter(antes);
    }

    // ─────────────────────────────────────────────────────────────
    // ChangePasswordAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsyncContrasenaCorrectaRetornaTrue()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var dto = new SistemaServicios.API.DTOs.Profile.ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NuevaPassword456!",
            ConfirmNewPassword = "NuevaPassword456!",
        };

        // Act
        var resultado = await _userService.ChangePasswordAsync(_usuarioActivo.Id, dto);

        // Assert
        _ = resultado.Should().BeTrue();
        _ = BCrypt
            .Net.BCrypt.Verify("NuevaPassword456!", _usuarioActivo.PasswordHash)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsyncContrasenaIncorrectaLanzaExcepcion()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);

        var dto = new SistemaServicios.API.DTOs.Profile.ChangePasswordDto
        {
            CurrentPassword = "ContrasenaMal!",
            NewPassword = "NuevaPassword456!",
            ConfirmNewPassword = "NuevaPassword456!",
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.ChangePasswordAsync(_usuarioActivo.Id, dto)
        );
        _ = ex.Message.Should().Be("La contraseña actual es incorrecta.");
    }

    [Fact]
    public async Task ChangePasswordAsyncUsuarioNoExisteRetornaFalse()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var dto = new SistemaServicios.API.DTOs.Profile.ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NuevaPassword456!",
            ConfirmNewPassword = "NuevaPassword456!",
        };

        // Act
        var resultado = await _userService.ChangePasswordAsync(Guid.NewGuid(), dto);

        // Assert
        _ = resultado.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────
    // UpdateProfileImageAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfileImageAsyncUsuarioNoExisteRetornaNull()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();

        // Act
        var resultado = await _userService.UpdateProfileImageAsync(Guid.NewGuid(), mockFile.Object);

        // Assert
        _ = resultado.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProfileImageAsyncMimeNoPermitidoLanzaExcepcion()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);

        var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        _ = mockFile.Setup(f => f.ContentType).Returns("image/gif");
        _ = mockFile.Setup(f => f.Length).Returns(512);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.UpdateProfileImageAsync(_usuarioActivo.Id, mockFile.Object)
        );
        _ = ex.Message.Should().Contain("JPG o PNG");
    }

    [Fact]
    public async Task UpdateProfileImageAsyncArchivoDemasiadoGrandeLanzaExcepcion()
    {
        // Arrange
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);

        var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        _ = mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        _ = mockFile.Setup(f => f.Length).Returns(3 * 1024 * 1024); // 3 MB > 2 MB

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.UpdateProfileImageAsync(_usuarioActivo.Id, mockFile.Object)
        );
        _ = ex.Message.Should().Contain("2 MB");
    }

    [Fact]
    public async Task UpdateProfileImageAsyncArchivoJpegValidoGuardaYRetornaDto()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        _ = _mockEnv.Setup(e => e.WebRootPath).Returns(tempDir);
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        _ = mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        _ = mockFile.Setup(f => f.Length).Returns(1024);
        _ = mockFile
            .Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _userService.UpdateProfileImageAsync(
            _usuarioActivo.Id,
            mockFile.Object
        );

        // Assert
        _ = resultado.Should().NotBeNull();
        _ = resultado!.ProfileImageUrl.Should().Contain("/uploads/avatars/");
        _ = resultado.ProfileImageUrl.Should().EndWith(".jpg");
    }

    [Fact]
    public async Task UpdateProfileImageAsyncArchivoPngValidoGuardaUrlConExtensionPng()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        _ = _mockEnv.Setup(e => e.WebRootPath).Returns(tempDir);
        _ = _mockRepo.Setup(r => r.GetByIdAsync(_usuarioActivo.Id)).ReturnsAsync(_usuarioActivo);
        _ = _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        _ = mockFile.Setup(f => f.ContentType).Returns("image/png");
        _ = mockFile.Setup(f => f.Length).Returns(512);
        _ = mockFile
            .Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _userService.UpdateProfileImageAsync(
            _usuarioActivo.Id,
            mockFile.Object
        );

        // Assert
        _ = resultado!.ProfileImageUrl.Should().EndWith(".png");
    }
}
