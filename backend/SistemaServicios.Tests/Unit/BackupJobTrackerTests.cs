using FluentAssertions;
using SistemaServicios.API.DTOs.Admin;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class BackupJobTrackerTests
{
    private readonly BackupJobTracker _tracker = new();

    [Fact]
    public void StartOrGetActiveCreaUnTrabajoPendiente()
    {
        // Act
        var (job, created) = _tracker.StartOrGetActive();

        // Assert
        _ = created.Should().BeTrue();
        _ = job.Status.Should().Be(BackupJobStatus.Pendiente);
    }

    [Fact]
    public void StartOrGetActiveConTrabajoPendienteDevuelveElMismo()
    {
        // Arrange
        var (primero, _) = _tracker.StartOrGetActive();

        // Act
        var (segundo, created) = _tracker.StartOrGetActive();

        // Assert: sin esto, dos peticiones lanzarían dos pg_dump a la vez
        _ = created.Should().BeFalse();
        _ = segundo.Id.Should().Be(primero.Id);
    }

    [Fact]
    public void StartOrGetActiveConTrabajoEnProcesoDevuelveElMismo()
    {
        // Arrange
        var (primero, _) = _tracker.StartOrGetActive();
        _tracker.MarkRunning(primero.Id);

        // Act
        var (segundo, created) = _tracker.StartOrGetActive();

        // Assert
        _ = created.Should().BeFalse();
        _ = segundo.Id.Should().Be(primero.Id);
    }

    [Fact]
    public void StartOrGetActiveTrasCompletarCreaUnoNuevo()
    {
        // Arrange
        var (primero, _) = _tracker.StartOrGetActive();
        _tracker.MarkCompleted(primero.Id, "backup_20260813_1200.sql", 100);

        // Act
        var (segundo, created) = _tracker.StartOrGetActive();

        // Assert
        _ = created.Should().BeTrue();
        _ = segundo.Id.Should().NotBe(primero.Id);
    }

    [Fact]
    public void StartOrGetActiveTrasFallarCreaUnoNuevo()
    {
        // Arrange: un fallo no debe dejar bloqueada la funcionalidad
        var (primero, _) = _tracker.StartOrGetActive();
        _tracker.MarkFailed(primero.Id, "algo salió mal");

        // Act
        var (_, created) = _tracker.StartOrGetActive();

        // Assert
        _ = created.Should().BeTrue();
    }

    [Fact]
    public void MarkCompletedGuardaNombreTamanoYFechaDeFin()
    {
        // Arrange
        var (job, _) = _tracker.StartOrGetActive();

        // Act
        _tracker.MarkCompleted(job.Id, "backup_20260813_1200.sql", 4096);

        // Assert
        var final = _tracker.Find(job.Id);
        _ = final!.Status.Should().Be(BackupJobStatus.Completado);
        _ = final.FileName.Should().Be("backup_20260813_1200.sql");
        _ = final.FileSizeBytes.Should().Be(4096);
        _ = final.FinishedAt.Should().NotBeNull();
    }

    [Fact]
    public void FindConIdInexistenteDevuelveNull()
    {
        // Act & Assert
        _ = _tracker.Find(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void FailActiveJobsMarcaLosSinTerminar()
    {
        // Arrange: es lo que se ejecuta al apagar el proceso
        var (pendiente, _) = _tracker.StartOrGetActive();
        _tracker.MarkRunning(pendiente.Id);

        // Act
        _tracker.FailActiveJobs("El servicio se detuvo.");

        // Assert: sin esto, el trabajo quedaría en EnProceso para siempre y
        // bloquearía cualquier respaldo posterior
        var final = _tracker.Find(pendiente.Id);
        _ = final!.Status.Should().Be(BackupJobStatus.Fallido);
        _ = final.Error.Should().Be("El servicio se detuvo.");
    }

    [Fact]
    public void FailActiveJobsNoTocaLosYaCompletados()
    {
        // Arrange
        var (job, _) = _tracker.StartOrGetActive();
        _tracker.MarkCompleted(job.Id, "backup_20260813_1200.sql", 10);

        // Act
        _tracker.FailActiveJobs("El servicio se detuvo.");

        // Assert
        _ = _tracker.Find(job.Id)!.Status.Should().Be(BackupJobStatus.Completado);
    }

    [Fact]
    public async Task StartOrGetActiveEsSeguroAnteLlamadasConcurrentes()
    {
        // Arrange & Act: veinte peticiones simultáneas
        var resultados = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => Task.Run(() => _tracker.StartOrGetActive()))
        );

        // Assert: exactamente una crea el trabajo; el resto recibe el mismo
        _ = resultados.Count(r => r.created).Should().Be(1);
        _ = resultados.Select(r => r.job.Id).Distinct().Should().ContainSingle();
    }
}
