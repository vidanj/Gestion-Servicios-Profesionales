namespace SistemaServicios.API.Interfaces;

public interface ILoginPinStore
{
    public LoginPinChallenge? Get(string email);

    public void Save(string email, LoginPinChallenge challenge);

    public void Remove(string email);
}