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
    app.MapOpenApi(); // Genera el JSON de la API
    
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Sistema Servicios API");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();