using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SistemaServicios.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN LIMPIA ---
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddControllers();

// --- 2. CONFIGURACIÓN OPENAPI ---
builder.Services.AddOpenApi();

var app = builder.Build();

// --- 3. PIPELINE ---

// NUEVO: Manejador global de excepciones manual y a prueba de errores (Issue #140)
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
