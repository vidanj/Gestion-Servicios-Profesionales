using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Pruebas unitarias del BackupService.
/// pg_dump se sustituye por un IProcessRunner mockeado: las rutas de fallo se verifican
/// sin depender de que el binario este o no instalado en la maquina que corre las pruebas.
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
    private readonly Dictionary<string, string?> _valoresOriginales = new(StringComparer.Ordinal);
    private readonly Mock<IProcessRunner> _processRunner = new();
    private readonly string _directorioTemporal;

    public BackupServiceTests()
    {
        foreach (var key in EnvKeys)
        {
            _valoresOriginales[key] = Environment.GetEnvironmentVariable(key);
        }

        _directorioTemporal = Path.Combine(Path.GetTempPath(), $"gsp_backups_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        foreach (var (key, value) in _valoresOriginales)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        if (Directory.Exists(_directorioTemporal))
        {
            Directory.Delete(_directorioTemporal, recursive: true);
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

    private BackupService CrearServicio()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["BackupSettings:Directory"] = _directorioTemporal,
                }
            )
            .Build();

        return new BackupService(_processRunner.Object, config);
    }

    /// <summary>Extrae la ruta que el servicio pasó en --file="..." a pg_dump.</summary>
    private static string RutaDeSalida(string argumentos)
    {
        const string marcador = "--file=\"";
        var inicio = argumentos.IndexOf(marcador, StringComparison.Ordinal) + marcador.Length;
        var fin = argumentos.IndexOf('"', inicio);
        return argumentos[inicio..fin];
    }

    /// <summary>
    /// Simula un pg_dump que termina bien: crea el archivo en la ruta solicitada,
    /// porque el servicio lee su tamaño con FileInfo después de ejecutarlo.
    /// </summary>
    private void SimularPgDumpExitoso(string contenido = "-- volcado de prueba")
    {
        _ = _processRunner
            .Setup(r =>
                r.RunAsync(
                    "pg_dump",
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(
                (
                    string _,
                    string argumentos,
                    IReadOnlyDictionary<string, string> _,
                    CancellationToken _
                ) =>
                {
                    File.WriteAllText(RutaDeSalida(argumentos), contenido);
                    return Task.FromResult(new ProcessRunResult(true, 0, string.Empty));
                }
            );
    }

    private void SimularRespuestaDePgDump(ProcessRunResult resultado)
    {
        _ = _processRunner
            .Setup(r =>
                r.RunAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(resultado);
    }

    // ─────────────────────────────────────────────────────────────
    // Validación de variables de entorno obligatorias
    // ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("DB_HOST")]
    [InlineData("DB_NAME")]
    [InlineData("DB_USER")]
    [InlineData("DB_PASSWORD")]
    public async Task GenerateBackupAsyncSinVariableObligatoriaLanzaInvalidOperationException(
        string variable
    )
    {
        // Arrange
        SetVariablesValidas();
        Environment.SetEnvironmentVariable(variable, null);
        var service = CrearServicio();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateBackupAsync()
        );

        _ = ex.Message.Should().Contain($"{variable} no definido");
    }

    // ─────────────────────────────────────────────────────────────
    // DB_PORT es opcional (valor por defecto 5432)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateBackupAsyncSinDbPortUsaElPuerto5432PorDefecto()
    {
        // Arrange
        SetVariablesValidas();
        Environment.SetEnvironmentVariable("DB_PORT", null);
        SimularPgDumpExitoso();
        var service = CrearServicio();

        // Act
        _ = await service.GenerateBackupAsync();

        // Assert: el puerto por defecto llega efectivamente en los argumentos de pg_dump
        _processRunner.Verify(
            r =>
                r.RunAsync(
                    "pg_dump",
                    It.Is<string>(a => a.Contains("--port=5432", StringComparison.Ordinal)),
                    It.IsAny<IReadOnlyDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    // ─────────────────────────────────────────────────────────────
    // Rutas de fallo de pg_dump
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateBackupAsyncConPgDumpAusenteLanzaExcepcionIndicandoloAsi()
    {
        // Arrange: este es el defecto del issue #133 — la imagen no trae pg_dump.
        // Antes solo se podía observar en máquinas donde el binario faltara de verdad.
        SetVariablesValidas();
        SimularRespuestaDePgDump(new ProcessRunResult(false, -1, string.Empty));
        var service = CrearServicio();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateBackupAsync()
        );

        _ = ex.Message.Should().Contain("pg_dump no encontrado");
    }

    [Fact]
    public async Task GenerateBackupAsyncConCodigoDeSalidaDistintoDeCeroPropagaElError()
    {
        // Arrange
        SetVariablesValidas();
        SimularRespuestaDePgDump(new ProcessRunResult(true, 3, "autenticación fallida"));
        var service = CrearServicio();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateBackupAsync()
        );

        _ = ex.Message.Should().Contain("código 3");
        _ = ex.Message.Should().Contain("autenticación fallida");
    }

    // ─────────────────────────────────────────────────────────────
    // Camino feliz
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateBackupAsyncExitosoDevuelveNombreConElPatronEsperado()
    {
        // Arrange
        SetVariablesValidas();
        SimularPgDumpExitoso();
        var service = CrearServicio();

        // Act
        var dto = await service.GenerateBackupAsync();

        // Assert
        _ = dto.FileName.Should().MatchRegex(@"^backup_\d{8}_\d{6}(_\d+)?\.sql$");
        _ = dto.FileSizeBytes.Should().BeGreaterThan(0);
        _ = File.Exists(Path.Combine(_directorioTemporal, dto.FileName)).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateBackupAsyncCreaElDirectorioConfiguradoSiNoExiste()
    {
        // Arrange: el directorio temporal aún no se ha creado
        _ = Directory.Exists(_directorioTemporal).Should().BeFalse();
        SetVariablesValidas();
        SimularPgDumpExitoso();
        var service = CrearServicio();

        // Act
        _ = await service.GenerateBackupAsync();

        // Assert
        _ = Directory.Exists(_directorioTemporal).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateBackupAsyncPasaLaContrasenaPorEntornoYNoPorLaLineaDeComandos()
    {
        // Arrange: PGPASSWORD en los argumentos quedaría visible en la lista de procesos
        SetVariablesValidas();
        SimularPgDumpExitoso();
        var service = CrearServicio();

        // Act
        _ = await service.GenerateBackupAsync();

        // Assert
        _processRunner.Verify(
            r =>
                r.RunAsync(
                    "pg_dump",
                    It.Is<string>(a => !a.Contains("testpass", StringComparison.Ordinal)),
                    It.Is<IReadOnlyDictionary<string, string>>(e => e["PGPASSWORD"] == "testpass"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    // ─────────────────────────────────────────────────────────────
    // Construcción del servicio
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void ConstructorSinVariablesDeEntornoNoCrash()
    {
        // Arrange: las variables se leen solo al llamar GenerateBackupAsync
        foreach (var key in EnvKeys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }

        // Act & Assert
        var act = CrearServicio;
        _ = act.Should().NotThrow();
    }

    [Fact]
    public void ConstructorNoCreaElDirectorioDeRespaldos()
    {
        // Arrange & Act: crear carpetas al resolver la dependencia sería un efecto
        // secundario en tiempo de arranque de la aplicación
        _ = CrearServicio();

        // Assert
        _ = Directory.Exists(_directorioTemporal).Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────
    // ListBackups
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void ListBackupsSinDirectorioDevuelveListaVacia()
    {
        // Arrange
        var service = CrearServicio();

        // Act & Assert
        _ = service.ListBackups().Should().BeEmpty();
    }

    [Fact]
    public void ListBackupsIgnoraArchivosQueNoSigueElPatronDeNombre()
    {
        // Arrange
        _ = Directory.CreateDirectory(_directorioTemporal);
        File.WriteAllText(Path.Combine(_directorioTemporal, "backup_20260101_1200.sql"), "a");
        File.WriteAllText(Path.Combine(_directorioTemporal, "otro_archivo.sql"), "b");
        File.WriteAllText(Path.Combine(_directorioTemporal, "notas.txt"), "c");
        var service = CrearServicio();

        // Act
        var resultado = service.ListBackups();

        // Assert
        _ = resultado.Should().ContainSingle();
        _ = resultado[0].FileName.Should().Be("backup_20260101_1200.sql");
    }

    // ─────────────────────────────────────────────────────────────
    // OpenBackup — defensa contra path traversal
    // ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\Windows\\win.ini")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("backup_20260101_1200.sql/../../secreto.sql")]
    [InlineData("..")]
    [InlineData("")]
    [InlineData("otro_archivo.sql")]
    [InlineData("backup_2026_12.sql")]
    public void OpenBackupConNombreNoValidoDevuelveNull(string nombre)
    {
        // Arrange
        _ = Directory.CreateDirectory(_directorioTemporal);
        var service = CrearServicio();

        // Act
        using var stream = service.OpenBackup(nombre);

        // Assert
        _ = stream.Should().BeNull();
    }

    [Fact]
    public void OpenBackupConNombreValidoInexistenteDevuelveNull()
    {
        // Arrange: mismo resultado que un nombre inválido, para no revelar qué existe
        _ = Directory.CreateDirectory(_directorioTemporal);
        var service = CrearServicio();

        // Act
        using var stream = service.OpenBackup("backup_20990101_0000.sql");

        // Assert
        _ = stream.Should().BeNull();
    }

    [Fact]
    public void OpenBackupConArchivoExistenteDevuelveSuContenido()
    {
        // Arrange
        _ = Directory.CreateDirectory(_directorioTemporal);
        const string nombre = "backup_20260101_1200.sql";
        File.WriteAllText(Path.Combine(_directorioTemporal, nombre), "-- contenido");
        var service = CrearServicio();

        // Act
        using var stream = service.OpenBackup(nombre);

        // Assert
        _ = stream.Should().NotBeNull();
        using var lector = new StreamReader(stream!);
        _ = lector.ReadToEnd().Should().Be("-- contenido");
    }

    [Fact]
    public async Task GenerateBackupAsyncDosRespaldosSeguidosNoCompartenNombre()
    {
        // Arrange: con precisión de minuto, dos respaldos del mismo minuto producían
        // el mismo nombre y el segundo sobrescribía al primero sin aviso (issue #162).
        SetVariablesValidas();
        SimularPgDumpExitoso();
        var service = CrearServicio();

        // Act
        var primero = await service.GenerateBackupAsync();
        var segundo = await service.GenerateBackupAsync();

        // Assert
        _ = segundo.FileName.Should().NotBe(primero.FileName);
        _ = Directory.GetFiles(_directorioTemporal, "*.sql").Should().HaveCount(2);
    }

    [Fact]
    public void OpenBackupAceptaElFormatoAntiguoDeCuatroDigitos()
    {
        // Arrange: los respaldos creados antes del cambio deben seguir descargándose.
        // Si la lista blanca solo aceptara seis dígitos, desaparecerían de la lista.
        _ = Directory.CreateDirectory(_directorioTemporal);
        const string nombreAntiguo = "backup_20260813_2017.sql";
        File.WriteAllText(Path.Combine(_directorioTemporal, nombreAntiguo), "-- antiguo");
        var service = CrearServicio();

        // Act
        using var stream = service.OpenBackup(nombreAntiguo);

        // Assert
        _ = stream.Should().NotBeNull();
    }

    [Fact]
    public void ListBackupsIncluyeAmbosFormatosDeNombre()
    {
        // Arrange
        _ = Directory.CreateDirectory(_directorioTemporal);
        File.WriteAllText(Path.Combine(_directorioTemporal, "backup_20260813_2017.sql"), "a");
        File.WriteAllText(Path.Combine(_directorioTemporal, "backup_20260813_201755.sql"), "b");
        var service = CrearServicio();

        // Act
        var resultado = service.ListBackups();

        // Assert
        _ = resultado.Should().HaveCount(2);
    }
}
