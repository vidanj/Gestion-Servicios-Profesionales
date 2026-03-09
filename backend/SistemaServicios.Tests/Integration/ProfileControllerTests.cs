using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SistemaServicios.API.Data;
using SistemaServicios.API.DTOs;
using SistemaServicios.API.DTOs.Auth;
using Xunit;

namespace SistemaServicios.Tests.Integration;

public class ProfileControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProfileControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string EmailUnico() => $"perfil_{Guid.NewGuid():N}@test.com";

    private async Task<(string token, string email)> RegistrarUsuario(
        string? email = null,
        string password = "Password123!"
    )
    {
        email ??= EmailUnico();
        var body = new { email, password, firstName = "Juan", lastName = "Pérez" };
        var res = await _client.PostAsJsonAsync("/api/auth/register", body);
        var auth = await res.Content.ReadFromJsonAsync<AuthResponseDto>();
        return (auth!.Token, email);
    }

    private static HttpRequestMessage ConRequest(HttpMethod method, string url, string token) =>
        new(method, url) { Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) } };

    /// <summary>
    /// Marca al usuario como inactivo (Status = false) directamente en la base de datos en memoria.
    /// Esto provoca que GetByIdAsync no lo encuentre y devuelva null, disparando los 404.
    /// </summary>
    private async Task DesactivarUsuarioAsync(string token)
    {
        var req = ConRequest(HttpMethod.Get, "/api/Profile", token);
        var res = await _client.SendAsync(req);
        var user = await res.Content.ReadFromJsonAsync<UserDto>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbUser = await db.Users.FindAsync(user!.Id);
        dbUser!.Status = false;
        await db.SaveChangesAsync();
    }

    // ─── GET /api/Profile ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfileConTokenValidoRetorna200ConDatosDelUsuario()
    {
        var (token, email) = await RegistrarUsuario();

        var req = ConRequest(HttpMethod.Get, "/api/Profile", token);
        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await res.Content.ReadFromJsonAsync<UserDto>();
        user!.Email.Should().Be(email);
        user.FirstName.Should().Be("Juan");
    }

    [Fact]
    public async Task GetProfileSinTokenRetorna401()
    {
        var res = await _client.GetAsync("/api/Profile");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── PUT /api/Profile ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfileDatosValidosRetorna200ConDatosActualizados()
    {
        var (token, _) = await RegistrarUsuario();

        var req = ConRequest(HttpMethod.Put, "/api/Profile", token);
        req.Content = JsonContent.Create(new
        {
            firstName = "Carlos",
            lastName = "García",
            phoneNumber = "6649876543",
        });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await res.Content.ReadFromJsonAsync<UserDto>();
        user!.FirstName.Should().Be("Carlos");
        user.LastName.Should().Be("García");
        user.PhoneNumber.Should().Be("6649876543");
    }

    [Fact]
    public async Task UpdateProfileSinTokenRetorna401()
    {
        var res = await _client.PutAsJsonAsync(
            "/api/Profile",
            new { firstName = "Carlos", lastName = "García" }
        );
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfileSinNombreRetorna400()
    {
        var (token, _) = await RegistrarUsuario();

        var req = ConRequest(HttpMethod.Put, "/api/Profile", token);
        req.Content = JsonContent.Create(new { firstName = "", lastName = "" });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── PUT /api/Profile/password ───────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordContrasenaCorrectaRetorna200()
    {
        var password = "Password123!";
        var (token, _) = await RegistrarUsuario(password: password);

        var req = ConRequest(HttpMethod.Put, "/api/Profile/password", token);
        req.Content = JsonContent.Create(new
        {
            currentPassword = password,
            newPassword = "NuevaPassword456!",
            confirmNewPassword = "NuevaPassword456!",
        });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("actualizada");
    }

    [Fact]
    public async Task ChangePasswordContrasenaIncorrectaRetorna400()
    {
        var (token, _) = await RegistrarUsuario();

        var req = ConRequest(HttpMethod.Put, "/api/Profile/password", token);
        req.Content = JsonContent.Create(new
        {
            currentPassword = "ContrasenaEquivocada",
            newPassword = "NuevaPassword456!",
            confirmNewPassword = "NuevaPassword456!",
        });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("incorrecta");
    }

    [Fact]
    public async Task ChangePasswordSinTokenRetorna401()
    {
        var res = await _client.PutAsJsonAsync(
            "/api/Profile/password",
            new { currentPassword = "a", newPassword = "b", confirmNewPassword = "b" }
        );
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePasswordPayloadInvalidoRetorna400()
    {
        var (token, _) = await RegistrarUsuario();

        var req = ConRequest(HttpMethod.Put, "/api/Profile/password", token);
        // newPassword muy corto (< 8 caracteres) — falla validación de modelo
        req.Content = JsonContent.Create(new
        {
            currentPassword = "Password123!",
            newPassword = "corta",
            confirmNewPassword = "corta",
        });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── POST /api/Profile/foto ──────────────────────────────────────────────

    [Fact]
    public async Task UploadFotoImagenJpegValidaRetorna200ConUrlActualizada()
    {
        var (token, _) = await RegistrarUsuario();

        using var content = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }; // cabecera JPEG mínima
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "foto", "avatar.jpg");

        var req = ConRequest(HttpMethod.Post, "/api/Profile/foto", token);
        req.Content = content;

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await res.Content.ReadFromJsonAsync<UserDto>();
        user!.ProfileImageUrl.Should().Contain("/uploads/avatars/");
    }

    [Fact]
    public async Task UploadFotoImagenPngValidaRetorna200()
    {
        var (token, _) = await RegistrarUsuario();

        using var content = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A }; // cabecera PNG mínima
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "foto", "avatar.png");

        var req = ConRequest(HttpMethod.Post, "/api/Profile/foto", token);
        req.Content = content;

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await res.Content.ReadFromJsonAsync<UserDto>();
        user!.ProfileImageUrl.Should().EndWith(".png");
    }

    [Fact]
    public async Task UploadFotoMimeNoPermitidoRetorna400()
    {
        var (token, _) = await RegistrarUsuario();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x47, 0x49, 0x46 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/gif");
        content.Add(fileContent, "foto", "avatar.gif");

        var req = ConRequest(HttpMethod.Post, "/api/Profile/foto", token);
        req.Content = content;

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("JPG o PNG");
    }

    [Fact]
    public async Task UploadFotoImagenDemasiadoGrandeRetorna400()
    {
        var (token, _) = await RegistrarUsuario();

        using var content = new MultipartFormDataContent();
        var imagenGrande = new byte[3 * 1024 * 1024]; // 3 MB > límite 2 MB
        var fileContent = new ByteArrayContent(imagenGrande);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "foto", "grande.jpg");

        var req = ConRequest(HttpMethod.Post, "/api/Profile/foto", token);
        req.Content = content;

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("2 MB");
    }

    [Fact]
    public async Task UploadFotoSinTokenRetorna401()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "foto", "avatar.jpg");

        var res = await _client.PostAsync("/api/Profile/foto", content);

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── NotFound paths (usuario en JWT pero no en BD) ───────────────────────

    [Fact]
    public async Task GetProfileUsuarioInactivoRetorna404()
    {
        var (token, _) = await RegistrarUsuario();
        await DesactivarUsuarioAsync(token);

        var req = ConRequest(HttpMethod.Get, "/api/Profile", token);
        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProfileUsuarioInactivoRetorna404()
    {
        var (token, _) = await RegistrarUsuario();
        await DesactivarUsuarioAsync(token);

        var req = ConRequest(HttpMethod.Put, "/api/Profile", token);
        req.Content = JsonContent.Create(new { firstName = "Carlos", lastName = "García" });
        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangePasswordUsuarioInactivoRetorna404()
    {
        var password = "Password123!";
        var (token, _) = await RegistrarUsuario(password: password);
        await DesactivarUsuarioAsync(token);

        var req = ConRequest(HttpMethod.Put, "/api/Profile/password", token);
        req.Content = JsonContent.Create(new
        {
            currentPassword = password,
            newPassword = "NuevaPassword456!",
            confirmNewPassword = "NuevaPassword456!",
        });
        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadFotoUsuarioInactivoRetorna404()
    {
        var (token, _) = await RegistrarUsuario();
        await DesactivarUsuarioAsync(token);

        using var content = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "foto", "avatar.jpg");

        var req = ConRequest(HttpMethod.Post, "/api/Profile/foto", token);
        req.Content = content;
        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
