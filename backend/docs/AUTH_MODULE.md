# Modulo de Autenticacion — Documentacion Tecnica

## Tabla de Contenidos

1. [Arquitectura y patron de diseno](#1-arquitectura-y-patron-de-diseno)
2. [Estructura de archivos](#2-estructura-de-archivos)
3. [Configuracion del entorno](#3-configuracion-del-entorno)
4. [Referencia de endpoints](#4-referencia-de-endpoints)
5. [DTOs — contratos de entrada y salida](#5-dtos--contratos-de-entrada-y-salida)
6. [Flujo interno de cada operacion](#6-flujo-interno-de-cada-operacion)
7. [Seguridad implementada](#7-seguridad-implementada)
8. [Como ejecutar el proyecto](#8-como-ejecutar-el-proyecto)
9. [Como probar con Swagger UI](#9-como-probar-con-swagger-ui)
10. [Como probar con curl](#10-como-probar-con-curl)
11. [Como probar con Postman](#11-como-probar-con-postman)
12. [Errores esperados y sus causas](#12-errores-esperados-y-sus-causas)
13. [Inyeccion de dependencias — mapa completo](#13-inyeccion-de-dependencias--mapa-completo)

---

## 1. Arquitectura y patron de diseno

El modulo sigue el patron **Fat Model, Skinny Controller**:

```
HTTP Request
     │
     ▼
┌────────────────────┐
│   AuthController   │  ← SKINNY: solo recibe, delega y responde HTTP
│  (Controllers/)    │
└────────┬───────────┘
         │ llama a
         ▼
┌────────────────────┐
│    AuthService     │  ← FAT: toda la logica de negocio vive aqui
│   (Services/)      │     - valida reglas de negocio
└────┬───────┬───────┘     - orquesta repo + token
     │       │
     ▼       ▼
┌─────────┐ ┌──────────────┐
│  User   │ │ TokenService │  ← genera JWT firmado
│ Repo    │ │  (Services/) │
│(Repos/) │ └──────────────┘
└─────────┘
     │
     ▼
┌────────────┐
│ AppDbContext│  ← Entity Framework Core → PostgreSQL
└────────────┘
```

Cada capa depende solo de la interfaz de la siguiente, nunca de la implementacion concreta.

---

## 2. Estructura de archivos

```
SistemaServicios.API/
│
├── Controllers/
│   └── AuthController.cs          # Endpoints HTTP (skinny)
│
├── Services/
│   ├── AuthService.cs             # Logica de negocio de auth (fat model)
│   └── TokenService.cs            # Generacion de JWT
│
├── Repositories/
│   └── UserRepository.cs          # Acceso a la tabla Users en PostgreSQL
│
├── Interfaces/
│   ├── IAuthService.cs            # Contrato del servicio de auth
│   ├── ITokenService.cs           # Contrato del generador de tokens
│   └── IUserRepository.cs         # Contrato del repositorio de usuarios
│
├── DTOs/
│   └── Auth/
│       ├── LoginRequestDto.cs     # Cuerpo del request de login
│       ├── RegisterRequestDto.cs  # Cuerpo del request de registro
│       └── AuthResponseDto.cs     # Respuesta unificada (token + datos)
│
├── Models/
│   └── User.cs                    # Entidad de base de datos (sin cambios)
│
├── Extensions/
│   └── ApplicationServiceExtensions.cs  # DI + JWT + .env (modificado)
│
├── Program.cs                     # Pipeline HTTP (modificado: +UseAuthentication)
├── appsettings.json               # Config no sensible (modificado)
└── .env                           # Secretos (nunca subir a git)
```

---

## 3. Configuracion del entorno

### Archivo `.env` (raiz del repositorio)

```env
DB_CONNECTION=Server=localhost;Database=ServiciosProfesionalesDB;User Id=postgres;Password=123;

JWT_KEY=UTUXSpZAvs1uzUUPHEAkjytWoAszbFgKBqpYb4l7D93eDRN9P2h+m2vKkGLrU7xq
JWT_ISSUER=SistemaServicios.API
JWT_AUDIENCE=SistemaServicios.Client
JWT_EXPIRES_MINUTES=60
```

| Variable              | Descripcion                                    | Requerida |
|-----------------------|------------------------------------------------|-----------|
| `DB_CONNECTION`       | Cadena de conexion a PostgreSQL                | Si        |
| `JWT_KEY`             | Clave secreta para firmar los tokens (min 32 chars) | Si   |
| `JWT_ISSUER`          | Emisor del token (identifica el servidor)      | Si        |
| `JWT_AUDIENCE`        | Audiencia del token (identifica el cliente)    | Si        |
| `JWT_EXPIRES_MINUTES` | Duracion del token en minutos                  | No (default: 60) |

> **IMPORTANTE:** El archivo `.env` NUNCA debe subirse a git. Verificar que este en `.gitignore`.

### Como se inyecta en el sistema de configuracion

`ApplicationServiceExtensions.cs` carga el `.env` con DotNetEnv y luego mapea las variables al sistema de configuracion de ASP.NET Core:

```csharp
((IConfigurationBuilder)config).AddInMemoryCollection(new Dictionary<string, string?>
{
    ["JwtSettings:Key"]              = Environment.GetEnvironmentVariable("JWT_KEY"),
    ["JwtSettings:Issuer"]           = Environment.GetEnvironmentVariable("JWT_ISSUER"),
    ["JwtSettings:Audience"]         = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
    ["JwtSettings:ExpiresInMinutes"] = Environment.GetEnvironmentVariable("JWT_EXPIRES_MINUTES"),
});
```

Esto permite que `config["JwtSettings:Key"]` funcione en `TokenService` sin acoplarse directamente a variables de entorno.

---

## 4. Referencia de endpoints

Base URL: `http://localhost:5000`

### POST `/api/auth/register`

Registra un nuevo usuario en el sistema.

- **Autenticacion requerida:** No
- **Content-Type:** `application/json`

**Request body:**
```json
{
  "email": "usuario@ejemplo.com",
  "password": "miPassword123",
  "firstName": "Juan",
  "lastName": "Perez",
  "phoneNumber": "+504 9999-9999",
  "role": 1
}
```

| Campo         | Tipo     | Requerido | Validacion              | Valores `role`            |
|---------------|----------|-----------|-------------------------|---------------------------|
| `email`       | `string` | Si        | Formato email valido    | —                         |
| `password`    | `string` | Si        | Minimo 6 caracteres     | —                         |
| `firstName`   | `string` | Si        | Max 255 chars           | —                         |
| `lastName`    | `string` | Si        | Max 255 chars           | —                         |
| `phoneNumber` | `string` | No        | Max 20 chars            | —                         |
| `role`        | `int`    | No        | Default: 1              | `0`=Admin, `1`=Client, `2`=Professional |

**Respuesta exitosa — 201 Created:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "usuario@ejemplo.com",
  "firstName": "Juan",
  "lastName": "Perez",
  "role": "Client"
}
```

**Respuestas de error:**

| Codigo | Condicion                        | Cuerpo                                         |
|--------|----------------------------------|------------------------------------------------|
| `400`  | El email ya esta registrado      | `{ "message": "El correo ya está registrado." }` |
| `400`  | Validacion de campos fallida     | Errores de DataAnnotations de ASP.NET          |

---

### POST `/api/auth/login`

Autentica un usuario existente y retorna un JWT.

- **Autenticacion requerida:** No
- **Content-Type:** `application/json`

**Request body:**
```json
{
  "email": "usuario@ejemplo.com",
  "password": "miPassword123"
}
```

**Respuesta exitosa — 200 OK:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "usuario@ejemplo.com",
  "firstName": "Juan",
  "lastName": "Perez",
  "role": "Client"
}
```

**Respuestas de error:**

| Codigo | Condicion                               | Cuerpo                                               |
|--------|-----------------------------------------|------------------------------------------------------|
| `401`  | Email no existe o password incorrecto   | `{ "message": "Credenciales inválidas." }`           |
| `401`  | Cuenta desactivada (`Status = false`)   | `{ "message": "La cuenta está desactivada." }`       |

> El mensaje es identico para email inexistente o password incorrecto para no revelar si un email existe en el sistema.

---

### GET `/api/auth/me`

Retorna la informacion del usuario autenticado extraida del JWT.

- **Autenticacion requerida:** Si — `Authorization: Bearer <token>`

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Respuesta exitosa — 200 OK:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "usuario@ejemplo.com",
  "role": "Client",
  "firstName": "Juan",
  "lastName": "Perez"
}
```

**Respuestas de error:**

| Codigo | Condicion                          |
|--------|------------------------------------|
| `401`  | Token ausente, invalido o expirado |

---

## 5. DTOs — contratos de entrada y salida

### `LoginRequestDto`

```csharp
public class LoginRequestDto
{
    [Required][EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}
```

### `RegisterRequestDto`

```csharp
public class RegisterRequestDto
{
    [Required][EmailAddress]   public required string Email { get; set; }
    [Required][MinLength(6)]   public required string Password { get; set; }
    [Required][MaxLength(255)] public required string FirstName { get; set; }
    [Required][MaxLength(255)] public required string LastName { get; set; }
    [MaxLength(20)]            public string? PhoneNumber { get; set; }
                               public UserRole Role { get; set; } = UserRole.Client;
}
```

### `AuthResponseDto`

```csharp
public class AuthResponseDto
{
    public string Token { get; set; }      // JWT firmado
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Role { get; set; }       // "Admin" | "Client" | "Professional"
}
```

---

## 6. Flujo interno de cada operacion

### Registro (`RegisterAsync`)

```
1. Verificar si el email ya existe en DB  →  si existe: lanza InvalidOperationException
2. Hashear password con BCrypt (cost factor 10)
3. Crear entidad User con Guid.NewGuid() y DateTime.UtcNow
4. Persistir en PostgreSQL via UserRepository
5. Generar JWT con TokenService
6. Retornar AuthResponseDto
```

### Login (`LoginAsync`)

```
1. Buscar usuario por email en DB  →  no encontrado: lanza UnauthorizedAccessException
2. Verificar Status == true  →  false: lanza UnauthorizedAccessException
3. Verificar password con BCrypt.Verify()  →  incorrecto: lanza UnauthorizedAccessException
4. Generar JWT con TokenService
5. Retornar AuthResponseDto
```

### Generacion de JWT (`CreateToken`)

```
Claims incluidos en el token:
  - ClaimTypes.NameIdentifier  →  user.Id (Guid)
  - ClaimTypes.Email           →  user.Email
  - ClaimTypes.Role            →  user.Role.ToString()
  - "firstName"                →  user.FirstName
  - "lastName"                 →  user.LastName

Algoritmo de firma: HMAC-SHA256
Expiracion: configurable via JWT_EXPIRES_MINUTES (default 60 min)
ClockSkew: TimeSpan.Zero (sin margen de tolerancia)
```

---

## 7. Seguridad implementada

| Aspecto                  | Implementacion                                                        |
|--------------------------|-----------------------------------------------------------------------|
| Hash de passwords        | BCrypt con cost factor 10 (resistente a fuerza bruta)                |
| Algoritmo JWT            | HS256 (HMAC-SHA256)                                                   |
| Secretos en codigo       | Ninguno — todo viene del `.env`                                       |
| Mensaje de error login   | Generico para no revelar si un email existe                           |
| Expiracion de token      | 60 minutos por defecto, configurable                                  |
| ClockSkew                | Cero — el token expira exactamente en el tiempo indicado              |
| Validacion de entrada    | DataAnnotations en DTOs, validadas automaticamente por ASP.NET        |
| Cuenta desactivada       | `Status = false` bloquea el login antes de verificar el password      |

---

## 8. Como ejecutar el proyecto

### Prerrequisitos

- .NET 9 SDK instalado
- PostgreSQL corriendo en `localhost`
- Base de datos `ServiciosProfesionalesDB` creada
- Archivo `.env` configurado en la raiz del repositorio

### Pasos

```bash
# 1. Navegar al proyecto
cd backend/SistemaServicios.API

# 2. Restaurar paquetes NuGet
dotnet restore

# 3. Aplicar migraciones (crea las tablas en PostgreSQL)
dotnet ef database update

# 4. Ejecutar
dotnet run
```

El servidor arranca en: `http://localhost:5000`

---

## 9. Como probar con Swagger UI

1. Iniciar el proyecto con `dotnet run`
2. Abrir en el navegador: `http://localhost:5000/swagger`
3. La interfaz mostrara los tres endpoints del modulo Auth

### Flujo de prueba en Swagger

**Paso 1 — Registrar un usuario**
- Expandir `POST /api/auth/register`
- Clic en "Try it out"
- Pegar el body de ejemplo y ejecutar
- Copiar el `token` de la respuesta

**Paso 2 — Autorizar Swagger con el token**
- Clic en el boton "Authorize" (candado) en la parte superior
- Escribir: `Bearer <token_copiado>`
- Clic en "Authorize" y luego "Close"

**Paso 3 — Probar endpoint protegido**
- Expandir `GET /api/auth/me`
- Clic en "Try it out" y ejecutar
- El servidor validara el token y retornara los datos del usuario

---

## 10. Como probar con curl

### Registro de usuario

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@ejemplo.com",
    "password": "password123",
    "firstName": "Juan",
    "lastName": "Perez",
    "role": 1
  }'
```

### Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@ejemplo.com",
    "password": "password123"
  }'
```

### Obtener perfil (requiere token)

```bash
# Sustituir <TOKEN> por el valor recibido en login o register
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer <TOKEN>"
```

### Probar error — password incorrecto

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@ejemplo.com",
    "password": "passwordIncorrecto"
  }'
# Esperado: 401 Unauthorized { "message": "Credenciales inválidas." }
```

### Probar error — email duplicado

```bash
# Ejecutar el mismo register dos veces con el mismo email
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@ejemplo.com",
    "password": "password123",
    "firstName": "Juan",
    "lastName": "Perez"
  }'
# Esperado: 400 Bad Request { "message": "El correo ya está registrado." }
```

### Probar error — token invalido

```bash
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer tokenInventado123"
# Esperado: 401 Unauthorized
```

---

## 11. Como probar con Postman

### Importar coleccion

Crear una nueva coleccion llamada `SistemaServicios Auth` y agregar las siguientes requests:

#### Request 1 — Register
- **Method:** POST
- **URL:** `http://localhost:5000/api/auth/register`
- **Body → raw → JSON:**
```json
{
  "email": "{{email}}",
  "password": "{{password}}",
  "firstName": "Juan",
  "lastName": "Perez",
  "role": 1
}
```

#### Request 2 — Login
- **Method:** POST
- **URL:** `http://localhost:5000/api/auth/login`
- **Body → raw → JSON:**
```json
{
  "email": "{{email}}",
  "password": "{{password}}"
}
```
- **Tests (captura automatica del token):**
```javascript
const res = pm.response.json();
if (res.token) {
    pm.collectionVariables.set("jwt_token", res.token);
}
```

#### Request 3 — Me (protegido)
- **Method:** GET
- **URL:** `http://localhost:5000/api/auth/me`
- **Authorization:** Bearer Token → `{{jwt_token}}`

#### Variables de coleccion recomendadas

| Variable    | Valor de ejemplo       |
|-------------|------------------------|
| `email`     | `test@ejemplo.com`     |
| `password`  | `password123`          |
| `jwt_token` | (se llena automaticamente con el script del Login) |

---

## 12. Errores esperados y sus causas

| HTTP | Endpoint         | Causa                                   | Respuesta                                              |
|------|------------------|-----------------------------------------|--------------------------------------------------------|
| 400  | `/register`      | Email ya registrado                     | `{ "message": "El correo ya está registrado." }`      |
| 400  | `/register`      | Campo requerido faltante o invalido     | Errores de validacion de ASP.NET ModelState            |
| 400  | `/login`         | Body mal formado                        | Errores de validacion de ASP.NET ModelState            |
| 401  | `/login`         | Email no existe o password incorrecto   | `{ "message": "Credenciales inválidas." }`            |
| 401  | `/login`         | Cuenta desactivada                      | `{ "message": "La cuenta está desactivada." }`        |
| 401  | `/me`            | Sin header Authorization                | Sin cuerpo (ASP.NET rechaza antes del controlador)     |
| 401  | `/me`            | Token expirado                          | Sin cuerpo                                             |
| 401  | `/me`            | Token con firma invalida                | Sin cuerpo                                             |
| 500  | Cualquiera       | JWT_KEY no definido en .env             | Error de servidor — revisar configuracion del .env     |

---

## 13. Inyeccion de dependencias — mapa completo

Registradas en `ApplicationServiceExtensions.cs` con lifetime **Scoped** (una instancia por request HTTP):

```
IUserRepository  →  UserRepository
ITokenService    →  TokenService
IAuthService     →  AuthService
```

El grafo de dependencias en tiempo de ejecucion:

```
AuthController
    └── IAuthService
            └── AuthService
                    ├── IUserRepository → UserRepository → AppDbContext
                    └── ITokenService   → TokenService   → IConfiguration
```

---

*Documentacion generada para el modulo de autenticacion — SistemaServicios.API v1.0*
