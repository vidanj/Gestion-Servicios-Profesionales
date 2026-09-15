# Contrato HTTP — Módulo de créditos de autores

> Contrato acordado en planeación. Es la referencia que debe cumplir la implementación y contra la
> que se escriben las pruebas de integración. Base: `/api/Credits`.

---

## Resumen

| Método y ruta | Acceso | Propósito | Requisitos |
|---|---|---|---|
| `GET /api/Credits` | Público | Título, mensaje y autores activos | FR-001 … FR-007 |
| `GET /api/Credits/{id}` | Admin | Un autor por identificador, activo o no | FR-009 |
| `POST /api/Credits` | Admin | Alta de autor | FR-008, FR-012, FR-014 |
| `PUT /api/Credits/{id}` | Admin | Actualización | FR-009, FR-012 |
| `PATCH /api/Credits/{id}/orden` | Admin | Cambio de orden de presentación | FR-013 |
| `DELETE /api/Credits/{id}` | Admin | Baja lógica | FR-010 |
| `POST /api/Credits/{id}/foto` | Admin | Asociación de fotografía | FR-015 … FR-019 |

**La consulta individual es de administración, no pública.** El recurso público devuelve la
colección completa en una sola respuesta; no hay caso de uso para consultar un autor suelto desde
fuera, y exponerlo permitiría enumerar registros dados de baja.

## Objetos de transferencia

### `CreditsPageDto` — salida de la consulta pública

| Campo | Tipo | Notas |
|---|---|---|
| `title` | texto | Desde configuración; valor por defecto si falta |
| `message` | texto | Desde configuración; cadena vacía si falta |
| `authors` | arreglo de `CreditDto` | Vacío si no hay autores activos |

### `CreditDto` — un autor en la salida

| Campo | Tipo | Notas |
|---|---|---|
| `id` | entero | |
| `fullName` | texto | |
| `projectRole` | texto | |
| `bio` | texto o nulo | |
| `photoUrl` | texto o nulo | Ruta al recurso público de archivos; nulo si no hay foto |
| `profileUrl` | texto o nulo | |
| `displayOrder` | entero | |

**`photoUrl` es una ruta, no un identificador.** Devolver el identificador del archivo obligaría al
consumidor a conocer cómo se arma la ruta de descarga. La respuesta entrega la ruta ya construida.

### `CreateCreditDto` / `UpdateCreditDto` — entrada de administración

| Campo | Obligatorio en alta | Reglas |
|---|---|---|
| `fullName` | Sí | No vacío, máx. 200 |
| `projectRole` | Sí | No vacío, máx. 100 |
| `bio` | No | Máx. 1000 |
| `profileUrl` | No | Máx. 2048, dirección absoluta con esquema seguro |
| `displayOrder` | No | Entero no negativo; en alta se asigna al final si se omite |

`UpdateCreditDto` agrega `isActive` para permitir la reactivación. **Ningún objeto de entrada
acepta `photoFileId`:** la fotografía se asocia exclusivamente por su recurso dedicado, para que la
validación de contenido no pueda saltarse.

---

## Detalle por recurso

### `GET /api/Credits`

Sin autenticación. Sin parámetros.

| Respuesta | Cuándo |
|---|---|
| `200` con `CreditsPageDto` | Siempre que la consulta se resuelva, incluso con cero autores |
| `500` con cuerpo de error genérico | Fallo no previsto; sin detalle de excepción |

Orden: por `displayOrder` ascendente, y ante empate por `id` ascendente. Solo autores activos.

### `GET /api/Credits/{id}`

Requiere rol Admin. Devuelve el autor completo, activo o inactivo.

| Respuesta | Cuándo |
|---|---|
| `200` con `CreditDto` | El autor existe |
| `401` | Sin sesión |
| `403` | Rol distinto de Admin |
| `404` | No existe |

### `POST /api/Credits`

Requiere rol Admin. Cuerpo: `CreateCreditDto`.

| Respuesta | Cuándo |
|---|---|
| `201` con el recurso creado y su ubicación | Alta correcta |
| `400` con los campos inválidos | Validación fallida |
| `401` / `403` | Sin sesión / rol insuficiente |

El autor nace activo y, si se omite el orden, al final de la lista.

### `PUT /api/Credits/{id}`

Requiere rol Admin. Cuerpo: `UpdateCreditDto`. Respuestas: `200`, `400`, `401`, `403`, `404`.

### `PATCH /api/Credits/{id}/orden`

Requiere rol Admin. Cuerpo: el nuevo valor de orden. Respuestas: `200`, `400`, `401`, `403`, `404`.

Existe como recurso aparte porque reordenar es una operación frecuente y aislada; obligarla a pasar
por la actualización completa haría que un reordenamiento reenviara todos los campos y pudiera
pisar una edición concurrente.

### `DELETE /api/Credits/{id}`

Requiere rol Admin. Baja lógica: conserva el registro y la fotografía.

| Respuesta | Cuándo |
|---|---|
| `204` sin cuerpo | Baja aplicada |
| `401` / `403` / `404` | Sin sesión / rol insuficiente / no existe |

Dar de baja un autor ya inactivo responde `204` y no cambia nada.

### `POST /api/Credits/{id}/foto`

Requiere rol Admin. Cuerpo: formulario con la parte del archivo.

| Respuesta | Cuándo |
|---|---|
| `200` con el autor actualizado | Carga correcta |
| `400` | Contenido binario no corresponde a una imagen admitida, o excede el tamaño máximo |
| `401` / `403` / `404` | Sin sesión / rol insuficiente / el autor no existe |

La validación es **por contenido binario, nunca por extensión ni por tipo declarado**. El
propietario del archivo almacenado es el administrador que realiza la carga. Al reemplazar, el
autor queda apuntando a la imagen nueva.

## Forma de los errores

Todas las respuestas de error usan la misma forma que el resto del sistema y **no incluyen el
mensaje de la excepción ni trazas**. El detalle va al registro de operación con su identificador de
traza, de modo que un incidente se investiga desde ahí y no desde la respuesta al cliente.
