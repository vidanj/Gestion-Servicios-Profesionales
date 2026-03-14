using FluentAssertions;
using Moq;
using SistemaServicios.API.DTOs;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class UserLogServiceTests
{
    private readonly Mock<IUserLogRepository> _mockRepo;
    private readonly UserLogService _service;

    private readonly UserLog _logEjemplo;

    public UserLogServiceTests()
    {
        _mockRepo = new Mock<IUserLogRepository>();
        _service = new UserLogService(_mockRepo.Object);

        _logEjemplo = new UserLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Juan",
                LastName = "Pérez",
                Email = "juan@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            Action = LogAction.CreacionUsuario,
            Detail = "Usuario creado.",
            Status = LogStatus.Exitoso,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ─── GetLogsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsyncRetornaListaDeLogsCorrectamente()
    {
        // Arrange
        var logs = new List<UserLog> { _logEjemplo };
        _mockRepo.Setup(r => r.GetLogsAsync(1, 10, null, null, null)).ReturnsAsync((logs, 1));

        // Act
        var (resultado, total) = await _service.GetLogsAsync(1, 10, null, null, null);

        // Assert
        resultado.Should().HaveCount(1);
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetLogsAsyncListaVaciaRetornaCeroElementos()
    {
        // Arrange
        _mockRepo
            .Setup(r => r.GetLogsAsync(1, 10, null, null, null))
            .ReturnsAsync((new List<UserLog>(), 0));

        // Act
        var (resultado, total) = await _service.GetLogsAsync(1, 10, null, null, null);

        // Assert
        resultado.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetLogsAsyncMapeaCorrectamenteADto()
    {
        // Arrange
        var logs = new List<UserLog> { _logEjemplo };
        _mockRepo.Setup(r => r.GetLogsAsync(1, 10, null, null, null)).ReturnsAsync((logs, 1));

        // Act
        var (resultado, _) = await _service.GetLogsAsync(1, 10, null, null, null);
        var dto = resultado.First();

        // Assert
        dto.Id.Should().Be(_logEjemplo.Id);
        dto.UserId.Should().Be(_logEjemplo.UserId);
        dto.Action.Should().Be(_logEjemplo.Action);
        dto.Status.Should().Be(_logEjemplo.Status);
        dto.Detail.Should().Be(_logEjemplo.Detail);
        dto.UserName.Should().Be("Juan Pérez");
    }

    [Fact]
    public async Task GetLogsAsyncConFiltroDeStatusPasaFiltroAlRepositorio()
    {
        // Arrange
        _mockRepo
            .Setup(r => r.GetLogsAsync(1, 10, LogStatus.Exitoso, null, null))
            .ReturnsAsync((new List<UserLog> { _logEjemplo }, 1));

        // Act
        var (resultado, total) = await _service.GetLogsAsync(1, 10, LogStatus.Exitoso, null, null);

        // Assert
        total.Should().Be(1);
        _mockRepo.Verify(r => r.GetLogsAsync(1, 10, LogStatus.Exitoso, null, null), Times.Once);
    }

    [Fact]
    public async Task GetLogsAsyncConFiltroDeAccionPasaFiltroAlRepositorio()
    {
        // Arrange
        _mockRepo
            .Setup(r => r.GetLogsAsync(1, 10, null, null, LogAction.CreacionUsuario))
            .ReturnsAsync((new List<UserLog> { _logEjemplo }, 1));

        // Act
        var (resultado, total) = await _service.GetLogsAsync(
            1,
            10,
            null,
            null,
            LogAction.CreacionUsuario
        );

        // Assert
        total.Should().Be(1);
        _mockRepo.Verify(
            r => r.GetLogsAsync(1, 10, null, null, LogAction.CreacionUsuario),
            Times.Once
        );
    }

    [Fact]
    public async Task GetLogsAsyncUsuarioNuloMuestraDesconocido()
    {
        // Arrange
        var logSinUsuario = new UserLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = null,
            Action = LogAction.IntentoAcceso,
            Detail = "Intento fallido.",
            Status = LogStatus.Error,
            CreatedAt = DateTime.UtcNow,
        };

        _mockRepo
            .Setup(r => r.GetLogsAsync(1, 10, null, null, null))
            .ReturnsAsync((new List<UserLog> { logSinUsuario }, 1));

        // Act
        var (resultado, _) = await _service.GetLogsAsync(1, 10, null, null, null);

        // Assert
        resultado.First().UserName.Should().Be("Desconocido");
    }

    // ─── CreateLogAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateLogAsyncGuardaYRetornaDto()
    {
        // Arrange
        _mockRepo
            .Setup(r => r.AddLogAsync(It.IsAny<UserLog>()))
            .ReturnsAsync(
                (UserLog l) =>
                {
                    l.User = _logEjemplo.User;
                    return l;
                }
            );

        var dto = new CreateUserLogDto
        {
            UserId = _logEjemplo.UserId,
            Action = LogAction.CreacionUsuario,
            Detail = "Usuario creado.",
            Status = LogStatus.Exitoso,
        };

        // Act
        var resultado = await _service.CreateLogAsync(dto);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Action.Should().Be(LogAction.CreacionUsuario);
        resultado.Status.Should().Be(LogStatus.Exitoso);
    }
}
