# 🚀 Solicitud de Pull Request

Gracias por tu contribución. Por favor, completa la siguiente información para ayudar al equipo a revisar y aprobar este PR de manera eficiente.

---

## 📌 Resumen del Cambio

Integración de `dev-backend` → `testing` para validación de integración del módulo backend completo.

Incluye tres bloques de trabajo fusionados en esta rama:

1. **Módulo de Autenticación JWT** (`feat/login-JWTAUTH`): implementación de endpoints `POST /api/auth/login`, `POST /api/auth/register` y `GET /api/auth/me` con tokens JWT firmados, autorización por roles (`Admin` / `Client`), hashing BCrypt y validación de cuenta activa.

2. **Code Coverage y Pruebas Unitarias** (`test/58-test-code-coverage`): suite de 69 pruebas (unitarias + integración) sobre `AuthService`, `TokenService`, `UserRepository`, `AuthController` y `AdminController`, con reporte de cobertura via `dotnet-reportgenerator-globaltool`.

3. **Sistema de Respaldo PostgreSQL** (`feat/47-Respaldo-PostgressSQL`): endpoint protegido `POST /api/admin/backup` (solo rol `Admin`) que genera un volcado SQL con `pg_dump`, scripts CLI multiplataforma (`.sh` / `.ps1`) para backup y restauración, y corrección de seguridad eliminando el campo `Role` de `RegisterRequestDto`.

---

## 🔍 Tipo de Cambio realizado

Marca con una `x` lo que aplica:

- [ ] 🐞 Corrección de error (`fix`)
- [x] ✨ Nueva funcionalidad (`feat`)
- [ ] ♻️ Refactorización del código sin cambios funcionales (`refactor`)
- [x] 🧪 Agregado o mejora de pruebas (`test`)
- [ ] 🧱 Cambio en configuración CI/CD (`ci`)
- [ ] 🚀 Mejora de rendimiento (`perf`)
- [x] 📚 Cambios en la documentación (`docs`)
- [ ] Otro (especificar):

---

## 📂 Archivos Afectados

**Autenticación JWT — nuevos:**
- `backend/SistemaServicios.API/Controllers/AuthController.cs`
- `backend/SistemaServicios.API/DTOs/Auth/AuthResponseDto.cs`
- `backend/SistemaServicios.API/DTOs/Auth/LoginRequestDto.cs`
- `backend/SistemaServicios.API/DTOs/Auth/RegisterRequestDto.cs`
- `backend/SistemaServicios.API/Interfaces/IAuthService.cs`
- `backend/SistemaServicios.API/Interfaces/ITokenService.cs`
- `backend/SistemaServicios.API/Interfaces/IUserRepository.cs`
- `backend/SistemaServicios.API/Repositories/UserRepository.cs`
- `backend/SistemaServicios.API/Services/AuthService.cs`
- `backend/SistemaServicios.API/Services/TokenService.cs`

**Sistema de Respaldo — nuevos:**
- `backend/SistemaServicios.API/Controllers/AdminController.cs`
- `backend/SistemaServicios.API/DTOs/Admin/BackupResponseDto.cs`
- `backend/SistemaServicios.API/Interfaces/IBackupService.cs`
- `backend/SistemaServicios.API/Services/BackupService.cs`
- `scripts/backup.sh` / `scripts/backup.ps1`
- `scripts/restore.sh` / `scripts/restore.ps1`
- `backups/.gitkeep`

**Pruebas — nuevos:**
- `backend/SistemaServicios.Tests/Unit/AuthServiceTests.cs`
- `backend/SistemaServicios.Tests/Unit/AuthControllerTests.cs`
- `backend/SistemaServicios.Tests/Unit/TokenServiceTests.cs`
- `backend/SistemaServicios.Tests/Unit/UserRepositoryTests.cs`
- `backend/SistemaServicios.Tests/Unit/AdminControllerTests.cs`
- `backend/SistemaServicios.Tests/Unit/BackupServiceTests.cs`
- `backend/SistemaServicios.Tests/Integration/AuthControllerTests.cs`
- `backend/SistemaServicios.Tests/Integration/AdminControllerTests.cs`
- `backend/SistemaServicios.Tests/Integration/CustomWebApplicationFactory.cs`
- `backend/SistemaServicios.Tests/coverlet.runsettings`
- `backend/SistemaServicios.Tests/SistemaServicios.Tests.csproj`

**Configuración / Infraestructura — modificados:**
- `backend/SistemaServicios.API/Extensions/ApplicationServiceExtensions.cs`
- `backend/SistemaServicios.API/Program.cs`
- `backend/SistemaServicios.API/SistemaServicios.API.csproj`
- `backend/SistemaServicios.API/appsettings.json`
- `backend/SistemaServicios.sln`
- `.config/dotnet-tools.json`
- `.env.example`
- `.gitattributes`
- `.gitignore`
- `.github/workflows/backend-tests.yml`
- `README.md`
- `backend/docs/AUTH_MODULE.md`

---

## 🧪 ¿Cómo Probarlo?

### 1. Levantar el backend

```bash
# Copiar y completar variables de entorno
cp .env.example .env

# Ejecutar migraciones y arrancar API
cd backend/SistemaServicios.API
dotnet run
```

### 2. Ejecutar la suite de pruebas

```bash
cd backend
dotnet test
# Resultado esperado: 69 pruebas — 69 aprobadas, 0 fallidas
```

### 3. Verificar autenticación

```bash
# Registro
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Password123!","firstName":"Test","lastName":"User"}'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Password123!"}'

# Me (con el token obtenido)
curl http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer <TOKEN>"
```

### 4. Verificar backup (requiere rol Admin y pg_dump en PATH)

```bash
# Promover usuario a Admin en BD
# UPDATE "Users" SET "Role" = 0 WHERE "Email" = 'test@test.com';

curl -X POST http://localhost:5000/api/admin/backup \
  -H "Authorization: Bearer <TOKEN_ADMIN>"
# Respuesta esperada: 200 con fileName, createdAt, fileSizeBytes
```

---

## ✅ Checklist

Asegúrate de completar lo siguiente antes de enviar:

- [x] He probado mis cambios localmente
- [x] Esta PR sigue el formato de convención de commits (si aplica)
- [x] Se han actualizado o agregado pruebas
- [x] La documentación se actualizó si fue necesario
- [ ] No hay errores en CI/CD

---

## 📎 Notas Adicionales

- Este PR integra tres features completadas y mergeadas en `dev-backend`: autenticación JWT (#43), code coverage (#58) y respaldo de BD (#47).
- **Fix de seguridad incluido:** se elimina `Role` de `RegisterRequestDto` para evitar auto-asignación de rol `Admin` desde el exterior.
- Las variables de conexión cambian de `CONNECTION_STRING` único a cinco vars individuales (`DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`); revisar `.env.example` antes de levantar.
- El endpoint `POST /api/admin/backup` requiere que `pg_dump` esté en el PATH del proceso que ejecuta la API (en Windows, iniciar desde la misma terminal donde `pg_dump --version` funciona, o añadir PostgreSQL bin al PATH de sistema).
- `dev-backend` local está **14 commits por delante** de `origin/dev-backend` — verificar que el remote esté actualizado antes de abrir el PR en GitHub.

---

Gracias 🙌
