using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Models;
using SistemaServicios.API.Repositories;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class UserRepositoryTests
{
    private static AppDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"UserRepo_{Guid.NewGuid()}")
                .Options
        );

    private static User CrearUsuario(string email = "test@test.com", bool status = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "hash-de-prueba",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Client,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    // ── GetByIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsyncUsuarioExistenteRetornaUsuario()
    {
        await using var context = CreateDbContext();
        var user = CrearUsuario();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var resultado = await new UserRepository(context).GetByIdAsync(user.Id);

        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsyncIdInexistenteRetornaNulo()
    {
        await using var context = CreateDbContext();

        var resultado = await new UserRepository(context).GetByIdAsync(Guid.NewGuid());

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsyncUsuarioInactivoRetornaNulo()
    {
        await using var context = CreateDbContext();
        var user = CrearUsuario(status: false);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var resultado = await new UserRepository(context).GetByIdAsync(user.Id);

        resultado.Should().BeNull();
    }

    // ── GetUsersAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsyncRetornaUsuariosActivos()
    {
        await using var context = CreateDbContext();
        context.Users.AddRange(
            CrearUsuario("a@test.com", status: true),
            CrearUsuario("b@test.com", status: true),
            CrearUsuario("c@test.com", status: false)
        );
        await context.SaveChangesAsync();

        var (users, total) = await new UserRepository(context).GetUsersAsync(1, 10);

        total.Should().Be(2);
        users.Should().HaveCount(2);
        users.Should().OnlyContain(u => u.Status == true);
    }

    [Fact]
    public async Task GetUsersAsyncPaginacionRetornaTamanoCorrecto()
    {
        await using var context = CreateDbContext();
        for (var i = 0; i < 5; i++)
        {
            context.Users.Add(CrearUsuario($"user{i}@test.com"));
        }
        await context.SaveChangesAsync();

        var (users, total) = await new UserRepository(context).GetUsersAsync(1, 2);

        total.Should().Be(5);
        users.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsersAsyncBaseDatosVaciaRetornaListaVacia()
    {
        await using var context = CreateDbContext();

        var (users, total) = await new UserRepository(context).GetUsersAsync(1, 10);

        total.Should().Be(0);
        users.Should().BeEmpty();
    }

    // ── GetUserByEmailAsync ───────────────────────────────────────

    [Fact]
    public async Task GetUserByEmailAsyncEmailExistenteRetornaUsuario()
    {
        await using var context = CreateDbContext();
        var user = CrearUsuario("correo@test.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var resultado = await new UserRepository(context).GetUserByEmailAsync("correo@test.com");

        resultado.Should().NotBeNull();
        resultado!.Email.Should().Be("correo@test.com");
    }

    [Fact]
    public async Task GetUserByEmailAsyncEmailInexistenteRetornaNulo()
    {
        await using var context = CreateDbContext();

        var resultado = await new UserRepository(context).GetUserByEmailAsync("noexiste@test.com");

        resultado.Should().BeNull();
    }

    // ── AddUserAsync ──────────────────────────────────────────────

    [Fact]
    public async Task AddUserAsyncGuardaUsuarioEnBaseDatos()
    {
        await using var context = CreateDbContext();
        var user = CrearUsuario("nuevo@test.com");

        var resultado = await new UserRepository(context).AddUserAsync(user);

        resultado.Should().NotBeNull();
        context.Users.Should().ContainSingle(u => u.Email == "nuevo@test.com");
    }

    // ── UpdateUserAsync ───────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsyncActualizaDatosCorrectamente()
    {
        await using var context = CreateDbContext();
        var user = CrearUsuario("update@test.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.FirstName = "Actualizado";
        await new UserRepository(context).UpdateUserAsync(user);

        var updated = await context.Users.FindAsync(user.Id);
        updated!.FirstName.Should().Be("Actualizado");
    }

    // ── GetByEmailAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetByEmailAsyncEmailExistenteRetornaUsuario()
    {
        await using var context = CreateDbContext();
        var user = CrearUsuario("auth@test.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var resultado = await new UserRepository(context).GetByEmailAsync("auth@test.com");

        resultado.Should().NotBeNull();
        resultado!.Email.Should().Be("auth@test.com");
    }

    [Fact]
    public async Task GetByEmailAsyncEmailInexistenteRetornaNulo()
    {
        await using var context = CreateDbContext();

        var resultado = await new UserRepository(context).GetByEmailAsync("noexiste@test.com");

        resultado.Should().BeNull();
    }

    // ── EmailExistsAsync ──────────────────────────────────────────

    [Fact]
    public async Task EmailExistsAsyncEmailRegistradoRetornaTrue()
    {
        await using var context = CreateDbContext();
        context.Users.Add(CrearUsuario("existe@test.com"));
        await context.SaveChangesAsync();

        var resultado = await new UserRepository(context).EmailExistsAsync("existe@test.com");

        resultado.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsyncEmailNoRegistradoRetornaFalse()
    {
        await using var context = CreateDbContext();

        var resultado = await new UserRepository(context).EmailExistsAsync("noexiste@test.com");

        resultado.Should().BeFalse();
    }

    // ── CreateAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateAsyncGuardaUsuarioYRetornaConId()
    {
        await using var context = CreateDbContext();
        var user = CrearUsuario("create@test.com");

        var resultado = await new UserRepository(context).CreateAsync(user);

        resultado.Should().NotBeNull();
        resultado.Id.Should().NotBeEmpty();
        context.Users.Should().ContainSingle(u => u.Email == "create@test.com");
    }
}
