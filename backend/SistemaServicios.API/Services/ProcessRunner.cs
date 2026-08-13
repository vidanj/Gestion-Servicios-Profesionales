using System.ComponentModel;
using System.Diagnostics;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

public class ProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(
        string fileName,
        string arguments,
        IReadOnlyDictionary<string, string> environment,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(environment);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            _ = process.Start();
        }
        catch (Win32Exception)
        {
            // El binario no existe en el PATH del sistema.
            return new ProcessRunResult(false, -1, string.Empty);
        }

        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return new ProcessRunResult(true, process.ExitCode, stderr);
    }
}
