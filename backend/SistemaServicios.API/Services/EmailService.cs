using System.Net;
using System.Net.Mail;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

public class EmailService : IEmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _user;
    private readonly string _password;
    private readonly string _from;

    public EmailService(IConfiguration config)
    {
        _host =
            config["SmtpSettings:Host"]
            ?? throw new InvalidOperationException("SMTP_HOST no configurado.");
        _port = int.Parse(
            config["SmtpSettings:Port"] ?? "587",
            System.Globalization.CultureInfo.InvariantCulture
        );
        _user =
            config["SmtpSettings:User"]
            ?? throw new InvalidOperationException("SMTP_USER no configurado.");
        _password =
            config["SmtpSettings:Password"]
            ?? throw new InvalidOperationException("SMTP_PASSWORD no configurado.");
        _from = config["SmtpSettings:From"] ?? _user;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string newPassword)
    {
        using var client = new SmtpClient(_host, _port)
        {
            Credentials = new NetworkCredential(_user, _password),
            EnableSsl = true,
        };

        var message = new MailMessage
        {
            From = new MailAddress(_from, "SistemaServicios"),
            Subject = "Tu nueva contraseña — SistemaServicios",
            Body = BuildEmailBody(newPassword),
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }

    private static string BuildEmailBody(string newPassword) =>
        $"""
            <h2>Recuperación de contraseña</h2>
            <p>Tu nueva contraseña temporal es:</p>
            <h3 style="letter-spacing:2px">{newPassword}</h3>
            <p>Te recomendamos cambiarla después de iniciar sesión.</p>
            """;
}
