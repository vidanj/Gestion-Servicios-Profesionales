using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc.Testing;
using SistemaServicios.API.Telemetry;

namespace SistemaServicios.Tests.Integration;

/// <summary>
/// Escucha el medidor de negocio de la aplicación bajo prueba.
/// </summary>
/// <remarks>
/// A diferencia del ayudante de las pruebas unitarias, aquí <b>no</b> se crea un medidor
/// propio: se observa el que la aplicación ya tiene registrado en su contenedor. Es lo que
/// permite afirmar que la instrumentación está conectada de punta a punta y no solo que la
/// clase funciona en aislamiento.
/// </remarks>
public sealed class EscuchaDeMetricasDeAplicacion : IDisposable
{
    private readonly MeterListener _escucha;
    private readonly List<string> _resultados = [];
    private readonly Lock _candado = new();

    /// <summary>Empieza a escuchar el medidor de negocio de la aplicación.</summary>
    /// <param name="factory">Fábrica de la aplicación bajo prueba.</param>
    public EscuchaDeMetricasDeAplicacion(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

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
            (_, _, etiquetas, _) =>
            {
                foreach (var etiqueta in etiquetas)
                {
                    if (etiqueta.Key == "resultado" && etiqueta.Value is string valor)
                    {
                        lock (_candado)
                        {
                            _resultados.Add(valor);
                        }
                    }
                }
            }
        );

        _escucha.Start();
    }

    /// <summary>Valores de la etiqueta "resultado" capturados hasta ahora.</summary>
    public IReadOnlyList<string> Resultados()
    {
        lock (_candado)
        {
            return [.. _resultados];
        }
    }

    /// <inheritdoc />
    public void Dispose() => _escucha.Dispose();
}
