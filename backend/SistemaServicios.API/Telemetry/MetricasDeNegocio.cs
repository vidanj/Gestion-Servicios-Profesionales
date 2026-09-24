using System.Diagnostics.Metrics;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Telemetry;

/// <summary>
/// Emisión de las métricas de negocio.
/// </summary>
/// <remarks>
/// <para>
/// Vive en <c>Telemetry/</c> y no en <c>Services/</c> porque no es lógica de negocio sino
/// infraestructura transversal, igual que los enriquecedores de <c>Logging/</c>.
/// </para>
/// <para>
/// Se registra como instancia única: un medidor es un recurso de proceso, y crear uno por
/// petición multiplicaría los ámbitos y la memoria sin ganar nada. Los instrumentos se
/// construyen una sola vez, aquí en el constructor.
/// </para>
/// </remarks>
public sealed class MetricasDeNegocio : IMetricasDeNegocio, IDisposable
{
    /// <summary>
    /// Nombre del medidor. Es constante pública porque <see cref="Extensions.TelemetryConfiguration"/>
    /// tiene que pedirlo por nombre: dos literales iguales en archivos distintos acaban
    /// divergiendo, y cuando eso pasa las métricas simplemente dejan de aparecer, sin error.
    /// </summary>
    public const string NombreDelMedidor = "SistemaServicios.API.Negocio";

    private readonly Meter _medidor;
    private readonly Counter<long> _solicitudesCreadas;
    private readonly Counter<long> _cambiosDeEstado;
    private readonly Counter<long> _intentosDeAutenticacion;
    private readonly Counter<long> _usuariosRegistrados;
    private readonly Counter<long> _respaldosEjecutados;
    private readonly Histogram<double> _duracionDeRespaldos;

    /// <summary>
    /// Crea el medidor y sus instrumentos.
    /// </summary>
    /// <param name="fabrica">Fábrica de medidores del contenedor de dependencias.</param>
    public MetricasDeNegocio(IMeterFactory fabrica)
    {
        ArgumentNullException.ThrowIfNull(fabrica);

        _medidor = fabrica.Create(NombreDelMedidor);

        _solicitudesCreadas = _medidor.CreateCounter<long>(
            "gsp.solicitudes.creadas",
            unit: "{solicitud}",
            description: "Intentos de creación de solicitud de servicio, por resultado."
        );

        _cambiosDeEstado = _medidor.CreateCounter<long>(
            "gsp.solicitudes.cambios_estado",
            unit: "{cambio}",
            description: "Intentos de cambio de estado, con su transición y su resultado."
        );

        _intentosDeAutenticacion = _medidor.CreateCounter<long>(
            "gsp.autenticacion.intentos",
            unit: "{intento}",
            description: "Intentos de inicio de sesión, por resultado."
        );

        _usuariosRegistrados = _medidor.CreateCounter<long>(
            "gsp.usuarios.registrados",
            unit: "{usuario}",
            description: "Intentos de alta de usuario, por resultado."
        );

        _respaldosEjecutados = _medidor.CreateCounter<long>(
            "gsp.respaldos.ejecutados",
            unit: "{respaldo}",
            description: "Respaldos terminados, por resultado."
        );

        _duracionDeRespaldos = _medidor.CreateHistogram<double>(
            "gsp.respaldos.duracion",
            unit: "s",
            description: "Duración de los respaldos, en segundos."
        );
    }

    /// <inheritdoc />
    public void SolicitudCreada(bool creada) =>
        _solicitudesCreadas.Add(
            1,
            Etiqueta("resultado", creada ? "creada" : "servicio_no_disponible")
        );

    /// <inheritdoc />
    public void CambioDeEstado(
        RequestStatus origen,
        RequestStatus destino,
        ResultadoDeCambioDeEstado resultado
    ) =>
        _cambiosDeEstado.Add(
            1,
            Etiqueta("estado_origen", origen.ToString()),
            Etiqueta("estado_destino", destino.ToString()),
            Etiqueta("resultado", Texto(resultado))
        );

    /// <inheritdoc />
    public void IntentoDeAutenticacion(ResultadoDeAutenticacion resultado) =>
        _intentosDeAutenticacion.Add(1, Etiqueta("resultado", Texto(resultado)));

    /// <inheritdoc />
    public void UsuarioRegistrado(bool creado) =>
        _usuariosRegistrados.Add(1, Etiqueta("resultado", creado ? "creado" : "correo_duplicado"));

    /// <inheritdoc />
    public void RespaldoEjecutado(ResultadoDeRespaldo resultado, double duracion)
    {
        var texto = Texto(resultado);
        _respaldosEjecutados.Add(1, Etiqueta("resultado", texto));

        // El histograma usa solo éxito/fallo y no el motivo detallado: los tramos se
        // multiplicarían por cuatro sin que la pregunta "¿cuánto tardan los respaldos?"
        // gane nada con esa división.
        _duracionDeRespaldos.Record(
            duracion,
            Etiqueta("resultado", resultado == ResultadoDeRespaldo.Exito ? "exito" : "fallo")
        );
    }

    /// <summary>Libera el medidor y, con él, sus instrumentos.</summary>
    public void Dispose() => _medidor.Dispose();

    private static KeyValuePair<string, object?> Etiqueta(string clave, string valor) =>
        new(clave, valor);

    // Los nombres de etiqueta se escriben aquí, en minúsculas y sin acentos, y no se
    // derivan de la enumeración con ToString(): renombrar un valor en C# es refactorización
    // normal, pero renombrar una etiqueta rompe en silencio todos los tableros y alertas
    // que la consultan. Esta traducción explícita es lo que desacopla ambas cosas.
    private static string Texto(ResultadoDeCambioDeEstado resultado) =>
        resultado switch
        {
            ResultadoDeCambioDeEstado.Aceptada => "aceptada",
            ResultadoDeCambioDeEstado.Rechazada => "rechazada",
            ResultadoDeCambioDeEstado.NoAutorizada => "no_autorizada",
            _ => "desconocido",
        };

    private static string Texto(ResultadoDeAutenticacion resultado) =>
        resultado switch
        {
            ResultadoDeAutenticacion.Exito => "exito",
            ResultadoDeAutenticacion.CredencialesInvalidas => "credenciales_invalidas",
            ResultadoDeAutenticacion.CuentaInactiva => "cuenta_inactiva",
            _ => "desconocido",
        };

    private static string Texto(ResultadoDeRespaldo resultado) =>
        resultado switch
        {
            ResultadoDeRespaldo.Exito => "exito",
            ResultadoDeRespaldo.HerramientaAusente => "herramienta_ausente",
            ResultadoDeRespaldo.FalloDeEjecucion => "fallo_ejecucion",
            ResultadoDeRespaldo.ConfiguracionIncompleta => "configuracion_incompleta",
            _ => "desconocido",
        };
}
