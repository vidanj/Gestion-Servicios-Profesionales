namespace SistemaServicios.API.Interfaces;

/// <summary>
/// Cómo terminó un intento de cambio de estado de una solicitud.
/// </summary>
public enum ResultadoDeCambioDeEstado
{
    /// <summary>La transición era válida y se aplicó.</summary>
    Aceptada,

    /// <summary>La transición no está permitida por la máquina de estados.</summary>
    Rechazada,

    /// <summary>Quien la pidió no tenía permiso sobre esa solicitud.</summary>
    NoAutorizada,
}

/// <summary>
/// Cómo terminó un intento de inicio de sesión.
/// </summary>
/// <remarks>
/// El servicio devuelve deliberadamente el mismo mensaje para credenciales incorrectas y
/// para cuenta desactivada, para no revelar cuál de las dos ocurrió. Distinguirlas aquí
/// <b>no</b> rompe esa decisión: la respuesta que ve el cliente sigue siendo idéntica, y la
/// métrica es agregada, interna y sin identificadores. Es lo que permite responder si la
/// gente no entra porque se equivoca o porque sus cuentas están desactivadas.
/// </remarks>
public enum ResultadoDeAutenticacion
{
    /// <summary>Credenciales correctas y cuenta activa.</summary>
    Exito,

    /// <summary>Usuario inexistente o contraseña incorrecta.</summary>
    CredencialesInvalidas,

    /// <summary>Credenciales correctas, pero la cuenta está desactivada.</summary>
    CuentaInactiva,
}

/// <summary>
/// Cómo terminó un respaldo.
/// </summary>
public enum ResultadoDeRespaldo
{
    /// <summary>El respaldo se generó.</summary>
    Exito,

    /// <summary>La herramienta de volcado no está instalada o no está en el PATH.</summary>
    HerramientaAusente,

    /// <summary>La herramienta corrió y terminó con error.</summary>
    FalloDeEjecucion,

    /// <summary>Falta alguna variable de conexión obligatoria.</summary>
    ConfiguracionIncompleta,
}
