using SistemaServicios.API.Extensions; // Usamos tu nueva clase

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN LIMPIA (Aquí llamamos a tu clase nueva) ---
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddControllers();

// --- 2. CONFIGURACIÓN OPENAPI (.NET 9 NATIVO) ---
// Esto es lo que Efrén quería que usaras en vez de Swagger viejo
builder.Services.AddOpenApi();

var app = builder.Build();

// --- 3. PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Genera el JSON de la API
    
    // Agregamos una interfaz bonita para probar (Swagger/Scalar)
    // Nota: En .NET 9 a veces se usa Scalar, pero por ahora MapOpenApi es lo vital.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Sistema Servicios API");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();