using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _config;

    // Clave de al menos 32 caracteres requerida por HMAC-SHA256
    private const string TestKey = "ClaveSecretaParaPruebasDeTests_32Chars!";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";
    private const int ExpiresMinutes = 60;

    public TokenServiceTests()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["JwtSettings:Key"]              = TestKey,
            ["JwtSettings:Issuer"]           = TestIssuer,
            ["JwtSettings:Audience"]         = TestAudience,
            ["JwtSettings:ExpiresInMinutes"] = ExpiresMinutes.ToString(),
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _tokenService = new TokenService(_config);
    }

    private static User CrearUsuarioDePrueba() => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Email = "usuario@test.com",
        PasswordHash = "hash-no-relevante",
        FirstName = "Juan",
        LastName = "Pérez",
        Role = UserRole.Client,
        Status = true,
    };

    // ─────────────────────────────────────────────────────────────
    // Formato del token
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CreateToken_UsuarioValido_RetornaStringNoVacio()
    {
        var token = _tokenService.CreateToken(CrearUsuarioDePrueba());

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreateToken_UsuarioValido_RetornaTokenJwtValido()
    {
        var token = _tokenService.CreateToken(CrearUsuarioDePrueba());

        // Un JWT siempre tiene el formato: header.payload.signature
        var partes = token.Split('.');
        partes.Should().HaveCount(3, "un JWT siempre tiene tres partes separadas por punto");
    }

    // ─────────────────────────────────────────────────────────────
    // Claims del payload
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CreateToken_UsuarioValido_ContieneClaimEmail()
    {
        var usuario = CrearUsuarioDePrueba();
        var token = _tokenService.CreateToken(usuario);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be("usuario@test.com");
    }

    [Fact]
    public void CreateToken_UsuarioValido_ContieneClaimNameIdentifier()
    {
        var usuario = CrearUsuarioDePrueba();
        var token = _tokenService.CreateToken(usuario);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var idClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        idClaim.Should().NotBeNull();
        idClaim!.Value.Should().Be("11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void CreateToken_UsuarioValido_ContieneClaimRole()
    {
        var usuario = CrearUsuarioDePrueba();
        var token = _tokenService.CreateToken(usuario);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be("Client");
    }

    [Fact]
    public void CreateToken_UsuarioValido_ContieneClaimFirstName()
    {
        var usuario = CrearUsuarioDePrueba();
        var token = _tokenService.CreateToken(usuario);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var firstNameClaim = jwt.Claims.FirstOrDefault(c => c.Type == "firstName");

        firstNameClaim.Should().NotBeNull();
        firstNameClaim!.Value.Should().Be("Juan");
    }

    [Fact]
    public void CreateToken_UsuarioValido_ContieneClaimLastName()
    {
        var usuario = CrearUsuarioDePrueba();
        var token = _tokenService.CreateToken(usuario);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var lastNameClaim = jwt.Claims.FirstOrDefault(c => c.Type == "lastName");

        lastNameClaim.Should().NotBeNull();
        lastNameClaim!.Value.Should().Be("Pérez");
    }

    // ─────────────────────────────────────────────────────────────
    // Issuer y Audience
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CreateToken_UsuarioValido_IssuerCorrecto()
    {
        var token = _tokenService.CreateToken(CrearUsuarioDePrueba());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be(TestIssuer);
    }

    [Fact]
    public void CreateToken_UsuarioValido_AudienceCorrecto()
    {
        var token = _tokenService.CreateToken(CrearUsuarioDePrueba());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Audiences.Should().Contain(TestAudience);
    }

    // ─────────────────────────────────────────────────────────────
    // Expiración
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CreateToken_UsuarioValido_TokenNoEstaExpirado()
    {
        var token = _tokenService.CreateToken(CrearUsuarioDePrueba());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void CreateToken_UsuarioValido_ExpiracionCercanaDeLaConfiguracion()
    {
        var antes = DateTime.UtcNow;
        var token = _tokenService.CreateToken(CrearUsuarioDePrueba());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // El token debe expirar aproximadamente en ExpiresMinutes minutos
        jwt.ValidTo.Should().BeCloseTo(
            antes.AddMinutes(ExpiresMinutes),
            precision: TimeSpan.FromSeconds(10));
    }

    // ─────────────────────────────────────────────────────────────
    // Roles distintos
    // ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(UserRole.Admin, "Admin")]
    [InlineData(UserRole.Client, "Client")]
    [InlineData(UserRole.Professional, "Professional")]
    public void CreateToken_CadaRol_ContieneRolCorrecto(UserRole rol, string rolEsperado)
    {
        var usuario = CrearUsuarioDePrueba();
        usuario.Role = rol;

        var token = _tokenService.CreateToken(usuario);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roleClaim = jwt.Claims.First(c => c.Type == ClaimTypes.Role);
        roleClaim.Value.Should().Be(rolEsperado);
    }

    // ─────────────────────────────────────────────────────────────
    // Configuración faltante
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CreateToken_SinJwtKey_LanzaInvalidOperationException()
    {
        var configSinKey = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"]           = TestIssuer,
                ["JwtSettings:Audience"]         = TestAudience,
                ["JwtSettings:ExpiresInMinutes"] = "60",
                // JWT_KEY intencionalmente omitido
            })
            .Build();

        var tokenServiceSinKey = new TokenService(configSinKey);

        var act = () => tokenServiceSinKey.CreateToken(CrearUsuarioDePrueba());
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("JWT Key no configurado.");
    }

    [Fact]
    public void CreateToken_SinExpiresInMinutes_UsaDefault60Minutos()
    {
        // Arrange: JwtSettings:ExpiresInMinutes omitido → debe usar el valor por defecto "60"
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"]      = TestKey,
                ["JwtSettings:Issuer"]   = TestIssuer,
                ["JwtSettings:Audience"] = TestAudience,
                // ExpiresInMinutes intencionalmente omitido
            })
            .Build();

        var service = new TokenService(config);
        var antes   = DateTime.UtcNow;

        // Act
        var token = service.CreateToken(CrearUsuarioDePrueba());

        // Assert: el token se generó y expira en ~60 minutos (valor por defecto)
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.ValidTo.Should().BeCloseTo(
            antes.AddMinutes(60),
            precision: TimeSpan.FromSeconds(10));
    }
}
