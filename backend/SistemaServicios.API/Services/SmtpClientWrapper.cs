using System.Net;
using System.Net.Mail;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

public class SmtpClientWrapper : ISmtpClientWrapper
{
    private readonly SmtpClient _client;

    public SmtpClientWrapper(string host, int port, string user, string password)
    {
        _client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, password),
            EnableSsl = true,
        };
    }

    public Task SendMailAsync(MailMessage message) => _client.SendMailAsync(message);

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
