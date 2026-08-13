using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SistemaServicios.API.Interfaces;
using Xunit;

namespace SistemaServicios.Tests.Integration;

/// <summary>
/// Factory que sustituye el almacenamiento por un mock, para no depender de que
/// haya archivos reales en la base de pruebas.
/// </summary>
public class FilesWebApplicationFactory : CustomWebApplicationFactory
{
    public Mock<IFileStorage> FileStorageMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        _ = builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorage));
            if (descriptor != null)
            {
                _ = services.Remove(descriptor);
            }

            _ = services.AddScoped<IFileStorage>(_ => FileStorageMock.Object);
        });
    }
}

public class FilesControllerTests : IClassFixture<FilesWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Mock<IFileStorage> _storageMock;

    public FilesControllerTests(FilesWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _storageMock = factory.FileStorageMock;
    }

    [Fact]
    public async Task GetFileSinTokenDevuelveElArchivo()
    {
        // Arrange: el endpoint es anónimo a propósito, porque los avatares se
        // muestran en el catálogo público, que se consulta sin iniciar sesión.
        var id = Guid.NewGuid();
        _ = _storageMock
            .Setup(s => s.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFileContent([1, 2, 3], "image/png"));

        // Act
        var response = await _client.GetAsync(new Uri($"/api/Files/{id}", UriKind.Relative));

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
        _ = response.Content.Headers.ContentType?.MediaType.Should().Be("image/png");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        _ = bytes.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task GetFileDevuelveCacheInmutable()
    {
        // Arrange
        var id = Guid.NewGuid();
        _ = _storageMock
            .Setup(s => s.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFileContent([9], "image/jpeg"));

        // Act
        var response = await _client.GetAsync(new Uri($"/api/Files/{id}", UriKind.Relative));

        // Assert: se puede cachear de forma agresiva porque el id cambia en cada subida
        var cacheControl = response.Headers.CacheControl?.ToString() ?? string.Empty;
        _ = cacheControl.Should().Contain("max-age=31536000");
        _ = cacheControl.Should().Contain("immutable");
    }

    [Fact]
    public async Task GetFileConIdInexistenteDevuelve404()
    {
        // Arrange
        var id = Guid.NewGuid();
        _ = _storageMock
            .Setup(s => s.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredFileContent?)null);

        // Act
        var response = await _client.GetAsync(new Uri($"/api/Files/{id}", UriKind.Relative));

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFileConIdMalFormadoDevuelve404()
    {
        // Arrange & Act: la restricción :guid de la ruta descarta el valor
        var response = await _client.GetAsync(
            new Uri("/api/Files/no-es-un-guid", UriKind.Relative)
        );

        // Assert
        _ = response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
