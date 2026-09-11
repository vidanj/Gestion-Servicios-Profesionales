# Contrato: respuestas simuladas de la API

Obtenido por ingeniería inversa de los controllers y DTOs en `dev` @ 0159a9b. Es la **única**
fuente de las respuestas que devuelve `tests/support/mock-api.ts`. Los puntos marcados como
*a confirmar* se verifican en T010 antes de escribir las pruebas.

| Endpoint | Respuesta de éxito | Errores simulados |
|---|---|---|
| `GET /api/services?page&size&categoryId&professionalId` | `200 { "data": ServiceDto[], "totalCount": n, "page": p, "size": s }` | `500 { "message": "Error interno del servidor." }` |
| `GET /api/services/{id}` | `200 ServiceDto` | `404` (inexistente); `ServiceDto` con `isActive: false` (inactivo) |
| `GET /api/categories` | `200 CategoryDto[]` *(envoltura a confirmar)* | `500` |
| `GET /api/services/my?page&size` | `200 { "data": ServiceDto[], "totalCount", "page", "size" }` *(a confirmar)* | `401`, `403` |
| `POST /api/services` | `201 ServiceDto` | `400 { errors }` (validación: título requerido ≤ 200, precio ≥ 0.01, categoría requerida) |
| `PUT /api/services/{id}` | `200 ServiceDto` | `400`, `403` (no es el dueño), `404` |
| `PATCH /api/services/{id}/toggle` | `200 ServiceDto` *(cuerpo a confirmar)* | `403`, `404` |
| `DELETE /api/services/{id}` | `204` *(código a confirmar)* | `403`, `404` |
| `POST /api/servicerequests` | `201 ServiceRequestDto` con `status` Pending | `400` (descripción requerida ≤ 1000), `401`, `404` (servicio no disponible), `500` |
| `GET /api/servicerequests/my?page&size` | `200 { "data": ServiceRequestDto[], … }` *(envoltura a confirmar)* | `401` |
| `GET /api/servicerequests/professional?page&size` | ídem | `401`, `403` |
| `PUT /api/servicerequests/{id}/status?newStatus={n}` | `200 ServiceRequestDto` | `400 { "message": "Transición de estado inválida: …" }`, `401`, `403`, `404` |

## Reglas

- Los errores `500` simulados **no** incluyen `error = ex.Message`: el cliente no debe depender de
  ese campo, y su presencia en la API real es una brecha registrada (B19).
- Cada prueba declara solo las rutas que necesita; una petición no simulada debe fallar la prueba
  (ruta comodín `**/api/**` que responde `501` y la registra).
