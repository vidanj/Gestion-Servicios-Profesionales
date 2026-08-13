using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SistemaServicios.API.Extensions;
using SistemaServicios.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN LIMPIA (Aquí llamamos a tu clase nueva) ---
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddControllers();

// --- 2. CONFIGURACIÓN OPENAPI (.NET 9 NATIVO) ---
builder.Services.AddOpenApi();

var app = builder.Build();

// --- 3. PIPELINE ---

// Primero de todo: el resto del pipeline debe ver ya la dirección real del cliente.
// Registrado más abajo, la autenticación y cualquier limitación por dirección
// seguirían viendo la del proxy.
// El diagnóstico va delante para ver la cadena tal como llega, antes de que
// UseForwardedHeaders consuma las entradas que aplica.
app.UseMiddleware<ForwardedHeadersDiagnostics>();
app.UseForwardedHeaders();
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

// Las sondas quedan fuera de la redirección a HTTPS. Hoy no redirige porque no hay
// puerto HTTPS configurado, pero si alguien lo añadiera, la sonda del contenedor
// recibiría un 307 y el contenedor pasaría a considerarse enfermo sin estarlo.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/health"),
    branch => branch.UseHttpsRedirection()
);

app.UseStaticFiles();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Liveness: responde mientras el proceso viva. No consulta dependencias a propósito;
// si lo hiciera, una base caída provocaría reinicios en bucle de un proceso sano.
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = HealthCheckResponseWriter.WriteAsync,
    }
);

// Readiness: solo está listo si además puede alcanzar la base de datos.
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = HealthCheckResponseWriter.WriteAsync,
    }
);

app.MapControllers();

app.Run();

// Necesario para que WebApplicationFactory<Program> pueda acceder a este ensamblado en los tests
public partial class Program;
