# Especificación del Plan de Pruebas y Casos de Prueba

## 1. Alcance y Estrategia de Pruebas
El sistema implementa una arquitectura de pruebas en tres capas desacopladas para garantizar estabilidad y seguridad:

* **Pruebas Unitarias (Backend):** Validan reglas de negocio y algoritmos de autenticación en aislamiento mediante `xUnit` y `Moq`.
* **Pruebas de Integración (Backend):** Ejercitan el pipeline HTTP de ASP.NET Core en memoria con `WebApplicationFactory`, verificando middlewares (Rate Limiting, Exception Handling) y persistencia en base de datos.
* **Pruebas End-to-End (Frontend):** Simulan navegación y flujos críticos de usuario sobre Chromium, Firefox y WebKit con `Playwright`.

## 2. Inventario de Automatización

| Nivel | Herramienta | Alcance | Comando de Ejecución |
| :--- | :--- | :--- | :--- |
| **Unitario** | xUnit + Moq | Servicios de negocio y tokens | `dotnet test --filter "FullyQualifiedName~Unit"` |
| **Integración** | xUnit + WebApplicationFactory | Pipeline HTTP y controladores | `dotnet test --filter "FullyQualifiedName~Integration"` |
| **E2E** | Playwright | Flujos completos UI / API | `npx playwright test` |

## 3. Casos de Prueba Formales

### CP-SEC-01: Prevención de asignación masiva en registro
* **Nivel:** Integración / Unitario
* **Precondición:** El correo a registrar no existe en la base de datos.
* **Pasos:**
  1. Enviar payload de registro con `"role": "Admin"`.
  2. Procesar la entidad en `AuthService`.
  3. Verificar persistencia en el repositorio.
* **Resultado Esperado:** Rol asignado estrictamente como `UserRole.Client`; HTTP 201 Created.

### CP-SEC-02: Mitigación de fuerza bruta vía Rate Limiting
* **Nivel:** Integración
* **Precondición:** IP cliente sin solicitudes previas en la ventana.
* **Pasos:**
  1. Enviar 5 peticiones consecutivas a `/api/auth/login`.
  2. Enviar una 6.ª petición en la misma ventana de 60 segundos.
* **Resultado Esperado:** Petición bloqueada por `AuthLimiter`; HTTP 429 Too Many Requests.

### CP-SEC-03: Validación binaria de firmas de archivo (Magic Bytes)
* **Nivel:** Unitario
* **Precondición:** Usuario autenticado.
* **Pasos:**
  1. Renombrar script malicioso a `archivo.png`.
  2. Enviar a `POST /api/profile/image`.
  3. Inspeccionar primeros 8 bytes de cabecera en el stream.
* **Resultado Esperado:** Detección de firma inválida; HTTP 400 Bad Request.

### CP-E2E-01: Autenticación de usuario y control de interfaz
* **Nivel:** E2E (Playwright)
* **Precondición:** Frontend y API activos con credenciales válidas.
* **Pasos:**
  1. Navegar a `/login`.
  2. Ingresar credenciales y pulsar `LOGIN`.
  3. Validar overlay de carga y bloqueo de inputs.
* **Resultado Esperado:** Almacenamiento de token JWT y redirección al panel según rol.
