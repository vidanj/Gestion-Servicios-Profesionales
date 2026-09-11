# Data Model: Pruebas automáticas E2E asistidas por IA

## Fixtures

Todas las formas provienen de los DTOs reales (`backend/SistemaServicios.API/DTOs/`), en
camelCase (serialización por defecto de ASP.NET Core).

### ServiceFixture (`ServiceDto`)

| Campo | Tipo | Reglas (de `CreateServiceDto`) |
|---|---|---|
| `id` | number | > 0 |
| `professionalId` | string (uuid) | — |
| `professionalName` | string | — |
| `categoryId` | number | Requerido |
| `categoryName` | string | — |
| `title` | string | Requerido, ≤ 200 |
| `description` | string \| null | ≤ 1000 |
| `basePrice` | number | ≥ 0.01 |
| `imageUrl` | string \| null | ≤ 2048 |
| `isActive` | boolean | — |
| `createdAt` / `updatedAt` | string ISO / null | — |

Conjuntos: `servicioActivo`, `servicioInactivo`, `catalogoVacio` (`[]`), `catalogoGrande` (50 elementos).

### CategoryFixture (`CategoryDto`)

`id`, `name` (≤ 200), `description`, `iconUrl`, `isActive`.

### RequestFixture (`ServiceRequestDto`)

| Campo | Tipo | Reglas |
|---|---|---|
| `id` | number | — |
| `clientId`, `professionalId` | string (uuid) | — |
| `clientName`, `professionalName`, `serviceTitle` | string | — |
| `serviceId` | number | — |
| `quotedPrice` | number | Igual al `basePrice` del servicio al crear |
| `status` | `RequestStatus` | Pending 0, Accepted 1, InProgress 2, Completed 3, Cancelled 4 (serialización a confirmar en T010) |
| `description` | string \| null | Requerida al crear, ≤ 1000 |
| `requestDate` / `scheduledDate` / `completionDate` | ISO / null | — |

Conjuntos: uno por estado (`pendiente`, `aceptada`, `enProgreso`, `completada`, `cancelada`).

### SessionFixture

| Rol | `localStorage.token` | `localStorage["auth-user"]` (Zustand persistido) |
|---|---|---|
| cliente | `fake-jwt-client` | `{"state":{"firstName":"Ana","lastName":"Prueba","email":"ana@ejemplo.test","role":"Client"},"version":0}` |
| profesional | `fake-jwt-professional` | ídem con `"role":"Professional"` (y variante `"2"`) |
| admin | `fake-jwt-admin` | ídem con `"role":"Admin"` |

Solo dominios `*.test`; ningún dato real.

## Mapa escenario → prueba (trazabilidad)

| Escenario | Requisito | Archivo | Título de la prueba |
|---|---|---|---|
| US1-1 | FR-001 | `catalogo.spec.ts` | muestra título, categoría y precio de cada servicio |
| US1-2 | FR-001 | `catalogo.spec.ts` | muestra mensaje de catálogo vacío |
| US1-3 | FR-001 | `solicitar-servicio.spec.ts` | envía la solicitud y la muestra como Pendiente |
| US1-4 | FR-001 | `solicitar-servicio.spec.ts` | servicio no disponible no permite solicitar |
| US1-5 | FR-001, FR-007 | `solicitar-servicio.spec.ts` | error del servidor conserva el formulario |
| US1-6 | FR-001 | `solicitar-servicio.spec.ts` | sesión vencida lleva a iniciar sesión |
| US2-1 … US2-6 | FR-001 | `profesionista.spec.ts` | lista propia · alta · validación · desactivar · editar · eliminar |
| US3-1 … US3-6 | FR-001 | `solicitudes.spec.ts` | aceptar · iniciar y completar · cancelar · estados finales · transición rechazada · vista del cliente |
| Borde: doble clic | FR-001 | `solicitar-servicio.spec.ts` | doble clic no crea dos solicitudes |
| Borde: rol cliente en `/profesionista` | FR-001 | `profesionista.spec.ts` | cliente no gestiona servicios |
