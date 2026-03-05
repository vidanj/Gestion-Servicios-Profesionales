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
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=design_time_db;Username=postgres;Password=postgres");
        return new AppDbContext(optionsBuilder.Options);
    }
}
