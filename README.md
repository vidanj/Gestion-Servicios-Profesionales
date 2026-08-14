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
 
## 🩺 Sondas de Disponibilidad

Dos endpoints anónimos, pensados para orquestadores y balanceadores. Son distintos a propósito: uno responde *"no me reinicies"* y el otro *"puedo recibir tráfico"*.

| Ruta | Comprueba | Respuestas |
|------|-----------|-----------|
| `GET /health/live` | Solo que el proceso esté vivo. **No consulta dependencias.** | `200 Healthy` mientras el proceso responda |
| `GET /health/ready` | Además, que PostgreSQL esté alcanzable (timeout 3 s) | `200 Healthy` / `503 Unhealthy` |

```bash
curl -i http://localhost:5000/health/live
curl -i http://localhost:5000/health/ready
```

**Por qué liveness no mira la base de datos:** si lo hiciera, una caída de PostgreSQL haría que el orquestador reiniciara un proceso perfectamente sano, una y otra vez, sin arreglar nada. Con la separación, una base caída saca la instancia de rotación (`503` en readiness) pero no provoca reinicios.

La respuesta es JSON con el estado y la duración de cada comprobación. **No incluye la cadena de conexión, el host de la base ni trazas de excepción**: los endpoints son anónimos y el detalle va al log.

El `Dockerfile` declara un `HEALTHCHECK` contra `/health/ready` con `start-period` de 60 s, margen que cubre el tiempo que `entrypoint.sh` dedica a aplicar migraciones antes de que Kestrel empiece a escuchar.

## 🔀 Proxy Inverso y Dirección Real del Cliente

En producción hay **dos proxies** delante de la API: Cloudflare y el edge de Render. Sin configuración, `HttpContext.Connection.RemoteIpAddress` sería la del proxy, no la del usuario.

| Variable | Por defecto | Qué hace |
|----------|-------------|----------|
| `FORWARDED_LIMIT` | `2` | Cuántos proxies de confianza hay delante |
| `FORWARDED_NETWORKS` | *(vacío)* | Redes CIDR del proxy, separadas por comas |

### Por qué `FORWARDED_LIMIT` es lo que protege

`X-Forwarded-For` se **anexa**, no se reemplaza: un cliente puede enviar una dirección inventada y los proxies añadirán las suyas *a la derecha*. El middleware toma las `N` entradas más a la derecha y descarta el resto.

```
Cliente envía:   X-Forwarded-For: 9.9.9.9
NGINX anexa:     X-Forwarded-For: 9.9.9.9, 203.0.113.7
Con LIMIT=1  ->  dirección resuelta: 203.0.113.7   (la falsificada se descarta)
```

**Un valor mayor que los saltos reales hace confiar en entradas que controla quien llama.** Uno menor deja la dirección del proxy en lugar de la del cliente.

### Cómo determinar el valor correcto

Subir el nivel del middleware de diagnóstico a `Debug` y leer la cadena tal como llega:

```
Logging__LogLevel__SistemaServicios.API.Middleware=Debug
```

```
X-Forwarded-For recibido: 9.9.9.9, 172.18.0.1 | dirección resuelta: 172.18.0.1
```

`FORWARDED_LIMIT` debe ser el número de entradas que añaden los proxies de confianza, contando desde la derecha.

### Verificarlo en local

`docker-compose.yml` levanta NGINX delante de la API, con la API **sin publicar al exterior**, igual que en producción:

```bash
docker compose up --build
curl -s http://localhost:8080/health/ready
curl -s -H "X-Forwarded-For: 9.9.9.9" http://localhost:8080/health/ready
```

En producción el proxy lo pone Render; este compose sirve para verificar el comportamiento y como base si algún día se autoaloja.

## 📋 Observabilidad — Logs y Telemetría

### Dos bitácoras que no son lo mismo

El sistema escribe en dos sitios distintos y conviene no confundirlos, porque
responden preguntas diferentes:

|  | `UserLog` (tabla) | Telemetría (stdout) |
|---|---|---|
| Responde | *quién hizo qué* | *por qué el sistema respondió mal o lento* |
| Vive en | PostgreSQL | stdout → recolector de la plataforma |
| Se consulta | desde el panel de administración | durante un incidente |
| Retención | permanente, es evidencia | la que decida el agregador |

Si un usuario reclama que le borraron algo, se mira `UserLog`. Si la aplicación
va lenta o devuelve 500, se miran los logs de operación. Este apartado va de los
segundos; los primeros no se han tocado.

### Formato

Serilog escribe **JSON compacto a stdout**, nunca a archivo: el contenedor es
efímero y un archivo se perdería en cada redespliegue. Docker y Render recogen
stdout sin configurar nada.

```bash
docker logs -f <contenedor>                 # en local
# En Render: pestaña "Logs" del servicio
```

Cada línea lleva `TraceId` y `SpanId`. **Esa es la propiedad que hace útil el
log**: durante un incidente permite reunir todas las líneas de una misma
petición en lugar de adivinar cuáles corresponden al usuario que se quejó.

```bash
# Todas las líneas de una petición concreta
docker logs <contenedor> | grep '"TraceId":"06ded6db324384e443897587f45aec09"'
```

Un login fallido en producción deja **un solo evento**, elevado a `Warning` por
ser 4xx:

```json
{"@t":"...","@mt":"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms",
 "@l":"Warning","StatusCode":401,"Elapsed":1361.32,"ClientIp":"::1",
 "TraceId":"06ded6db...","SpanId":"5ed3357b...","Environment":"Production"}
```

Las sondas de `/health` **no generan ninguna línea**: el contenedor las consulta
cada 30 s y, sin excluirlas, el log sería sobre todo ruido de sondas. Si una
sonda falla se nota porque el contenedor se reinicia, no por una línea de log.

### Secretos

Un enricher redacta las propiedades cuyo nombre delata un secreto —contraseñas,
tokens, `Authorization`, cadenas de conexión, `PGPASSWORD`—, incluso dentro de
objetos volcados enteros con `{@Dto}`.

**Su alcance tiene un límite que conviene conocer:** actúa sobre las propiedades
estructuradas, no sobre texto ya interpolado en la plantilla del mensaje. Es
decir, `_logger.LogInformation($"clave {clave}")` sí filtra el secreto. La regla
sigue siendo **no meter secretos en la plantilla**; el enricher es la red que
recoge el descuido habitual, que es volcar el DTO completo. Ambos límites están
documentados con pruebas en `SecretRedactionEnricherTests`.

### Niveles

Se configuran en `appsettings.json` (producción) y `appsettings.Development.json`:
`Information` en producción y `Debug` en desarrollo, con el SQL de EF Core en
`Warning` en producción para no volcar cada consulta.

### Trazas

OpenTelemetry instrumenta ASP.NET Core, `HttpClient` y Npgsql. La exportación al
colector es **opcional**:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

Sin esa variable la aplicación **arranca igual y no exporta nada**, que es la
situación actual: no hay colector desplegado. La instrumentación sigue activa
aunque no se exporte, porque de ella sale el `TraceId` de los logs.

No se usa el paquete de instrumentación de EF Core ni el exportador de
Prometheus: ambos siguen en preestreno y este build trata las advertencias como
errores. Las trazas de base de datos se obtienen del driver con `AddNpgsql()`,
que sí es estable.

## 📊 Índices de Base de Datos — Notas de Diseño

### `Users.Status` (índice parcial)

```csharp
modelBuilder.Entity<User>().HasIndex(u => u.Status).HasFilter("\"Status\" = true");
```

**Por qué parcial y no un índice normal:** `Status` es un `bool` (baja cardinalidad — solo dos valores posibles), y todas las consultas de lectura del sistema filtran exclusivamente por usuarios activos (`Status = true`) — el listado paginado (`GetUsersAsync`/`GetUserDtosAsync`) y la búsqueda por Id (`GetByIdAsync`/`GetUserDtoByIdAsync`) en `UserRepository.cs`. Un índice completo indexaría también las filas con `Status = false`, que nunca se consultan directamente (el borrado de usuario es lógico —soft delete—, no se filtra por inactivos en ningún flujo actual), desperdiciando espacio y costo de mantenimiento en cada escritura sin beneficio de lectura.

**Vigencia del criterio:** si `Status` deja de ser `bool` y pasa a un enum con más estados, este índice debe reevaluarse — el filtro `"Status" = true` ya no tendría sentido tal cual, y habría que identificar cuál sería el nuevo "camino caliente" de lectura (ej. un estado `Active` entre varios) antes de decidir si el índice parcial se mantiene, se amplía, o se reemplaza por uno completo.

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
