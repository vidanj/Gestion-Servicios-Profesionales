using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using SistemaServicios.API.Telemetry;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Captura las mediciones de un medidor para poder afirmar sobre ellas.
/// </summary>
/// <remarks>
/// Es el espejo de <see cref="SinkDeMemoria"/>, que hace lo mismo con los registros, y se
/// escribe aquí por el mismo motivo: <c>MeterListener</c> ya viene en la plataforma, así que
/// no hace falta añadir un paquete de pruebas más a un proyecto que ya arrastra bastantes
/// alertas de dependencias.
/// </remarks>
public sealed class EscuchaDeMetricas : IDisposable
{
    private readonly MeterListener _escucha;
    private readonly List<Medicion> _mediciones = [];
    private readonly Lock _candado = new();
    private readonly ServiceProvider _proveedor;

    /// <summary>
    /// Crea un medidor de negocio real y empieza a escuchar lo que emite.
    /// </summary>
    public EscuchaDeMetricas()
    {
        _proveedor = new ServiceCollection().AddMetrics().BuildServiceProvider();
        Metricas = new MetricasDeNegocio(_proveedor.GetRequiredService<IMeterFactory>());

        _escucha = new MeterListener
        {
            InstrumentPublished = (instrumento, escucha) =>
            {
                if (instrumento.Meter.Name == MetricasDeNegocio.NombreDelMedidor)
                {
                    escucha.EnableMeasurementEvents(instrumento);
                }
            },
        };

        _escucha.SetMeasurementEventCallback<long>(
            (instrumento, valor, etiquetas, _) => Anotar(instrumento.Name, valor, etiquetas)
        );
        _escucha.SetMeasurementEventCallback<double>(
            (instrumento, valor, etiquetas, _) => Anotar(instrumento.Name, valor, etiquetas)
        );

        _escucha.Start();
    }

    /// <summary>Medidor real que se está observando.</summary>
    public MetricasDeNegocio Metricas { get; }

    /// <summary>Mediciones capturadas, en orden de emisión.</summary>
    public IReadOnlyList<Medicion> Mediciones
    {
        get
        {
            lock (_candado)
            {
                return [.. _mediciones];
            }
        }
    }

    /// <summary>Devuelve las mediciones de un instrumento concreto.</summary>
    /// <param name="instrumento">Nombre completo del instrumento.</param>
    public IReadOnlyList<Medicion> De(string instrumento) =>
        [.. Mediciones.Where(m => m.Instrumento == instrumento)];

    /// <inheritdoc />
    public void Dispose()
    {
        _escucha.Dispose();
        Metricas.Dispose();
        _proveedor.Dispose();
    }

    private void Anotar(
        string instrumento,
        double valor,
        ReadOnlySpan<KeyValuePair<string, object?>> etiquetas
    )
    {
        var copia = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var etiqueta in etiquetas)
        {
            copia[etiqueta.Key] = etiqueta.Value?.ToString() ?? string.Empty;
        }

        // Las mediciones pueden llegar desde varios hilos; sin el candado la lista se
        // corrompe y el fallo aparece como una prueba intermitente.
        lock (_candado)
        {
            _mediciones.Add(new Medicion(instrumento, valor, copia));
        }
    }
}

/// <summary>Una medición capturada.</summary>
/// <param name="Instrumento">Nombre del instrumento que la emitió.</param>
/// <param name="Valor">Valor registrado.</param>
/// <param name="Etiquetas">Etiquetas de la medición.</param>
public sealed record Medicion(
    string Instrumento,
    double Valor,
    IReadOnlyDictionary<string, string> Etiquetas
)
{
    /// <summary>Valor de una etiqueta, o cadena vacía si no está.</summary>
    /// <param name="clave">Nombre de la etiqueta.</param>
    public string Etiqueta(string clave) =>
        Etiquetas.TryGetValue(clave, out var valor) ? valor : string.Empty;
}
