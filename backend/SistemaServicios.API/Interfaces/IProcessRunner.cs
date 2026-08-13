namespace SistemaServicios.API.Interfaces;

/// <summary>
/// Ejecuta un binario externo. Existe para que los servicios que dependen de una
/// herramienta del sistema (por ejemplo pg_dump) puedan probarse sin invocarla.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Ejecuta <paramref name="fileName"/> y espera a que termine, capturando stderr.
    /// </summary>
    /// <param name="environment">
    /// Variables de entorno adicionales para el proceso hijo. Se usan para pasar
    /// secretos que no deben aparecer en la linea de comandos.
    /// </param>
    public Task<ProcessRunResult> RunAsync(
        string fileName,
        string arguments,
        IReadOnlyDictionary<string, string> environment,
        CancellationToken cancellationToken = default
    );
}
