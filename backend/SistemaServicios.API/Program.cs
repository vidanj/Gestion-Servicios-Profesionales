using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Formatting.Compact;
using SistemaServicios.API.Extensions;
using SistemaServicios.API.Middleware;

// Logger mínimo previo a la construcción del host. AddApplicationServices lanza si falta
// DB_HOST, JWT_KEY o las credenciales SMTP, y esos fallos ocurren antes de que exista el
// logger definitivo: sin esto, un arranque fallido en producción no deja rastro y solo se
// ve el contenedor reiniciándose sin explicación.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(
    (context, _, logger) =>
        LoggingConfiguration.Configure(logger, context.Configuration, context.HostingEnvironment)
);

// --- 1. CONFIGURACIÓN LIMPIA (Aquí llamamos a tu clase nueva) ---
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
            opt.PermitLimit = builder.Configuration.GetValue<int>("AUTH_RATE_LIMIT", 5); // Máximo 5 peticiones permitidas...
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

// Primero de todo: el resto del pipeline debe ver ya la dirección real del cliente.
// Registrado más abajo, la autenticación y cualquier limitación por dirección
// seguirían viendo la del proxy.
// El diagnóstico va delante para ver la cadena tal como llega, antes de que
// UseForwardedHeaders consuma las entradas que aplica.
app.UseMiddleware<ForwardedHeadersDiagnostics>();
app.UseForwardedHeaders();

// Después de UseForwardedHeaders y no antes: registrado delante, la dirección que anotaría
// sería la del proxy y no la del cliente, que es justo lo que resolvió el issue #128.
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (context, _, exception) =>
        LoggingConfiguration.NivelDePeticion(context, exception);

    options.EnrichDiagnosticContext = (diagnostic, context) =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        if (clientIp is not null)
        {
            diagnostic.Set("ClientIp", clientIp);
        }

        // Solo si hay sesión: en las rutas anónimas no existe el reclamo y anotar un
        // UserId vacío haría creer que la petición venía de alguien identificado.
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null)
        {
            diagnostic.Set("UserId", userId);
        }
    };
});

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

// Las rutas de sondeo quedan fuera de la redirección a HTTPS, por dos motivos.
// Si algún día se configura un puerto HTTPS, la sonda del contenedor recibiría un 307 y
// el contenedor pasaría a considerarse enfermo sin estarlo. Y hoy, además, el sondeo
// interno de Render llega sin X-Forwarded-Proto, así que el middleware intentaba redirigir,
// no encontraba puerto HTTPS y dejaba un aviso por arranque. El tráfico real entra por
// Cloudflare ya como https y nunca pasa por ahí.
app.UseWhen(
    context => !RutasDeSondeo.Es(context.Request.Path),
    branch => branch.UseHttpsRedirection()
);

app.UseStaticFiles();
app.UseCors("FrontendPolicy");

// Activar el Rate Limiter solo si NO estamos en el entorno de pruebas automatizadas
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseRateLimiter();
}

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

// Raíz del servicio. Render y Cloudflare la sondean, y sin mapearla cada sondeo
// devolvía 404 y quedaba registrado como Warning, porque los 4xx se elevan: el mismo
// ruido que se evitó en /health colándose por otra puerta.
// Se mapean GET y HEAD porque el sondeo de la plataforma usa HEAD, y MapGet por sí solo
// no lo atiende. La respuesta es deliberadamente escueta: el endpoint es anónimo, así
// que no se exponen versión, entorno ni nada que ayude a perfilar el servicio.
app.MapMethods(
        "/",
        ["GET", "HEAD"],
        () => Results.Ok(new { service = "SistemaServicios.API", status = "ok" })
    )
    .AllowAnonymous();

app.MapControllers();

app.Run();

public partial class Program { }
