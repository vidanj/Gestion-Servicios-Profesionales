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

- .NET SDK 9 -> https://dotnet.microsoft.com/es-es/download/dotnet/thank-you/sdk-9.0.201-windows-x64-installer
- Node.js 24 -> https://nodejs.org/en
- PostgreSQL -> https://www.postgresql.org/download/

# Configurar .env
En la raíz del repositorio crear un archivo .env
```bash
/repo-root
   .env        👈 Crear aquí
   global.json
   /backend
   /frontend
```
Configurar Conexion a PostgreSQL
```bash
DB_HOST=localhost
DB_PORT=5432
DB_NAME=nombre_db
DB_USER=usuario
DB_PASSWORD=contraseña
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
#http://localhost:5000/openapi/v1.json
```

# Pruebas Unitarias e Integración

## Estructura de las Pruebas

```
backend/SistemaServicios.Tests/
├── Unit/
│   ├── AuthServiceTests.cs         # Registro, login, validaciones de negocio
│   ├── TokenServiceTests.cs        # Generación y validación de JWT
│   ├── AdminControllerTests.cs     # Mapeo HTTP del controller (201 / 500)
│   └── BackupServiceTests.cs       # Validación de variables de entorno
└── Integration/
    ├── CustomWebApplicationFactory.cs  # Factory compartida: InMemory DB + JWT de prueba
    ├── AuthControllerTests.cs          # Pipeline completo de autenticación
    └── AdminControllerTests.cs         # Pipeline completo de backup con mock
```

## Ejecutar las Pruebas

Desde la carpeta `backend/`:

```bash
cd backend
dotnet test
```

Para ver el detalle de cada prueba:

```bash
dotnet test --verbosity normal
```

Para filtrar por categoría (Unit o Integration):

```bash
# Solo pruebas unitarias
dotnet test --filter "FullyQualifiedName~Unit"

# Solo pruebas de integración
dotnet test --filter "FullyQualifiedName~Integration"
```

---

# Code Coverage

## Requisitos

`reportgenerator` está configurado como herramienta local en `.config/dotnet-tools.json`.
Instálala una sola vez desde la raíz del repositorio:

```bash
dotnet tool restore
```

## Generar el Reporte

Desde la carpeta `backend/`:

**1. Ejecutar pruebas con recolección de cobertura:**

```bash
cd backend
dotnet test --collect:"XPlat Code Coverage" --results-directory ../coverage
```

**2. Convertir los datos a un reporte HTML:**

```bash
dotnet reportgenerator \
  -reports:"../coverage/**/coverage.cobertura.xml" \
  -targetdir:"../coverage/report" \
  -reporttypes:Html
```

> En Windows (PowerShell), reemplaza `\` por `` ` `` para continuar línea, o escríbelo en una sola línea.

**3. Abrir el reporte:**

**Linux / macOS:**
```bash
open ../coverage/report/index.html
```

**Windows (PowerShell):**
```powershell
Start-Process ..\coverage\report\index.html
```

> Los archivos generados en `coverage/` están en `.gitignore` y no se suben al repositorio.


## Probar Frontend
```bash
cd frontend
npm install
npm run dev
#http://localhost:3000
```

---

# Respaldo y Restauración de la Base de Datos

Los scripts se encuentran en la carpeta `scripts/` y funcionan en Linux/macOS (`.sh`) y Windows (`.ps1`).

## Variables de entorno necesarias

Los scripts leen las credenciales directamente del archivo `.env` en la raíz del repositorio:

```
DB_HOST=localhost
DB_PORT=5432
DB_NAME=nombre_db
DB_USER=usuario
DB_PASSWORD=contraseña
```

## Generar un respaldo

Los archivos de respaldo se guardan en `backups/` con nombre `backup_YYYYMMDD_HHmm.sql`.

**Linux / macOS:**
```bash
chmod +x scripts/backup.sh
./scripts/backup.sh
# → Genera: backups/backup_20260225_1430.sql
```

**Windows (PowerShell):**
```powershell
.\scripts\backup.ps1
# → Genera: backups\backup_20260225_1430.sql
```

## Restaurar desde un respaldo

> **Advertencia:** la restauración sobreescribe los datos actuales de la base de datos.
> Los scripts solicitan confirmación antes de ejecutar.

**Linux / macOS:**
```bash
./scripts/restore.sh backups/backup_20260225_1430.sql
```

**Windows (PowerShell):**
```powershell
.\scripts\restore.ps1 -BackupFile backups\backup_20260225_1430.sql
```

## Ciclo completo: verificar backup → borrar dato → restaurar

```bash
# 1. Generar respaldo
./scripts/backup.sh

# 2. Simular pérdida de datos (en psql o pgAdmin)
#    DELETE FROM "Users" WHERE "Email" = 'usuario@ejemplo.com';

# 3. Restaurar
./scripts/restore.sh backups/backup_20260225_1430.sql

# 4. Verificar que el dato volvió
#    SELECT * FROM "Users" WHERE "Email" = 'usuario@ejemplo.com';
```

> Los archivos `.sql` y el contenido de `backups/` están en `.gitignore` y no se suben al repositorio.

---
