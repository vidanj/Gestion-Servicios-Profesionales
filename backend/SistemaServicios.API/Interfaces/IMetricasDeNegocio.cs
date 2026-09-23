using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

/// <summary>
/// Registro de los hechos de negocio que se quieren contar.
/// </summary>
/// <remarks>
/// <para>
/// Existe para poder distinguir "la API responde 200" de "el negocio funciona". Un fallo
/// que deja de crear solicitudes pero sigue devolviendo 200 es invisible para las métricas
/// de peticiones, que solo ven códigos de estado.
/// </para>
/// <para>
/// Se consume <b>solo desde <c>Services/</c></b>: un controlador mide lo que entró por
/// HTTP, mientras que un servicio mide lo que el negocio decidió, y esa diferencia es la
/// razón de ser de estas métricas (constitución, Principio II).
/// </para>
/// <para>
/// <b>Ningún método de este contrato lanza.</b> Una métrica que rompe un inicio de sesión
/// es peor que no tener métrica. Todos devuelven <c>void</c> a propósito: nadie debe poder
/// ramificar según el resultado de medir.
/// </para>
/// <para>
/// Ninguna implementación debe incluir datos personales ni identificadores en las
/// etiquetas: la métrica cuenta, el registro identifica. Por eso estos métodos reciben
/// enumeraciones y banderas, nunca entidades ni cadenas libres. El detalle está en
/// <c>specs/008-monitoreo-metricas-alertas/contracts/metricas.md</c>.
/// </para>
/// </remarks>
public interface IMetricasDeNegocio
{
    /// <summary>Cuenta un intento de creación de solicitud de servicio.</summary>
    /// <param name="creada">
    /// <c>true</c> si la solicitud se creó; <c>false</c> si el servicio no estaba disponible.
    /// </param>
    public void SolicitudCreada(bool creada);

    /// <summary>Cuenta un intento de cambio de estado de una solicitud.</summary>
    /// <param name="origen">Estado desde el que se intentó cambiar.</param>
    /// <param name="destino">Estado al que se intentó cambiar.</param>
    /// <param name="resultado">Cómo terminó el intento.</param>
    public void CambioDeEstado(
        RequestStatus origen,
        RequestStatus destino,
        ResultadoDeCambioDeEstado resultado
    );

    /// <summary>Cuenta un intento de inicio de sesión.</summary>
    /// <param name="resultado">Cómo terminó el intento.</param>
    public void IntentoDeAutenticacion(ResultadoDeAutenticacion resultado);

    /// <summary>Cuenta un intento de alta de usuario.</summary>
    /// <param name="creado"><c>true</c> si se creó; <c>false</c> si el correo ya existía.</param>
    public void UsuarioRegistrado(bool creado);

    /// <summary>Cuenta un respaldo terminado y registra cuánto tardó.</summary>
    /// <param name="resultado">Cómo terminó el respaldo.</param>
    /// <param name="duracion">Duración medida, en segundos.</param>
    public void RespaldoEjecutado(ResultadoDeRespaldo resultado, double duracion);
}
