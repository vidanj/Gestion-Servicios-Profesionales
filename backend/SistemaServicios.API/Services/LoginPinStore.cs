using System.Collections.Concurrent;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

public class LoginPinStore : ILoginPinStore
{
    private readonly ConcurrentDictionary<string, LoginPinChallenge> _pins = new();

    public LoginPinChallenge? Get(string email) =>
        _pins.TryGetValue(Normalize(email), out var challenge) ? challenge : null;

    public void Save(string email, LoginPinChallenge challenge) =>
        _pins[Normalize(email)] = challenge;

    public void Remove(string email) => _pins.TryRemove(Normalize(email), out _);

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}