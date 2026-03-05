using FluentAssertions;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Pruebas unitarias del BackupService.
/// Se enfoca en la validación de variables de entorno (lógica pura, sin dependencias externas).
/// El ciclo completo (pg_dump real) se verifica en pruebas de integración con servicio mockeado.
/// </summary>
public class BackupServiceTests : IDisposable
{
    // Variables de entorno que gestiona este servicio
    private static readonly string[] EnvKeys =
    [
        "DB_HOST",
        "DB_PORT",
        "DB_NAME",
        "DB_USER",
        "DB_PASSWORD",
    ];

    // Guarda los valores originales para restaurarlos al terminar cada prueba
    private readonly Dictionary<string, string?> _valoresOriginales = new();

    public BackupServiceTests()
    {
        foreach (var key in EnvKeys)
        {
            _valoresOriginales[key] = Environment.GetEnvironmentVariable(key);
        }
    }

    public void Dispose()
    {
        foreach (var (key, value) in _valoresOriginales)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>Establece las cinco variables con valores sintéticos válidos.</summary>
    private static void SetVariablesValidas()
    {
        Environment.SetEnvironmentVariable("DB_HOST", "localhost");
        Environment.SetEnvironmentVariable("DB_PORT", "5432");
        Environment.SetEnvironmentVariable("DB_NAME", "testdb");
        Environment.SetEnvironmentVariable("DB_USER", "testuser");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "testpass");
    }

    // ─────────────────────────────────────────────────────────────
    // Validación de variables de entorno obligatorias
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateBackupAsyncSinDbHostLanzaInvalidOperationException()
    {
        // Arrange
        SetVariablesValidas();
        Environment.SetEnvironmentVariable("DB_HOST", null);
        var service = new BackupService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateBackupAsync()
        );

        _ = ex.Message.Should().Contain("DB_HOST no definido");
    }

    [Fact]
    public async Task GenerateBackupAsyncSinDbNameLanzaInvalidOperationException()
    {
        // Arrange
        SetVariablesValidas();
        Environment.SetEnvironmentVariable("DB_NAME", null);
        var service = new BackupService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateBackupAsync()
        );

        _ = ex.Message.Should().Contain("DB_NAME no definido");
    }

    [Fact]
    public async Task GenerateBackupAsyncSinDbUserLanzaInvalidOperationException()
    {
        // Arrange
        SetVariablesValidas();
        Environment.SetEnvironmentVariable("DB_USER", null);
        var service = new BackupService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateBackupAsync()
        );

        _ = ex.Message.Should().Contain("DB_USER no definido");
    }

    [Fact]
    public async Task GenerateBackupAsyncSinDbPasswordLanzaInvalidOperationException()
    {
        // Arrange
        SetVariablesValidas();
        Environment.SetEnvironmentVariable("DB_PASSWORD", null);
        var service = new BackupService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateBackupAsync()
        );

        _ = ex.Message.Should().Contain("DB_PASSWORD no definido");
    }

    // ─────────────────────────────────────────────────────────────
    // DB_PORT es opcional (valor por defecto 5432)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateBackupAsyncSinDbPortUsaElPuerto5432PorDefecto()
    {
        // Arrange: DB_PORT ausente — pg_dump debe intentar conectar al puerto 5432.
        // El proceso fallará porque no hay PostgreSQL real, pero queremos confirmar que
        // la excepción proviene de pg_dump y no de la validación de la variable.
        SetVariablesValidas();
        Environment.SetEnvironmentVariable("DB_PORT", null);
        var service = new BackupService();

        // Act
        var act = () => service.GenerateBackupAsync();

        // Assert: la excepción NO menciona "DB_PORT no definido" porque el puerto tiene default.
        // Puede ser InvalidOperationException (pg_dump no encontrado o falla) u otra excepción
        // del sistema de procesos — en cualquier caso, no es un error de validación de variables.
        var ex = await Record.ExceptionAsync(act);
        _ = ex.Should().NotBeNull();
        _ = ex!.Message.Should().NotContain("DB_PORT no definido");
    }

    // ─────────────────────────────────────────────────────────────
    // Construcción del servicio (no requiere variables de entorno)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void ConstructorSinVariablesDeEntornoNoCrash()
    {
        // Arrange: incluso sin variables configuradas, el constructor debe completarse.
        // Las variables se leen solo al llamar GenerateBackupAsync.
        foreach (var key in EnvKeys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }

        // Act & Assert: no debe lanzar excepción
        var act = () => new BackupService();
        _ = act.Should().NotThrow();
    }
}
