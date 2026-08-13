using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SistemaServicios.API.Controllers;
using SistemaServicios.API.DTOs.Admin;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Despachador falso que retiene el trabajo encolado en lugar de ejecutarlo.
/// Permite comprobar por separado que el controller encola, y qué hace ese trabajo.
/// </summary>
internal sealed class FakeDispatcher : IBackgroundTaskDispatcher
{
    public Func<IServiceProvider, CancellationToken, ValueTask>? WorkItem { get; private set; }

    public int EnqueueCount { get; private set; }

    public ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, ValueTask> workItem)
    {
        WorkItem = workItem;
        EnqueueCount++;
        return ValueTask.CompletedTask;
    }

    public ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken
    ) => throw new NotSupportedException("El falso no consume la cola.");
}

/// <summary>
/// Pruebas unitarias del AdminController tras pasar el respaldo a segundo plano.
/// El controller ya no espera a pg_dump: acepta el trabajo y responde 202.
/// </summary>
public class AdminControllerTests
{
    private readonly Mock<IBackupService> _mockBackupService;
    private readonly FakeDispatcher _dispatcher;
    private readonly BackupJobTracker _tracker;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _mockBackupService = new Mock<IBackupService>();
        _dispatcher = new FakeDispatcher();
        _tracker = new BackupJobTracker();
        _controller = new AdminController(
            _mockBackupService.Object,
            _dispatcher,
            _tracker,
            NullLogger<AdminController>.Instance
        );
    }

    /// <summary>Proveedor que resuelve lo que el trabajo encolado necesita.</summary>
    private IServiceProvider BuildProvider()
    {
        var provider = new Mock<IServiceProvider>();
        _ = provider.Setup(p => p.GetService(typeof(IBackupJobTracker))).Returns(_tracker);
        _ = provider
            .Setup(p => p.GetService(typeof(IBackupService)))
            .Returns(_mockBackupService.Object);
        return provider.Object;
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/admin/backup — encolado
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBackupRetorna202YNoEsperaAPgDump()
    {
        // Act
        var result = await _controller.CreateBackup();

        // Assert: antes devolvía 201 tras esperar al volcado completo
        var accepted = result.Should().BeOfType<AcceptedResult>().Subject;
        _ = accepted.StatusCode.Should().Be(202);
        _mockBackupService.Verify(s => s.GenerateBackupAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateBackupDevuelveUnTrabajoPendiente()
    {
        // Act
        var result = await _controller.CreateBackup();

        // Assert
        var job = (result as AcceptedResult)!.Value.Should().BeOfType<BackupJobDto>().Subject;
        _ = job.Id.Should().NotBeEmpty();
        _ = job.Status.Should().Be(BackupJobStatus.Pendiente);
    }

    [Fact]
    public async Task CreateBackupEncolaElTrabajoUnaVez()
    {
        // Act
        _ = await _controller.CreateBackup();

        // Assert
        _ = _dispatcher.EnqueueCount.Should().Be(1);
        _ = _dispatcher.WorkItem.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBackupDosVecesSeguidasNoLanzaDosPgDump()
    {
        // Arrange & Act: segunda petición con el primer trabajo aún sin terminar
        var primera = await _controller.CreateBackup();
        var segunda = await _controller.CreateBackup();

        // Assert: se devuelve el mismo trabajo y se encola una sola vez
        var jobPrimera = (primera as AcceptedResult)!.Value as BackupJobDto;
        var jobSegunda = (segunda as AcceptedResult)!.Value as BackupJobDto;

        _ = jobSegunda!.Id.Should().Be(jobPrimera!.Id);
        _ = _dispatcher.EnqueueCount.Should().Be(1);
    }

    // ─────────────────────────────────────────────────────────────
    // El trabajo encolado
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrabajoEncoladoExitosoDejaElEstadoEnCompletado()
    {
        // Arrange
        _ = _mockBackupService
            .Setup(s => s.GenerateBackupAsync())
            .ReturnsAsync(
                new BackupResponseDto
                {
                    FileName = "backup_20260813_1200.sql",
                    FileSizeBytes = 2048,
                }
            );

        var result = await _controller.CreateBackup();
        var job = (result as AcceptedResult)!.Value as BackupJobDto;

        // Act: se ejecuta el trabajo tal como lo haría el servicio en segundo plano
        await _dispatcher.WorkItem!(BuildProvider(), CancellationToken.None);

        // Assert
        var final = _tracker.Find(job!.Id);
        _ = final!.Status.Should().Be(BackupJobStatus.Completado);
        _ = final.FileName.Should().Be("backup_20260813_1200.sql");
        _ = final.FileSizeBytes.Should().Be(2048);
        _ = final.FinishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TrabajoEncoladoQueFallaDejaElEstadoEnFallidoSinFiltrarDetalle()
    {
        // Arrange
        _ = _mockBackupService
            .Setup(s => s.GenerateBackupAsync())
            .ThrowsAsync(
                new InvalidOperationException("pg_dump falló (código 1): /var/backups/gsp/x.sql")
            );

        var result = await _controller.CreateBackup();
        var job = (result as AcceptedResult)!.Value as BackupJobDto;

        // Act
        await _dispatcher.WorkItem!(BuildProvider(), CancellationToken.None);

        // Assert: el detalle interno queda en el log, no en el estado consultable
        var final = _tracker.Find(job!.Id);
        _ = final!.Status.Should().Be(BackupJobStatus.Fallido);
        _ = final.Error.Should().NotContain("pg_dump");
        _ = final.Error.Should().NotContain("/var/backups");
    }

    [Fact]
    public async Task TrasFallarUnTrabajoSePuedeLanzarOtro()
    {
        // Arrange: un trabajo que falla no debe bloquear los siguientes
        _ = _mockBackupService
            .Setup(s => s.GenerateBackupAsync())
            .ThrowsAsync(new InvalidOperationException("falló"));

        var primera = await _controller.CreateBackup();
        await _dispatcher.WorkItem!(BuildProvider(), CancellationToken.None);

        // Act
        var segunda = await _controller.CreateBackup();

        // Assert
        var jobPrimera = (primera as AcceptedResult)!.Value as BackupJobDto;
        var jobSegunda = (segunda as AcceptedResult)!.Value as BackupJobDto;
        _ = jobSegunda!.Id.Should().NotBe(jobPrimera!.Id);
        _ = _dispatcher.EnqueueCount.Should().Be(2);
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/admin/backup/jobs/{id}
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBackupJobConIdExistenteRetorna200()
    {
        // Arrange
        var creado = await _controller.CreateBackup();
        var job = (creado as AcceptedResult)!.Value as BackupJobDto;

        // Act
        var result = _controller.GetBackupJob(job!.Id);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        _ = (ok.Value as BackupJobDto)!.Id.Should().Be(job.Id);
    }

    [Fact]
    public void GetBackupJobConIdInexistenteRetorna404()
    {
        // Act
        var result = _controller.GetBackupJob(Guid.NewGuid());

        // Assert
        _ = result.Should().BeOfType<NotFoundObjectResult>();
    }
}
