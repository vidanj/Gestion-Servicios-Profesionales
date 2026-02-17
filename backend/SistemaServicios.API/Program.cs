using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURACIÓN DE SERVICIOS (AGREGAR AL CONTENEDOR)
// ==========================================

// A) Base de Datos (PostgreSQL)
// Esto lee la conexión de tu archivo appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// B) Controladores (IMPORTANTE: Esto faltaba)
// Sin esto, UsersController es invisible.
builder.Services.AddControllers();

// C) Swagger / OpenAPI (IMPORTANTE: Esto faltaba)
// Esto crea la documentación y la página de pruebas.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ==========================================
// 2. CONFIGURACIÓN DEL PIPELINE HTTP
// ==========================================

// Configurar Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Esto le dice a la app: "Usa las rutas que definí en UsersController"
app.MapControllers();

app.Run();