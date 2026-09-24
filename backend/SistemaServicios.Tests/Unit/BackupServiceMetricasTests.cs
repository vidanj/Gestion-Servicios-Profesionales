using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Comprueba que el servicio de respaldos cuenta cada desenlace y mide su duración.
/// </summary>
/// <remarks>
/// La duración se registra también cuando el respaldo falla, y a propósito: un respaldo que
/// revienta a los dos segundos y otro que agota su plazo son dos problemas distintos, y el
/// tiempo es lo único que los separa.
/// </remarks>
public class BackupServiceMetricasTests : IDisposable
{
    private static readonly string[] Variables =
    [
        "DB_HOST",
        "DB_PORT",
        "DB_NAME",
        "DB_USER",
        "DB_PASSWORD",
    ];

    private readonly Dictionary<string, string?> _originales = new(StringComparer.Ordinal);
    private readonly Mock<IProcessRunner> _procesos = new();
    private readonly Mock<IMetricasDeNegocio> _metricas = new();
    private readonly string _directorio;

    public BackupServiceMetricasTests()
    {
        foreach (var clave in Variables)
        {
            _originales[clave] = Environment.GetEnvironmentVariable(clave);
        }

        _directorio = Path.Combine(Path.GetTempPath(), $"gsp_metricas_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        foreach (var (clave, valor) in _originales)
        {
            Environment.SetEnvironmentVariable(clave, valor);
        }

        if (Directory.Exists(_directorio))
        {
            Directory.Delete(_directorio, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task RespaldoCorrectoCuentaExitoYRegistraDuracion()
    {
        ConfigurarEntorno();
        SimularPgDump(new ProcessRunResult(true, 0, string.Empty), creaArchivo: true);

        _ = await CrearServicio().GenerateBackupAsync();

        _metricas.Verify(
            m => m.RespaldoEjecutado(ResultadoDeRespaldo.Exito, It.Is<double>(d => d >= 0)),
            Times.Once
        );
    }

    [Fact]
    public async Task HerramientaAusenteCuentaSuMotivoPropio()
    {
        ConfigurarEntorno();
        SimularPgDump(new ProcessRunResult(false, -1, string.Empty), creaArchivo: false);

        var accion = () => CrearServicio().GenerateBackupAsync();

        _ = await accion.Should().ThrowAsync<InvalidOperationException>();
        _metricas.Verify(
            m =>
                m.RespaldoEjecutado(
                    ResultadoDeRespaldo.HerramientaAusente,
                    It.Is<double>(d => d >= 0)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task FalloDeEjecucionCuentaSuMotivoPropio()
    {
        ConfigurarEntorno();
        SimularPgDump(new ProcessRunResult(true, 3, "autenticación fallida"), creaArchivo: false);

        var accion = () => CrearServicio().GenerateBackupAsync();

        _ = await accion.Should().ThrowAsync<InvalidOperationException>();
        _metricas.Verify(
            m =>
                m.RespaldoEjecutado(
                    ResultadoDeRespaldo.FalloDeEjecucion,
                    It.Is<double>(d => d >= 0)
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Si falta una variable de conexión, el fallo ocurre antes de intentar nada y se cuenta
    /// aparte: es un problema de despliegue, no del respaldo.
    /// </summary>
    [Fact]
    public async Task ConfiguracionIncompletaSeCuentaAparte()
    {
        ConfigurarEntorno();
        Environment.SetEnvironmentVariable("DB_HOST", null);

        var accion = () => CrearServicio().GenerateBackupAsync();

        _ = await accion.Should().ThrowAsync<InvalidOperationException>();
        _metricas.Verify(
            m =>
                m.RespaldoEjecutado(
                    ResultadoDeRespaldo.ConfiguracionIncompleta,
                    It.Is<double>(d => d >= 0)
                ),
            Times.Once
        );
    }

    private static void ConfigurarEntorno()
    {
        Environment.SetEnvironmentVariable("DB_HOST", "localhost");
        Environment.SetEnvironmentVariable("DB_PORT", "5432");
        Environment.SetEnvironmentVariable("DB_NAME", "gsp");
        Environment.SetEnvironmentVariable("DB_USER", "usuario");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "clave-de-prueba");
    }

    private void SimularPgDump(ProcessRunResult resultado, bool creaArchivo)
    {
        _procesos
            .Setup(p =>
                p.RunAsync(
                    It.IsAny<string>(),
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
                    if (creaArchivo)
                    {
                        // El servicio consulta el tamaño del archivo generado; sin crearlo,
                        // fallaría por una razón distinta de la que se quiere probar.
                        File.WriteAllText(RutaDeSalida(argumentos), "-- respaldo de prueba");
                    }

                    return Task.FromResult(resultado);
                }
            );
    }

    private static string RutaDeSalida(string argumentos)
    {
        const string marcador = "--file=\"";
        var inicio = argumentos.IndexOf(marcador, StringComparison.Ordinal) + marcador.Length;
        var fin = argumentos.IndexOf('"', inicio);
        return argumentos[inicio..fin];
    }

    private BackupService CrearServicio()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["BackupSettings:Directory"] = _directorio,
                }
            )
            .Build();

        return new BackupService(_procesos.Object, config, _metricas.Object);
    }
}
