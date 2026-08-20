using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using SistemaServicios.API.Interfaces;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SistemaServicios.Tests")]

namespace SistemaServicios.API.Services;

public class EmailService : IEmailService
{
    private readonly ISmtpClientWrapper _smtpClient;
    private readonly string _from;

    public EmailService(IConfiguration config, ISmtpClientWrapper? smtpClient = null)
    {
        var host =
            config["SmtpSettings:Host"]
            ?? throw new InvalidOperationException("SMTP_HOST no configurado.");
        var port = int.Parse(
            config["SmtpSettings:Port"] ?? "587",
            System.Globalization.CultureInfo.InvariantCulture
        );
        var user =
            config["SmtpSettings:User"]
            ?? throw new InvalidOperationException("SMTP_USER no configurado.");
        var password =
            config["SmtpSettings:Password"]
            ?? throw new InvalidOperationException("SMTP_PASSWORD no configurado.");

        _from = config["SmtpSettings:From"] ?? user;
        _smtpClient = smtpClient ?? new SmtpClientWrapper(host, port, user, password);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_from, "SistemaServicios"),
            Subject = "Recuperación de contraseña — SistemaServicios",
            Body = BuildEmailBody(resetToken),
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        await _smtpClient.SendMailAsync(message);
    }

    internal static string BuildEmailBody(string resetToken) =>
        $"""
            <h2>Recuperación de contraseña</h2>
            <p>Has solicitado restablecer tu contraseña. Utiliza el siguiente token para definir tu nueva clave (válido por 15 minutos):</p>
            <h3 style="letter-spacing:2px;background:#f4f4f4;padding:10px;display:inline-block">{resetToken}</h3>
            <p>Si no solicitaste este cambio, puedes ignorar este mensaje; tu contraseña actual no ha sido modificada.</p>
            """;
}
