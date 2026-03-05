using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaServicios.API.Data;

namespace SistemaServicios.Tests.Integration;

/// <summary>
/// Levanta el servidor completo de ASP.NET Core en memoria para pruebas de integración.
/// Reemplaza PostgreSQL con una base de datos en memoria (EF InMemory).
/// Cada instancia de la factory usa una base de datos aislada.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Nombre único por instancia para aislar las pruebas entre sí
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    public CustomWebApplicationFactory()
    {
        // Las variables de entorno se establecen en el constructor para garantizar
        // que estén disponibles ANTES de que WebApplicationFactory comience a construir
        // el host. Program.cs llama a AddApplicationServices() durante la fase de
        // configuración del builder, que puede ocurrir antes de que ConfigureWebHost
        // sea invocado como callback post-configuración.
        // En CI no existe archivo .env, por lo que estas variables son la única fuente.
        Environment.SetEnvironmentVariable("JWT_KEY", "ClaveSecretaParaIntegracionTests_32Ch!");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "TestIssuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "TestAudience");
        Environment.SetEnvironmentVariable("JWT_EXPIRES_MINUTES", "60");
        // Variables individuales de BD: evitan la excepción en AddApplicationServices.
        // El DbContext será reemplazado en ConfigureWebHost por InMemory.
        Environment.SetEnvironmentVariable("DB_HOST", "localhost");
        Environment.SetEnvironmentVariable("DB_PORT", "5432");
        Environment.SetEnvironmentVariable("DB_NAME", "fake");
        Environment.SetEnvironmentVariable("DB_USER", "fake");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "fake");
        // CORS: origen de prueba que CorsTests utilizará para verificar la política FrontendPolicy.
        Environment.SetEnvironmentVariable("ALLOWED_ORIGINS", "http://localhost:3000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _ = builder.ConfigureServices(services =>
        {
            // EF Core 9 registra la configuración del provider en IDbContextOptionsConfiguration<T>.
            // Si no se remueve antes de agregar InMemory, ambos providers quedan activos y EF
            // lanza InvalidOperationException al detectar dos providers en el mismo service provider.

            // 1. Remover DbContextOptions<AppDbContext>
            var optionsDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>)
            );
            if (optionsDescriptor != null)
            {
                _ = services.Remove(optionsDescriptor);
            }

            // 2. Remover IDbContextOptionsConfiguration<AppDbContext> (configuración de Npgsql)
            var configType = typeof(IDbContextOptionsConfiguration<AppDbContext>);
            var configDescriptors = services.Where(d => d.ServiceType == configType).ToList();
            foreach (var d in configDescriptors)
            {
                _ = services.Remove(d);
            }

            // 3. Registrar DbContext fresco con base de datos en memoria
            _ = services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });

        // Suprimir warnings de HTTPS del test server (no tiene puerto HTTPS configurado)
        _ = builder.ConfigureLogging(logging =>
            logging.AddFilter("Microsoft.AspNetCore.HttpsPolicy", LogLevel.None)
        );

        // Ambiente explícito para pruebas
        _ = builder.UseEnvironment("Testing");
    }
}
