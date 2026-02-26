using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaServicios.API.Data;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Repositories;
using SistemaServicios.API.Services;

namespace SistemaServicios.API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            // Busca el .env subiendo desde el directorio actual hasta encontrarlo
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, ".env")))
                directory = directory.Parent;

            if (directory != null)
                Env.Load(Path.Combine(directory.FullName, ".env"));

            ((IConfigurationBuilder)config).AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"]              = Environment.GetEnvironmentVariable("JWT_KEY"),
                ["JwtSettings:Issuer"]           = Environment.GetEnvironmentVariable("JWT_ISSUER"),
                ["JwtSettings:Audience"]         = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                ["JwtSettings:ExpiresInMinutes"] = Environment.GetEnvironmentVariable("JWT_EXPIRES_MINUTES"),
            });

            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();

            // Services
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<RequestService>();

            // JWT Authentication
            var jwtKey = config["JwtSettings:Key"]
                ?? throw new InvalidOperationException("JWT_KEY no definido en el archivo .env");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateIssuer = false,
                        ValidIssuer = config["JwtSettings:Issuer"],
                        ValidateAudience = false,
                        ValidAudience = config["JwtSettings:Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                    };
                });

            services.AddAuthorization();

            // Swagger con soporte para JWT Bearer
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sistema Servicios API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Ingresa el token así: Bearer {tu_token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }
    }
}
