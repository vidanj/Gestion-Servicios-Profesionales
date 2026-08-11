using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using SistemaServicios.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN LIMPIA ---
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddControllers();

// NUEVO: Configuración de Rate Limiting (Issue #139)
builder.Services.AddRateLimiter(options =>
{
    // Creamos una política específica llamada "AuthLimiter"
    options.AddFixedWindowLimiter(
        "AuthLimiter",
        opt =>
        {
            opt.PermitLimit = 5; // Máximo 5 peticiones permitidas...
            opt.Window = TimeSpan.FromMinutes(1); // ...en una ventana de 1 minuto
            opt.QueueProcessingOrder = System
                .Threading
                .RateLimiting
                .QueueProcessingOrder
                .OldestFirst;
            opt.QueueLimit = 0; // Si se pasan de 5, rechazamos inmediatamente
        }
    );

    // Si superan el límite, devolvemos el código HTTP 429 (Too Many Requests)
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// --- 2. CONFIGURACIÓN OPENAPI ---
builder.Services.AddOpenApi();

var app = builder.Build();

// --- 3. PIPELINE ---

// Manejador global de excepciones manual y a prueba de errores (Issue #140)
app.Use(
    async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var errorJson =
                "{\"message\":\"Ocurrió un error inesperado en el servidor. Intente más tarde.\"}";
            await context.Response.WriteAsync(errorJson);
        }
    }
);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Servicios API v1");
    });
}

app.Use(
    async (context, next) =>
    {
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    }
);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("FrontendPolicy");

// NUEVO: Activar el middleware de Rate Limiting en el pipeline (Issue #139)
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
