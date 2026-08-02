namespace SistemaServicios.API.Models;

public class LoginPinChallenge
{
    public required string PinHash { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public int FailedAttempts { get; set; }
}