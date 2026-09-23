using FluentAssertions;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Comprueba qué emite el medidor de negocio y, sobre todo, qué NO emite.
/// </summary>
public class MetricasDeNegocioTests
{
    [Fact]
    public void SolicitudCreadaEmiteElResultadoCorrecto()
    {
        using var escucha = new EscuchaDeMetricas();

        escucha.Metricas.SolicitudCreada(creada: true);
        escucha.Metricas.SolicitudCreada(creada: false);

        var mediciones = escucha.De("gsp.solicitudes.creadas");
        mediciones.Should().HaveCount(2);
        mediciones[0].Valor.Should().Be(1);
        mediciones[0].Etiqueta("resultado").Should().Be("creada");
        mediciones[1].Etiqueta("resultado").Should().Be("servicio_no_disponible");
    }

    [Fact]
    public void CambioDeEstadoEmiteOrigenDestinoYResultado()
    {
        using var escucha = new EscuchaDeMetricas();

        escucha.Metricas.CambioDeEstado(
            RequestStatus.Pending,
            RequestStatus.Accepted,
            ResultadoDeCambioDeEstado.Aceptada
        );

        var medicion = escucha
            .De("gsp.solicitudes.cambios_estado")
            .Should()
            .ContainSingle()
            .Subject;
        medicion.Etiqueta("estado_origen").Should().Be("Pending");
        medicion.Etiqueta("estado_destino").Should().Be("Accepted");
        medicion.Etiqueta("resultado").Should().Be("aceptada");
    }

    [Theory]
    [InlineData(ResultadoDeAutenticacion.Exito, "exito")]
    [InlineData(ResultadoDeAutenticacion.CredencialesInvalidas, "credenciales_invalidas")]
    [InlineData(ResultadoDeAutenticacion.CuentaInactiva, "cuenta_inactiva")]
    public void IntentoDeAutenticacionTraduceElResultado(
        ResultadoDeAutenticacion resultado,
        string esperado
    )
    {
        using var escucha = new EscuchaDeMetricas();

        escucha.Metricas.IntentoDeAutenticacion(resultado);

        escucha
            .De("gsp.autenticacion.intentos")
            .Should()
            .ContainSingle()
            .Which.Etiqueta("resultado")
            .Should()
            .Be(esperado);
    }

    [Fact]
    public void RespaldoEjecutadoCuentaElMotivoYResumeLaDuracion()
    {
        using var escucha = new EscuchaDeMetricas();

        escucha.Metricas.RespaldoEjecutado(ResultadoDeRespaldo.HerramientaAusente, 2.5);

        // El contador conserva el motivo detallado...
        escucha
            .De("gsp.respaldos.ejecutados")
            .Should()
            .ContainSingle()
            .Which.Etiqueta("resultado")
            .Should()
            .Be("herramienta_ausente");

        // ...y el histograma lo resume a exito/fallo, para no multiplicar por cuatro los
        // tramos sin que la pregunta "cuanto tardan los respaldos" gane nada.
        var duracion = escucha.De("gsp.respaldos.duracion").Should().ContainSingle().Subject;
        duracion.Valor.Should().Be(2.5);
        duracion.Etiqueta("resultado").Should().Be("fallo");
    }

    /// <summary>
    /// Es el requisito FR-026, y se prueba porque una fuga de datos personales a una
    /// etiqueta no se nota: la métrica sigue funcionando, solo que multiplica sus series y
    /// expone en el sistema de monitoreo algo que no debería estar ahí.
    /// </summary>
    [Fact]
    public void NingunaEtiquetaContieneDatosPersonalesNiIdentificadores()
    {
        using var escucha = new EscuchaDeMetricas();

        escucha.Metricas.SolicitudCreada(creada: true);
        escucha.Metricas.CambioDeEstado(
            RequestStatus.InProgress,
            RequestStatus.Completed,
            ResultadoDeCambioDeEstado.Aceptada
        );
        escucha.Metricas.IntentoDeAutenticacion(ResultadoDeAutenticacion.CredencialesInvalidas);
        escucha.Metricas.UsuarioRegistrado(creado: true);
        escucha.Metricas.RespaldoEjecutado(ResultadoDeRespaldo.Exito, 12);

        var permitidas = new[] { "resultado", "estado_origen", "estado_destino" };

        var claves = escucha.Mediciones.SelectMany(m => m.Etiquetas.Keys).Distinct();
        claves.Should().BeSubsetOf(permitidas, "el catálogo no declara ninguna etiqueta más");

        var valores = escucha.Mediciones.SelectMany(m => m.Etiquetas.Values);
        valores
            .Should()
            .NotContain(v => v.Contains('@', StringComparison.Ordinal), "sería un correo")
            .And.NotContain(v => EsIdentificador(v), "sería un identificador de entidad");
    }

    // Se extrae a un método porque un árbol de expresión no admite descartes, y el
    // parámetro de salida de TryParse obliga a uno.
    private static bool EsIdentificador(string valor) => Guid.TryParse(valor, out _);

    /// <summary>
    /// FR-033: una métrica no puede tumbar la operación que la emite, tampoco cuando el
    /// medidor ya fue liberado durante el apagado de la aplicación.
    /// </summary>
    [Fact]
    public void UsarElMedidorDespuesDeLiberarloNoLanza()
    {
        var escucha = new EscuchaDeMetricas();
        var metricas = escucha.Metricas;
        escucha.Dispose();

        var accion = () =>
        {
            metricas.SolicitudCreada(creada: true);
            metricas.IntentoDeAutenticacion(ResultadoDeAutenticacion.Exito);
            metricas.RespaldoEjecutado(ResultadoDeRespaldo.Exito, 1);
        };

        accion.Should().NotThrow();
    }
}
