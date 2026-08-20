using Microsoft.Extensions.DependencyInjection;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

/// <summary>
/// Envuelve el envío real y lo saca de la petición HTTP. El usuario deja de esperar a
/// que responda el servidor SMTP, y de paso el tiempo de respuesta deja de delatar si
/// la cuenta existe: antes, encontrarla implicaba esperar al envío completo.
/// </summary>
public partial class QueuedEmailService : IEmailService
{
    private const int MaxIntentos = 3;

    private readonly IBackgroundTaskDispatcher _queue;
    private readonly ILogger<QueuedEmailService> _logger;
    private readonly TimeSpan _esperaBase;

    /// <param name="esperaBase">
    /// Base del backoff exponencial. Es inyectable para que las pruebas puedan
    /// ejercitar los reintentos sin esperar segundos reales.
    /// </param>
    public QueuedEmailService(
        IBackgroundTaskDispatcher queue,
        ILogger<QueuedEmailService> logger,
        TimeSpan? esperaBase = null
    )
    {
        _queue = queue;
        _logger = logger;
        _esperaBase = esperaBase ?? TimeSpan.FromSeconds(1);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string newPassword)
    {
        await _queue.EnqueueAsync(
            async (serviceProvider, cancellationToken) =>
            {
                var sender = serviceProvider.GetRequiredService<EmailService>();

                for (var intento = 1; intento <= MaxIntentos; intento++)
                {
                    try
                    {
                        await sender.SendPasswordResetEmailAsync(toEmail, newPassword);
                        return;
                    }
                    catch (Exception ex)
                    {
                        if (intento == MaxIntentos)
                        {
                            // Agotados los intentos, el fallo se registra pero no se
                            // propaga: al usuario nunca debe llegarle, porque saber que su
                            // correo no salió revelaría que la cuenta existe.
                            LogFalloDefinitivo(MaxIntentos, ex);
                            return;
                        }

                        // Fallo transitorio del proveedor: se reintenta con espera creciente.
                        LogReintento(intento, ex);
                        var espera = _esperaBase * Math.Pow(2, intento);
                        await Task.Delay(espera, cancellationToken);
                    }
                }
            }
        );
    }

    [LoggerMessage(
        EventId = 2100,
        Level = LogLevel.Warning,
        Message = "Falló el envío de correo en el intento {Intento}; se reintentará"
    )]
    private partial void LogReintento(int intento, Exception exception);

    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Error,
        Message = "No se pudo enviar el correo de recuperación tras {Intentos} intentos"
    )]
    private partial void LogFalloDefinitivo(int intentos, Exception exception);
}
