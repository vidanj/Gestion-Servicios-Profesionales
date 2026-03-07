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

namespace SistemaServicios.API.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, ".env")))
        {
            directory = directory.Parent;
        }

        if (directory != null)
        {
            Env.Load(
                Path.Combine(directory.FullName, ".env"),
                new LoadOptions(clobberExistingVars: false)
            );
        }

        ((IConfigurationBuilder)config).AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = Environment.GetEnvironmentVariable("JWT_KEY"),
                ["JwtSettings:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER"),
                ["JwtSettings:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                ["JwtSettings:ExpiresInMinutes"] = Environment.GetEnvironmentVariable(
                    "JWT_EXPIRES_MINUTES"
                ),
                ["CorsSettings:AllowedOrigins"] = Environment.GetEnvironmentVariable(
                    "ALLOWED_ORIGINS"
                ),
                ["SmtpSettings:Host"] = Environment.GetEnvironmentVariable("SMTP_HOST"),
                ["SmtpSettings:Port"] = Environment.GetEnvironmentVariable("SMTP_PORT"),
                ["SmtpSettings:User"] = Environment.GetEnvironmentVariable("SMTP_USER"),
                ["SmtpSettings:Password"] = Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
                ["SmtpSettings:From"] = Environment.GetEnvironmentVariable("SMTP_FROM"),

            }
        );

        var dbHost =
            Environment.GetEnvironmentVariable("DB_HOST")
            ?? throw new InvalidOperationException("DB_HOST no definido en el archivo .env");
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var dbName =
            Environment.GetEnvironmentVariable("DB_NAME")
            ?? throw new InvalidOperationException("DB_NAME no definido en el archivo .env");
        var dbUser =
            Environment.GetEnvironmentVariable("DB_USER")
            ?? throw new InvalidOperationException("DB_USER no definido en el archivo .env");
        var dbPassword =
            Environment.GetEnvironmentVariable("DB_PASSWORD")
            ?? throw new InvalidOperationException("DB_PASSWORD no definido en el archivo .env");

        var connectionString =
            $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
        services.AddScoped<IServiceRequestService, ServiceRequestService>();
        services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
        services.AddScoped<IServiceRequestService, ServiceRequestService>();
        services.AddScoped<IEmailService, EmailService>();
        var jwtKey =
            config["JwtSettings:Key"]
            ?? throw new InvalidOperationException("JWT_KEY no definido en el archivo .env");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = config["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = config["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization();

        var rawOrigins =
            config["CorsSettings:AllowedOrigins"]
            ?? throw new InvalidOperationException(
                "ALLOWED_ORIGINS no definido en el archivo .env"
            );
        var allowedOrigins = rawOrigins.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        services.AddCors(options =>
        {
            options.AddPolicy(
                "FrontendPolicy",
                policy =>
                {
                    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
                }
            );
        });

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sistema Servicios API", Version = "v1" });

            c.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Ingresa el token así: Bearer {tu_token}",
                }
            );

            c.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer",
                            },
                        },
                        Array.Empty<string>()
                    },
                }
            );
        });

        return services;
    }
}
