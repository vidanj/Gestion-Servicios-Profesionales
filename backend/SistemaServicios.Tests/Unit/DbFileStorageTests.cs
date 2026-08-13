using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Pruebas del almacenamiento en base de datos, que es lo que permite que los
/// avatares sobrevivan a un despliegue y sean visibles desde cualquier réplica.
/// </summary>
public class DbFileStorageTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly DbFileStorage _storage;
    private readonly Guid _ownerId = Guid.NewGuid();

    public DbFileStorageTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"FilesTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _storage = new DbFileStorage(_context);

        _ = _context.Users.Add(
            new User
            {
                Id = _ownerId,
                Email = "duenio@test.com",
                PasswordHash = "hash",
                FirstName = "Due",
                LastName = "Nio",
                Role = UserRole.Client,
                Status = true,
            }
        );
        _ = _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private static MemoryStream Contenido(params byte[] bytes) => new(bytes);

    [Fact]
    public async Task SaveForOwnerAsyncDevuelveUnaRutaRelativaDeLaApi()
    {
        // Act
        var url = await _storage.SaveForOwnerAsync(_ownerId, Contenido(1, 2, 3), "image/png");

        // Assert: relativa, porque el frontend la concatena a la URL base de la API
        _ = url.Should().StartWith("/api/Files/");
        _ = Guid.TryParse(url.Replace("/api/Files/", string.Empty, StringComparison.Ordinal), out _)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task SaveForOwnerAsyncGuardaContenidoTipoYTamano()
    {
        // Act
        await _storage.SaveForOwnerAsync(_ownerId, Contenido(10, 20, 30, 40), "image/jpeg");

        // Assert
        var guardado = await _context.StoredFiles.SingleAsync();
        _ = guardado.Content.Should().Equal(10, 20, 30, 40);
        _ = guardado.ContentType.Should().Be("image/jpeg");
        _ = guardado.SizeBytes.Should().Be(4);
        _ = guardado.OwnerUserId.Should().Be(_ownerId);
    }

    [Fact]
    public async Task SaveForOwnerAsyncSegundaSubidaReemplazaLaAnterior()
    {
        // Arrange
        var primeraUrl = await _storage.SaveForOwnerAsync(_ownerId, Contenido(1), "image/jpeg");

        // Act: el cambio de jpg a png es justo el caso que antes dejaba un huérfano
        var segundaUrl = await _storage.SaveForOwnerAsync(_ownerId, Contenido(2), "image/png");

        // Assert: queda exactamente un archivo, el nuevo
        _ = _context.StoredFiles.Should().ContainSingle();
        var guardado = await _context.StoredFiles.SingleAsync();
        _ = guardado.ContentType.Should().Be("image/png");
        _ = segundaUrl.Should().NotBe(primeraUrl);
    }

    [Fact]
    public async Task SaveForOwnerAsyncNoBorraLosArchivosDeOtrosUsuarios()
    {
        // Arrange
        var otroId = Guid.NewGuid();
        _ = _context.Users.Add(
            new User
            {
                Id = otroId,
                Email = "otro@test.com",
                PasswordHash = "hash",
                FirstName = "Otro",
                LastName = "Usuario",
                Role = UserRole.Client,
                Status = true,
            }
        );
        _ = await _context.SaveChangesAsync();

        await _storage.SaveForOwnerAsync(otroId, Contenido(9), "image/png");

        // Act
        await _storage.SaveForOwnerAsync(_ownerId, Contenido(1), "image/png");

        // Assert
        _ = _context.StoredFiles.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAsyncDevuelveElContenidoYSuTipo()
    {
        // Arrange
        var url = await _storage.SaveForOwnerAsync(_ownerId, Contenido(7, 8), "image/jpeg");
        var id = Guid.Parse(url.Replace("/api/Files/", string.Empty, StringComparison.Ordinal));

        // Act
        var archivo = await _storage.GetAsync(id);

        // Assert
        _ = archivo.Should().NotBeNull();
        _ = archivo!.Content.Should().Equal(7, 8);
        _ = archivo.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task GetAsyncConIdInexistenteDevuelveNull()
    {
        // Act
        var archivo = await _storage.GetAsync(Guid.NewGuid());

        // Assert
        _ = archivo.Should().BeNull();
    }
}
