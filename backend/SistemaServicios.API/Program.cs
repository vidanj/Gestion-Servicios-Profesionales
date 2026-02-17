using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;

var builder = WebApplication.CreateBuilder(args);

// --- INICIO DE CONFIGURACIÓN DE BASE DE DATOS ---
// Esto lee la conexión de tu archivo appsettings.json y conecta PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
// --- FIN DE CONFIGURACIÓN DE BASE DE DATOS ---

// Add services to the container.
// builder.Services.AddOpenApi(); // <-- COMENTADO: Esto daba error en .NET 8

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi(); // <-- COMENTADO: Esto daba error en .NET 8
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}