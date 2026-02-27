using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SistemaServicios.API.DTOs.Auth;
using Xunit;

namespace SistemaServicios.Tests.Integration;

/// <summary>
/// Pruebas de integración del AuthController.
/// Prueban el pipeline completo: HTTP → middleware → controller → service → EF InMemory.
/// IClassFixture reutiliza una sola instancia de la factory por clase de prueba.
/// </summary>
public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    /// <summary>Genera un email único para evitar colisiones entre pruebas.</summary>
    private static string EmailUnico() => $"user_{Guid.NewGuid():N}@test.com";

    /// <summary>Registra un usuario y devuelve su token JWT.</summary>
    private async Task<string> RegistrarYObtenerToken(
        string email, string password = "Password123!")
    {
        var body = new
        {
            email,
            password,
            firstName = "Test",
            lastName = "User",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return auth!.Token;
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/auth/register
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_DatosValidos_Retorna201ConToken()
    {
        var body = new
        {
            email = EmailUnico(),
            password = "Password123!",
            firstName = "Carlos",
            lastName = "García",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        auth!.Token.Should().NotBeNullOrEmpty();
        auth.Email.Should().Be(body.email);
        auth.FirstName.Should().Be("Carlos");
        auth.LastName.Should().Be("García");
        auth.Role.Should().Be("Client");
    }

    [Fact]
    public async Task Register_EmailDuplicado_Retorna400ConMensaje()
    {
        var email = EmailUnico();
        var body = new { email, password = "Password123!", firstName = "A", lastName = "B" };

        // Primer registro: exitoso
        await _client.PostAsJsonAsync("/api/auth/register", body);

        // Segundo registro con el mismo email: debe fallar
        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var contenido = await response.Content.ReadAsStringAsync();
        contenido.Should().Contain("El correo ya está registrado.");
    }

    [Fact]
    public async Task Register_EmailInvalido_Retorna400()
    {
        var body = new
        {
            email = "esto-no-es-un-email",
            password = "Password123!",
            firstName = "A",
            lastName = "B",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_PasswordMenorA6Caracteres_Retorna400()
    {
        var body = new
        {
            email = EmailUnico(),
            password = "123",   // menos de 6 caracteres
            firstName = "A",
            lastName = "B",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_SinFirstName_Retorna400()
    {
        var body = new
        {
            email = EmailUnico(),
            password = "Password123!",
            // firstName omitido intencionalmente
            lastName = "García",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_CualquierRolEnviado_SiempreAsignaRolClient()
    {
        // El campo 'role' ya no existe en el DTO: el servicio fuerza UserRole.Client
        // para prevenir que un usuario externo se auto-asigne como Admin o Professional.
        var body = new
        {
            email = EmailUnico(),
            password = "Password123!",
            firstName = "Pro",
            lastName = "User",
            role = 2,   // enviado pero ignorado por el servidor
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        auth!.Role.Should().Be("Client");
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/auth/login
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_CredencialesValidas_Retorna200ConToken()
    {
        var email = EmailUnico();
        await RegistrarYObtenerToken(email);

        var body = new { email, password = "Password123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        auth!.Token.Should().NotBeNullOrEmpty();
        auth.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_PasswordIncorrecto_Retorna401()
    {
        var email = EmailUnico();
        await RegistrarYObtenerToken(email);

        var body = new { email, password = "PasswordEquivocado!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var contenido = await response.Content.ReadAsStringAsync();
        contenido.Should().Contain("Credenciales inválidas.");
    }

    [Fact]
    public async Task Login_EmailNoRegistrado_Retorna401()
    {
        var body = new { email = "noexiste@test.com", password = "Password123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var contenido = await response.Content.ReadAsStringAsync();
        contenido.Should().Contain("Credenciales inválidas.");
    }

    [Fact]
    public async Task Login_CuerpoVacio_Retorna400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/auth/me
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_SinToken_Retorna401()
    {
        // Asegurar que no hay token en el cliente
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ConTokenValido_Retorna200ConDatosDelUsuario()
    {
        var email = EmailUnico();
        var token = await RegistrarYObtenerToken(email);

        // Adjuntar el Bearer token al header
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var contenido = await response.Content.ReadAsStringAsync();
        contenido.Should().Contain(email);
        contenido.Should().Contain("Test");   // firstName
        contenido.Should().Contain("User");   // lastName
        contenido.Should().Contain("Client"); // role
    }

    [Fact]
    public async Task Me_ConTokenMalformado_Retorna401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "esto.no.es.un.jwt");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────────────────
    // Flujo completo: registro → login → me
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task FlujoCompleto_RegistroLoginMe_TodosRetornanDatosConsistentes()
    {
        var email = EmailUnico();
        const string password = "Password123!";

        // 1. Registro
        var registerBody = new { email, password, firstName = "María", lastName = "Torres" };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerBody);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registerAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        // 2. Login con las mismas credenciales
        var loginBody = new { email, password };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginBody);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Ambos tokens deben tener el mismo email
        loginAuth!.Email.Should().Be(registerAuth!.Email);

        // 3. Me con el token del login
        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginAuth.Token);
        var meResponse = await _client.SendAsync(meRequest);

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var meContenido = await meResponse.Content.ReadAsStringAsync();
        meContenido.Should().Contain(email);
        meContenido.Should().Contain("María");
        meContenido.Should().Contain("Torres");
    }
}
