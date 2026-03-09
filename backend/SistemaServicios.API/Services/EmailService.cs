using System.Net.Mail;
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

    public async Task SendPasswordResetEmailAsync(string toEmail, string newPassword)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_from, "SistemaServicios"),
            Subject = "Tu nueva contraseña — SistemaServicios",
            Body = BuildEmailBody(newPassword),
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        await _smtpClient.SendMailAsync(message);
    }

    internal static string BuildEmailBody(string newPassword) =>
        $"""
            <h2>Recuperación de contraseña</h2>
            <p>Tu nueva contraseña temporal es:</p>
            <h3 style="letter-spacing:2px">{newPassword}</h3>
            <p>Te recomendamos cambiarla después de iniciar sesión.</p>
            """;
}
