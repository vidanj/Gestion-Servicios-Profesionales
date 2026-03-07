namespace SistemaServicios.API.Interfaces;

public interface IEmailService
{
    public Task SendPasswordResetEmailAsync(string toEmail, string newPassword);
}
