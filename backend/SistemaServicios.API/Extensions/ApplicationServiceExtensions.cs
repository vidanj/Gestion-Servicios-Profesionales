using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;

namespace SistemaServicios.API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            // 1. lo que hace que cargue el archivo .env 
            Env.Load();

            // 2. Obtener la cadena de conexión de la variable de entorno
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION") 
                                ?? config.GetConnectionString("DefaultConnection");

            // 3. Conecta a PostgreSQL
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            return services;
        }
    }
}