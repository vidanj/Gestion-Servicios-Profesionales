using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Models;
using SistemaServicios.API.Repositories;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Pruebas unitarias del UserRepository.
/// Usa EF InMemory para validar las consultas sin depender de PostgreSQL.
/// Cada prueba obtiene su propia base de datos aislada mediante un nombre único.
/// </summary>
public class UserRepositoryTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"UserRepo_{Guid.NewGuid()}")
            .Options);

    private static User CrearUsuario(string email = "test@test.com") => new()
    {
        Id           = Guid.NewGuid(),
        Email        = email,
        PasswordHash = "hash-de-prueba",
        FirstName    = "Test",
        LastName     = "User",
        Role         = UserRole.Client,
        Status       = true,
        CreatedAt    = DateTime.UtcNow,
        UpdatedAt    = DateTime.UtcNow,
    };

    // ─────────────────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_UsuarioExistente_RetornaUsuario()
    {
        // Arrange
        await using var context = CreateDbContext();
        var user = CrearUsuario();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);

        // Act
        var resultado = await repo.GetByIdAsync(user.Id);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(user.Id);
        resultado.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetByIdAsync_IdInexistente_RetornaNulo()
    {
        // Arrange: base de datos vacía
        await using var context = CreateDbContext();
        var repo = new UserRepository(context);

        // Act
        var resultado = await repo.GetByIdAsync(Guid.NewGuid());

        // Assert
        resultado.Should().BeNull();
    }
}
