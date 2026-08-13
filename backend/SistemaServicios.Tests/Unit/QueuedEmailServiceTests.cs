using System.Net.Mail;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Pruebas del envío de correo en segundo plano. Lo relevante es que la llamada retorna
/// sin esperar al servidor SMTP —antes el tiempo de respuesta del usuario dependía de la
/// latencia del proveedor— y que los fallos transitorios se reintentan sin llegar a él.
/// </summary>
public class QueuedEmailServiceTests
{
    private readonly FakeDispatcher _dispatcher = new();
    private readonly Mock<ISmtpClientWrapper> _smtp = new();
    private readonly QueuedEmailService _service;

    public QueuedEmailServiceTests()
    {
        _service = new QueuedEmailService(
            _dispatcher,
            NullLogger<QueuedEmailService>.Instance,
            // Sin espera real: si no, ejercitar los tres intentos costaría seis segundos.
            TimeSpan.Zero
        );
    }

    /// <summary>Proveedor que resuelve el EmailService real con un SMTP controlado.</summary>
    private IServiceProvider BuildProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["SmtpSettings:Host"] = "smtp.test",
                    ["SmtpSettings:Port"] = "587",
                    ["SmtpSettings:User"] = "test@test.com",
                    ["SmtpSettings:Password"] = "clave",
                    ["SmtpSettings:From"] = "test@test.com",
                }
            )
            .Build();

        var emailService = new EmailService(config, _smtp.Object);

        var provider = new Mock<IServiceProvider>();
        _ = provider.Setup(p => p.GetService(typeof(EmailService))).Returns(emailService);
        return provider.Object;
    }

    private async Task EjecutarTrabajoEncolado() =>
        await _dispatcher.WorkItem!(BuildProvider(), CancellationToken.None);

    // ─────────────────────────────────────────────────────────────
    // Encolado
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendPasswordResetEmailAsyncEncolaYNoEnviaEnLaPeticion()
    {
        // Act
        await _service.SendPasswordResetEmailAsync("juan@test.com", "NuevaPass123");

        // Assert: el envío no ocurre dentro de la petición
        _ = _dispatcher.EnqueueCount.Should().Be(1);
        _smtp.Verify(s => s.SendMailAsync(It.IsAny<MailMessage>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────
    // Reintentos
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrabajoEncoladoEnviaUnaSolaVezSiElPrimerIntentoFunciona()
    {
        // Arrange
        _ = _smtp.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>())).Returns(Task.CompletedTask);
        await _service.SendPasswordResetEmailAsync("juan@test.com", "NuevaPass123");

        // Act
        await EjecutarTrabajoEncolado();

        // Assert
        _smtp.Verify(s => s.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
    }

    [Fact]
    public async Task TrabajoEncoladoReintentaTrasUnFalloTransitorio()
    {
        // Arrange: falla el primer intento y funciona el segundo
        var intentos = 0;
        _ = _smtp
            .Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
            .Returns(() =>
            {
                intentos++;
                return intentos == 1
                    ? Task.FromException(new InvalidOperationException("fallo transitorio"))
                    : Task.CompletedTask;
            });

        await _service.SendPasswordResetEmailAsync("juan@test.com", "NuevaPass123");

        // Act
        await EjecutarTrabajoEncolado();

        // Assert
        _ = intentos.Should().Be(2);
    }

    [Fact]
    public async Task TrabajoEncoladoSeRindeTrasTresIntentosSinPropagarLaExcepcion()
    {
        // Arrange: el proveedor está caído de forma permanente
        _ = _smtp
            .Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
            .ThrowsAsync(new InvalidOperationException("SMTP caído"));

        await _service.SendPasswordResetEmailAsync("juan@test.com", "NuevaPass123");

        // Act
        var ex = await Record.ExceptionAsync(EjecutarTrabajoEncolado);

        // Assert: tres intentos y ninguna excepción escapa. Propagarla tumbaría el
        // consumidor de la cola; además, el usuario nunca debe enterarse de este fallo,
        // porque saber que su correo no salió revelaría que la cuenta existe.
        _ = ex.Should().BeNull();
        _smtp.Verify(s => s.SendMailAsync(It.IsAny<MailMessage>()), Times.Exactly(3));
    }
}
