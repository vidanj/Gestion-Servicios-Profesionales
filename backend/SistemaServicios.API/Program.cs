using SistemaServicios.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN LIMPIA (Aquí llamamos a tu clase nueva) ---
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddControllers();

// --- 2. CONFIGURACIÓN OPENAPI (.NET 9 NATIVO) ---
builder.Services.AddOpenApi();

var app = builder.Build();

// --- 3. PIPELINE ---
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
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Necesario para que WebApplicationFactory<Program> pueda acceder a este ensamblado en los tests
public partial class Program { }
