using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using SistemaServicios.API.DTOs;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class UserRegistrationStatsTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly Mock<IUserLogService> _mockLogService;
    private readonly UserService _service;

    public UserRegistrationStatsTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockLogService = new Mock<IUserLogService>();
        _mockLogService
            .Setup(l => l.CreateLogAsync(It.IsAny<CreateUserLogDto>()))
            .ReturnsAsync(new UserLogDto());

        _service = new UserService(_mockRepo.Object, _mockEnv.Object, _mockLogService.Object);
    }

    [Fact]
    public async Task GetRegistrationsByDateAsyncRetornaDatosDelRepositorio()
    {
        // Arrange
        var stats = new List<UserRegistrationStatDto>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), Count = 3 },
            new() { Date = DateOnly.FromDateTime(DateTime.UtcNow), Count = 5 },
        };
        _mockRepo.Setup(r => r.GetRegistrationsByDateAsync(30)).ReturnsAsync(stats);

        // Act
        var resultado = await _service.GetRegistrationsByDateAsync(30);

        // Assert
        resultado.Should().HaveCount(2);
        resultado.Sum(s => s.Count).Should().Be(8);
    }

    [Fact]
    public async Task GetRegistrationsByDateAsyncDaysNegativoUsaDefault30()
    {
        // Arrange
        _mockRepo
            .Setup(r => r.GetRegistrationsByDateAsync(30))
            .ReturnsAsync(new List<UserRegistrationStatDto>());

        // Act
        await _service.GetRegistrationsByDateAsync(-5);

        // Assert
        _mockRepo.Verify(r => r.GetRegistrationsByDateAsync(30), Times.Once);
    }

    [Fact]
    public async Task GetRegistrationsByDateAsyncDaysMayorA365UsaDefault30()
    {
        // Arrange
        _mockRepo
            .Setup(r => r.GetRegistrationsByDateAsync(30))
            .ReturnsAsync(new List<UserRegistrationStatDto>());

        // Act
        await _service.GetRegistrationsByDateAsync(400);

        // Assert
        _mockRepo.Verify(r => r.GetRegistrationsByDateAsync(30), Times.Once);
    }

    [Fact]
    public async Task GetRegistrationsByDateAsyncDays7RetornaCorrectamente()
    {
        // Arrange
        var stats = Enumerable
            .Range(0, 7)
            .Select(i => new UserRegistrationStatDto
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6 + i)),
                Count = i,
            })
            .ToList();
        _mockRepo.Setup(r => r.GetRegistrationsByDateAsync(7)).ReturnsAsync(stats);

        // Act
        var resultado = await _service.GetRegistrationsByDateAsync(7);

        // Assert
        resultado.Should().HaveCount(7);
        _mockRepo.Verify(r => r.GetRegistrationsByDateAsync(7), Times.Once);
    }

    [Fact]
    public async Task GetRegistrationsByDateAsyncListaVaciaRetornaSinErrores()
    {
        // Arrange
        _mockRepo
            .Setup(r => r.GetRegistrationsByDateAsync(30))
            .ReturnsAsync(new List<UserRegistrationStatDto>());

        // Act
        var resultado = await _service.GetRegistrationsByDateAsync(30);

        // Assert
        resultado.Should().BeEmpty();
    }
}
