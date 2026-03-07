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
export $(grep -v '^#' .env | xargs)
dotnet clean backend
dotnet tool install --tool-path backend dotnet-ef
dotnet restore backend
dotnet tool restore
dotnet build backend
dotnet ef database update --project backend/SistemaServicios.API
dotnet run --project backend/SistemaServicios.API
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

ALLOWED_ORIGINS=http://localhost:3000

NEXT_PUBLIC_ALLOWED_PATH=http://localhost:5000

SMTP_HOST= smtp.gmail.com
SMTP_PORT=587
SMTP_USER= usuario del correo
SMTP_PASSWORD= clave
SMTP_FROM= remitente

Nota: los smtp user, from y password se generan para el ambiente de prueba, desde tu cuenta de gmail ve a configuración, añade contraseña de app y listo; User y from serían tu propio correo. En caso de producción, tener un correo dedicado para esto como no_reply
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


## 🔌 Documentación de la API (Endpoints)

La API REST está documentada de forma interactiva a través de **Swagger / OpenAPI**.
Para visualizar la interfaz gráfica y probar las rutas en tu máquina, levanta el proyecto backend (`dotnet run`) y navega a la ruta `/swagger`.

### Módulo de Autenticación (`/api/Auth`)
| Método | Endpoint | Descripción | Acceso |
|---|---|---|---|
| `POST` | `/api/Auth/login` | Inicia sesión y devuelve un token JWT | Público |
| `POST` | `/api/Auth/register` | Registra un nuevo usuario en la plataforma | Público |
| `GET`  | `/api/Auth/me` | Obtiene los claims y datos del usuario actual | Autenticado |

### Módulo de Usuarios (`/api/Users`)
| Método | Endpoint | Descripción | Acceso |
|---|---|---|---|
| `GET`  | `/api/Users` | Lista todos los usuarios activos (con paginación) | Autenticado |
| `GET`  | `/api/Users/{id}` | Obtiene los detalles de un usuario por su UUID | Autenticado |
| `POST` | `/api/Users` | Crea un usuario internamente (validando unicidad) | Autenticado |
| `PUT`  | `/api/Users/{id}` | Actualiza la información básica y rol de un usuario | Autenticado |
| `DELETE`| `/api/Users/{id}` | Realiza un borrado lógico (Soft Delete) del usuario | Autenticado |

### Módulo de Administración (`/api/Admin`)
| Método | Endpoint | Descripción | Acceso |
|---|---|---|---|
| `POST` | `/api/Admin/backup` | Ejecuta un volcado SQL para el respaldo de base de datos | Admin |


> Los archivos `.sql` y el contenido de `backups/` están en `.gitignore` y no se suben al repositorio.

---
