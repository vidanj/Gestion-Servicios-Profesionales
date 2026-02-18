using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;

namespace SistemaServicios.API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            // Busca el .env subiendo desde el directorio actual hasta encontrarlo
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, ".env")))
            {
                directory = directory.Parent;
            }

            if (directory != null)
                Env.Load(Path.Combine(directory.FullName, ".env"));

            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            return services;
        }
    }
}