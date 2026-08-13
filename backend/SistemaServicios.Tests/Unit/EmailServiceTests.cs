using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class EmailServiceTests
{
    private sealed class FakeSmtpClient : ISmtpClientWrapper
    {
        public MailMessage? MensajeEnviado { get; private set; }
        public bool DebeFallar { get; set; }

        public Task SendMailAsync(MailMessage message)
        {
            if (DebeFallar)
            {
                throw new SmtpException("fallo simulado");
            }

            MensajeEnviado = message;
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }

    private static IConfiguration BuildConfig(
        string? host = "smtp.test.com",
        string? port = "587",
        string? user = "user@test.com",
        string? password = "secret",
        string? from = null
    )
    {
        var dict = new Dictionary<string, string?>
        {
            ["SmtpSettings:Host"] = host,
            ["SmtpSettings:Port"] = port,
            ["SmtpSettings:User"] = user,
            ["SmtpSettings:Password"] = password,
            ["SmtpSettings:From"] = from,
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public void ConstructorConfigValidaNoLanzaExcepcion()
    {
        var ex = Record.Exception(() => new EmailService(BuildConfig(), new FakeSmtpClient()));
        Assert.Null(ex);
    }

    [Fact]
    public void ConstructorSinHostLanzaInvalidOperation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new EmailService(BuildConfig(host: null), new FakeSmtpClient())
        );
        Assert.Contains("SMTP_HOST", ex.Message);
    }

    [Fact]
    public void ConstructorSinUserLanzaInvalidOperation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new EmailService(BuildConfig(user: null), new FakeSmtpClient())
        );
        Assert.Contains("SMTP_USER", ex.Message);
    }

    [Fact]
    public void ConstructorSinPasswordLanzaInvalidOperation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new EmailService(BuildConfig(password: null), new FakeSmtpClient())
        );
        Assert.Contains("SMTP_PASSWORD", ex.Message);
    }

    [Fact]
    public void ConstructorSinFromUsaUserComoFrom()
    {
        var ex = Record.Exception(() =>
            new EmailService(BuildConfig(from: null), new FakeSmtpClient())
        );
        Assert.Null(ex);
    }

    [Fact]
    public void ConstructorPuertoInvalidoLanzaFormatException()
    {
        Assert.Throws<FormatException>(() =>
            new EmailService(BuildConfig(port: "abc"), new FakeSmtpClient())
        );
    }

    [Fact]
    public async Task SendPasswordResetEmailAsyncEnviaMensajeCorrecto()
    {
        var fake = new FakeSmtpClient();
        var service = new EmailService(BuildConfig(), fake);

        await service.SendPasswordResetEmailAsync("dest@test.com", "Pass123!");

        Assert.NotNull(fake.MensajeEnviado);
        Assert.Equal("dest@test.com", fake.MensajeEnviado!.To[0].Address);
        Assert.Contains("Tu nueva contraseña", fake.MensajeEnviado.Subject);
        Assert.True(fake.MensajeEnviado.IsBodyHtml);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsyncSmtpFallaPropagaExcepcion()
    {
        var fake = new FakeSmtpClient { DebeFallar = true };
        var service = new EmailService(BuildConfig(), fake);

        await Assert.ThrowsAsync<SmtpException>(() =>
            service.SendPasswordResetEmailAsync("dest@test.com", "Pass123!")
        );
    }

    [Fact]
    public void BuildEmailBodyContienePasswordYHtml()
    {
        var body = EmailService.BuildEmailBody("MiPass99");
        Assert.Contains("MiPass99", body);
        Assert.Contains("<h2>", body);
    }
}
