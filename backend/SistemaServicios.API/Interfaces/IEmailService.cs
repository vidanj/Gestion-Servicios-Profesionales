namespace SistemaServicios.API.Interfaces;

public interface IEmailService
{
    public Task SendPasswordResetEmailAsync(string toEmail, string newPassword);

    public Task SendLoginPinEmailAsync(string toEmail, string pin);
}
