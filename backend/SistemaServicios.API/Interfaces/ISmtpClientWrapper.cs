using System.Net.Mail;

namespace SistemaServicios.API.Interfaces;

public interface ISmtpClientWrapper : IDisposable
{
    public Task SendMailAsync(MailMessage message);
}
