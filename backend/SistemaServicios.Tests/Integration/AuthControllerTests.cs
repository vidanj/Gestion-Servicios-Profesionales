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
        string email,
        string password = "Password123!"
    )
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
    public async Task RegisterDatosValidosRetorna201ConToken()
    {
        var body = new
        {
            email = EmailUnico(),
            password = "Password123!",
            firstName = "Carlos",
            lastName = "García",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        _ = response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        _ = auth!.Token.Should().NotBeNullOrEmpty();
        _ = auth.Email.Should().Be(body.email);
        _ = auth.FirstName.Should().Be("Carlos");
        _ = auth.LastName.Should().Be("García");
        _ = auth.Role.Should().Be("Client");
    }

    [Fact]
    public async Task RegisterEmailDuplicadoRetorna400ConMensaje()
    {
        var email = EmailUnico();
        var body = new
        {
            email,
            password = "Password123!",
            firstName = "A",
            lastName = "B",
        };

        // Primer registro: exitoso
        _ = await _client.PostAsJsonAsync("/api/auth/register", body);

        // Segundo registro con el mismo email: debe fallar
        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        _ = response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var contenido = await response.Content.ReadAsStringAsync();
        _ = contenido.Should().Contain("El correo ya está registrado.");
    }

    [Fact]
    public async Task RegisterEmailInvalidoRetorna400()
    {
        var body = new
        {
            email = "esto-no-es-un-email",
            password = "Password123!",
            firstName = "A",
            lastName = "B",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        _ = response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterPasswordMenorA6CaracteresRetorna400()
    {
        var body = new
        {
            email = EmailUnico(),
            password = "123", // menos de 6 caracteres
            firstName = "A",
            lastName = "B",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        _ = response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterSinFirstNameRetorna400()
    {
        var body = new
        {
            email = EmailUnico(),
            password = "Password123!",
            // firstName omitido intencionalmente
            lastName = "García",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        _ = response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterConRoleProfesionalAsignaRolProfesional()
    {
        // El campo 'role' se respeta: el usuario puede registrarse como Client o Professional.
        var body = new
        {
            email = EmailUnico(),
            password = "Password123!",
            firstName = "Pro",
            lastName = "User",
            role = 2,
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        _ = response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        _ = auth!.Role.Should().Be("Professional"); // ← sin typo, correcto
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/auth/login
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginCredencialesValidasRetorna200ConToken()
    {
        var email = EmailUnico();
        _ = await RegistrarYObtenerToken(email);

        var body = new { email, password = "Password123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", body);

        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        _ = auth!.Token.Should().NotBeNullOrEmpty();
        _ = auth.Email.Should().Be(email);
    }

    [Fact]
    public async Task LoginPasswordIncorrectoRetorna401()
    {
        var email = EmailUnico();
        _ = await RegistrarYObtenerToken(email);

        var body = new { email, password = "PasswordEquivocado!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", body);

        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var contenido = await response.Content.ReadAsStringAsync();
        _ = contenido.Should().Contain("Credenciales inválidas.");
    }

    [Fact]
    public async Task LoginEmailNoRegistradoRetorna401()
    {
        var body = new { email = "noexiste@test.com", password = "Password123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", body);

        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var contenido = await response.Content.ReadAsStringAsync();
        _ = contenido.Should().Contain("Credenciales inválidas.");
    }

    [Fact]
    public async Task LoginCuerpoVacioRetorna400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { });

        _ = response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/auth/me
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task MeSinTokenRetorna401()
    {
        // Asegurar que no hay token en el cliente
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/auth/me");

        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MeConTokenValidoRetorna200ConDatosDelUsuario()
    {
        var email = EmailUnico();
        var token = await RegistrarYObtenerToken(email);

        // Adjuntar el Bearer token al header
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
        var contenido = await response.Content.ReadAsStringAsync();
        _ = contenido.Should().Contain(email);
        _ = contenido.Should().Contain("Test"); // firstName
        _ = contenido.Should().Contain("User"); // lastName
        _ = contenido.Should().Contain("Client"); // role
    }

    [Fact]
    public async Task MeConTokenMalformadoRetorna401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "esto.no.es.un.jwt"
        );

        var response = await _client.SendAsync(request);

        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────────────────
    // Flujo completo: registro → login → me
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task FlujoCompletoRegistroLoginMeTodosRetornanDatosConsistentes()
    {
        var email = EmailUnico();
        const string password = "Password123!";

        // 1. Registro
        var registerBody = new
        {
            email,
            password,
            firstName = "María",
            lastName = "Torres",
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerBody);
        _ = registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registerAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        // 2. Login con las mismas credenciales
        var loginBody = new { email, password };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginBody);
        _ = loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Ambos tokens deben tener el mismo email
        _ = loginAuth!.Email.Should().Be(registerAuth!.Email);

        // 3. Me con el token del login
        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginAuth.Token);
        var meResponse = await _client.SendAsync(meRequest);

        _ = meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var meContenido = await meResponse.Content.ReadAsStringAsync();
        _ = meContenido.Should().Contain(email);
        _ = meContenido.Should().Contain("María");
        _ = meContenido.Should().Contain("Torres");
    }
}
