namespace SistemaServicios.API.Interfaces;

/// <summary>
/// Resultado de ejecutar un proceso externo.
/// </summary>
/// <param name="ExecutableFound">
/// <c>false</c> cuando el binario no existe en el PATH. Se reporta como dato y no como
/// excepcion para que la capa superior distinga "no instalado" de "ejecuto y fallo",
/// y para que esa ruta sea verificable en pruebas sin depender de que el binario
/// realmente falte en la maquina que las corre.
/// </param>
/// <param name="ExitCode">Codigo de salida del proceso; -1 si nunca llego a arrancar.</param>
/// <param name="StandardError">Contenido completo de stderr.</param>
public record ProcessRunResult(bool ExecutableFound, int ExitCode, string StandardError);
