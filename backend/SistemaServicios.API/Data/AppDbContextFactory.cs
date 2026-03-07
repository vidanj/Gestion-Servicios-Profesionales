using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SistemaServicios.API.Data;

/// <summary>
/// Used only by EF Core design-time tools (dotnet ef migrations / bundle).
/// It bypasses the DI container, so no environment variables are required
/// during docker build. The connection string here is never used at runtime.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "pizza007";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            $"Host={host};Port={port};Database=design_time_db;Username={user};Password={password}"
        );
        return new AppDbContext(optionsBuilder.Options);
    }
}
