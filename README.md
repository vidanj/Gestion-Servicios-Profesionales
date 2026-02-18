# Gestion-Servicios-Profesionales
El Sistema de Gestión de Servicios Profesionales es una aplicación web orientada a la intermediación entre clientes y profesionales, permitiendo la publicación, consulta, cotización, solicitud, seguimiento y evaluación de servicios, así como la administración y verificación de los usuarios profesionales.


# Stack Tecnológico

## Frontend
- Node.js v24.13.1
- Next.js 14+
- React 18+
- TypeScript
- TailwindCSS
- Axios
- Zustand
- React Hook Form
- Yup

## Backend
- .NET SDK 9.0.201
- ASP.NET Core Web API
- Entity Framework Core
- Npgsql (PostgreSQL Provider)
- JWT Authentication
- Swagger

## Base de Datos
- PostgreSQL 14+

# Requisitos Previos

Verificar instalación:

```bash
dotnet --version
# 9.0.201

node -v
# v24.13.1
```

Si no están instalados:
    .NET SDK 9 -> https://dotnet.microsoft.com/download
    Node.js 24 -> https://nodejs.org/en
    PostgreSQL -> https://www.postgresql.org/download/

# Configurar .env
En la raíz del repositorio crear un archivo .env
```bash
/repo-root
   .env        👈 Crear aquí
   global.json
   /backend
   /frontend
```
Configurar Conexion a postgresSQL
```bash
DB_CONNECTION=Host=localhost;Port=5432;Database=DATABASE;Username=USER;Password=PASS
```

## Probar Backend:

```bash
cd backend
dotnet clean
dotnet tool install dotnet-ef
dotnet restore
dotnet tool restore
dotnet build
cd SistemaServicios.API
dotnet ef database update
dotnet run
#http://localhost:5116/openapi/v1.json
```

## Probar Frontend
```bash
cd frontend
npm install
npm run dev
#http://localhost:3000
```


    
