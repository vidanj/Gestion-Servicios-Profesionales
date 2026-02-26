using SistemaServicios.API.Extensions; 
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN LIMPIA 
builder.Services.AddApplicationServices(builder.Configuration);

// MODIFICACIÓN AQUÍ: Agregamos la regla para evitar ciclos infinitos en JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();