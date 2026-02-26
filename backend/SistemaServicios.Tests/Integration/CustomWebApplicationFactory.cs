using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Establecer variables de entorno antes de que AddApplicationServices las lea.
        // ApplicationServiceExtensions llama a Environment.GetEnvironmentVariable(...)
        // para poblar la configuración de JWT y la cadena de conexión.
        Environment.SetEnvironmentVariable("JWT_KEY", "ClaveSecretaParaIntegracionTests_32Ch!");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "TestIssuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "TestAudience");
        Environment.SetEnvironmentVariable("JWT_EXPIRES_MINUTES", "60");
        // DB_CONNECTION se establece para que AddApplicationServices no lance excepción,
        // pero el DbContext será reemplazado abajo por InMemory.
        Environment.SetEnvironmentVariable("DB_CONNECTION", "Host=localhost;Database=fake");

        builder.ConfigureServices(services =>
        {
            // EF Core 9 registra la configuración del provider en IDbContextOptionsConfiguration<T>.
            // Si no se remueve antes de agregar InMemory, ambos providers quedan activos y EF
            // lanza InvalidOperationException al detectar dos providers en el mismo service provider.

            // 1. Remover DbContextOptions<AppDbContext>
            var optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (optionsDescriptor != null)
                services.Remove(optionsDescriptor);

            // 2. Remover IDbContextOptionsConfiguration<AppDbContext> (configuración de Npgsql)
            var configType = typeof(IDbContextOptionsConfiguration<AppDbContext>);
            var configDescriptors = services.Where(d => d.ServiceType == configType).ToList();
            foreach (var d in configDescriptors)
                services.Remove(d);

            // 3. Registrar DbContext fresco con base de datos en memoria
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });

        // Ambiente explícito para pruebas
        builder.UseEnvironment("Testing");
    }
}
