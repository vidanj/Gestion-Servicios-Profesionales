using FluentAssertions;
using Moq;
using SistemaServicios.API.DTOs.Auth;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Comprueba que el servicio de autenticación cuenta cada camino con su resultado.
/// </summary>
/// <remarks>
/// Va en un archivo aparte en lugar de ampliar <see cref="AuthServiceTests"/>: aquellas
/// pruebas verifican el comportamiento del servicio y estas, su instrumentación. Mezclarlas
/// haría que un cambio en las métricas tocara el archivo de las pruebas de negocio.
/// </remarks>
public class AuthServiceMetricasTests
{
    private readonly Mock<IUserRepository> _repositorio = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IEmailService> _correo = new();
    private readonly Mock<IMetricasDeNegocio> _metricas = new();
    private readonly AuthService _servicio;

    public AuthServiceMetricasTests()
    {
        _tokens.Setup(t => t.CreateToken(It.IsAny<User>())).Returns("token-de-prueba");
        _servicio = new AuthService(
            _repositorio.Object,
            _tokens.Object,
            _correo.Object,
            _metricas.Object
        );
    }

    [Fact]
    public async Task LoginConCredencialesValidasCuentaExito()
    {
        _repositorio
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(Usuario(activo: true));

        _ = await _servicio.LoginAsync(
            new LoginRequestDto { Email = "a@b.c", Password = "Clave123!" }
        );

        _metricas.Verify(m => m.IntentoDeAutenticacion(ResultadoDeAutenticacion.Exito), Times.Once);
    }

    [Fact]
    public async Task LoginConUsuarioInexistenteCuentaCredencialesInvalidas()
    {
        _repositorio.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var accion = () =>
            _servicio.LoginAsync(new LoginRequestDto { Email = "a@b.c", Password = "Clave123!" });

        _ = await accion.Should().ThrowAsync<UnauthorizedAccessException>();
        _metricas.Verify(
            m => m.IntentoDeAutenticacion(ResultadoDeAutenticacion.CredencialesInvalidas),
            Times.Once
        );
    }

    /// <summary>
    /// El servicio devuelve el mismo mensaje que para credenciales incorrectas, para no
    /// revelar que la cuenta existe. La métrica sí los distingue, y eso es precisamente lo
    /// que permite saber si la gente no entra por error propio o porque su cuenta está
    /// desactivada, sin filtrar nada a quien pregunta desde fuera.
    /// </summary>
    [Fact]
    public async Task LoginConCuentaDesactivadaCuentaCuentaInactiva()
    {
        _repositorio
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(Usuario(activo: false));

        var accion = () =>
            _servicio.LoginAsync(new LoginRequestDto { Email = "a@b.c", Password = "Clave123!" });

        _ = await accion.Should().ThrowAsync<UnauthorizedAccessException>();
        _metricas.Verify(
            m => m.IntentoDeAutenticacion(ResultadoDeAutenticacion.CuentaInactiva),
            Times.Once
        );
        _metricas.Verify(
            m => m.IntentoDeAutenticacion(ResultadoDeAutenticacion.CredencialesInvalidas),
            Times.Never
        );
    }

    [Fact]
    public async Task RegistroCorrectoCuentaAltaCreada()
    {
        _repositorio.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        _ = await _servicio.RegisterAsync(
            new RegisterRequestDto
            {
                Email = "nuevo@b.c",
                Password = "Clave123!",
                FirstName = "Ana",
                LastName = "Lopez",
            }
        );

        _metricas.Verify(m => m.UsuarioRegistrado(true), Times.Once);
    }

    [Fact]
    public async Task RegistroConCorreoDuplicadoCuentaElRechazo()
    {
        _repositorio.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        var accion = () =>
            _servicio.RegisterAsync(
                new RegisterRequestDto
                {
                    Email = "repetido@b.c",
                    Password = "Clave123!",
                    FirstName = "Ana",
                    LastName = "Lopez",
                }
            );

        _ = await accion.Should().ThrowAsync<InvalidOperationException>();
        _metricas.Verify(m => m.UsuarioRegistrado(false), Times.Once);
    }

    private static User Usuario(bool activo) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = "a@b.c",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Clave123!"),
            FirstName = "Ana",
            LastName = "Lopez",
            Role = UserRole.Client,
            Status = activo,
        };
}
