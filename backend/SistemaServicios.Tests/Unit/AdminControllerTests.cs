using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SistemaServicios.API.Controllers;
using SistemaServicios.API.DTOs.Admin;
using SistemaServicios.API.Interfaces;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Pruebas unitarias del AdminController.
/// Verifican el mapeo de respuestas HTTP: 201 en éxito y 500 en fallo.
/// La autorización ([Authorize(Roles = "Admin")]) se prueba en las pruebas de integración.
/// </summary>
public class AdminControllerTests
{
    private readonly Mock<IBackupService> _mockBackupService;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _mockBackupService = new Mock<IBackupService>();
        _controller = new AdminController(_mockBackupService.Object);
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/admin/backup — respuestas HTTP
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBackup_Exitoso_Retorna201()
    {
        // Arrange
        _mockBackupService.Setup(s => s.GenerateBackupAsync())
            .ReturnsAsync(new BackupResponseDto
            {
                FileName = "backup_20260226_1000.sql",
                CreatedAt = DateTime.UtcNow,
                FileSizeBytes = 4096,
            });

        // Act
        var result = await _controller.CreateBackup();

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateBackup_Exitoso_RetornaBackupResponseDto()
    {
        // Arrange
        var expected = new BackupResponseDto
        {
            FileName = "backup_20260226_1430.sql",
            CreatedAt = new DateTime(2026, 2, 26, 14, 30, 0, DateTimeKind.Utc),
            FileSizeBytes = 8192,
        };

        _mockBackupService.Setup(s => s.GenerateBackupAsync())
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.CreateBackup();

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateBackup_Exitoso_LlamaAlServicioExactamenteUnaVez()
    {
        // Arrange
        _mockBackupService.Setup(s => s.GenerateBackupAsync())
            .ReturnsAsync(new BackupResponseDto { FileName = "backup.sql" });

        // Act
        await _controller.CreateBackup();

        // Assert: el controller no duplica llamadas al servicio
        _mockBackupService.Verify(s => s.GenerateBackupAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateBackup_ServicioLanzaInvalidOperationException_Retorna500()
    {
        // Arrange
        _mockBackupService.Setup(s => s.GenerateBackupAsync())
            .ThrowsAsync(new InvalidOperationException("pg_dump falló (código 1): conexión rechazada"));

        // Act
        var result = await _controller.CreateBackup();

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateBackup_ServicioLanzaInvalidOperationException_RetornaMensajeDeError()
    {
        // Arrange
        const string mensajeEsperado = "pg_dump falló (código 1): conexión rechazada";

        _mockBackupService.Setup(s => s.GenerateBackupAsync())
            .ThrowsAsync(new InvalidOperationException(mensajeEsperado));

        // Act
        var result = await _controller.CreateBackup();

        // Assert: el mensaje de error se propaga en el body de la respuesta
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.Value.Should().BeEquivalentTo(new { message = mensajeEsperado });
    }

    [Fact]
    public async Task CreateBackup_ExcepcionNoEsperada_NoesCapturaday_BurbujeoHaciaArriba()
    {
        // Arrange: el controller solo captura InvalidOperationException.
        // Cualquier otra excepción debe burbujear para que el middleware la maneje.
        _mockBackupService.Setup(s => s.GenerateBackupAsync())
            .ThrowsAsync(new OutOfMemoryException("sin memoria"));

        // Act & Assert
        await Assert.ThrowsAsync<OutOfMemoryException>(
            () => _controller.CreateBackup());
    }
}
