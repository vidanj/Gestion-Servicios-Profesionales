# Spec 007 — Módulo de créditos de autores

> **Estado:** planeación (sin implementar) · **Fecha:** 2026-09-11 · **Rama:** `docs/228-spec-007-planeación-del-módulo-de-créditos-de-autores`
>
> **Origen:** necesidad nueva del producto (issue #228). No proviene de una brecha de la
> [ingeniería inversa](../../docs/sdd/estado-actual/ingenieria-inversa.md).
>
> **Relacionados:** [plan](plan.md) · [research](research.md) · [modelo de datos](data-model.md) ·
> [contrato](contracts/credits-api.md) · [quickstart](quickstart.md) · [tareas](tasks.md) ·
> [análisis](analysis.md) · [constitución](../../.specify/memory/constitution.md)

---

## 1. Contexto y valor

El sistema no reconoce en ninguna parte a las personas que lo construyeron. La página `/about`
del frontend existe pero sirve contenido estático escrito a mano, sin relación con el equipo real
del proyecto.

Este módulo expone, desde el backend, la información de los autores del sistema: nombre, rol en el
proyecto, semblanza, foto y enlace público, junto con un mensaje del equipo. Es un módulo de
lectura para cualquier visitante y de mantenimiento para el rol Admin.

**Por qué ahora:** es un módulo acotado, sin dependencias con la lógica de negocio, que sirve de
primer ejercicio completo del ciclo SDD sobre una capacidad nueva (los pilotos 001–003 son
capacidades transversales, no módulos de producto).

## 2. Historias de usuario

### US1 — Consultar los créditos del sistema (P1, MVP)

**Como** visitante del sitio, **quiero** ver quiénes construyeron el sistema, **para** conocer al
equipo responsable.

Esta historia es entregable por sí sola: con ella el módulo ya cumple su propósito principal.

**Escenario 1 — Consulta con datos cargados**

```
DADO que existen tres autores activos registrados
CUANDO un visitante sin sesión solicita los créditos
ENTONCES el sistema devuelve el título, el mensaje del equipo y los tres autores
Y cada autor incluye su nombre, su rol y su orden de presentación
```

**Escenario 2 — Autor sin foto**

```
DADO que existe un autor activo sin foto asociada
CUANDO un visitante solicita los créditos
ENTONCES el autor aparece en la lista con su referencia de foto vacía
Y la respuesta no produce error
```

**Escenario 3 — Autor dado de baja**

```
DADO que existe un autor marcado como inactivo
CUANDO un visitante solicita los créditos
ENTONCES ese autor no aparece en la lista
```

**Escenario 4 — Sin autores registrados**

```
DADO que no existe ningún autor activo
CUANDO un visitante solicita los créditos
ENTONCES el sistema devuelve el título y el mensaje con una lista vacía
Y responde con estado 200, no con error
```

### US2 — Mantener los créditos (P2)

**Como** administrador, **quiero** dar de alta, editar y dar de baja autores, **para** mantener
los créditos al día sin desplegar la aplicación.

**Escenario 1 — Alta**

```
DADO que un administrador tiene sesión iniciada
CUANDO registra un autor con nombre y rol válidos
ENTONCES el sistema lo crea y lo devuelve con su identificador
Y el autor queda activo y al final del orden de presentación
```

**Escenario 2 — Acceso denegado**

```
DADO que un usuario con rol Client tiene sesión iniciada
CUANDO intenta registrar un autor
ENTONCES el sistema rechaza la operación por falta de permisos
```

**Escenario 3 — Baja lógica**

```
DADO que existe un autor activo con foto asociada
CUANDO un administrador lo da de baja
ENTONCES el autor deja de aparecer en la consulta pública
Y su registro y su foto se conservan en la base de datos
```

**Escenario 4 — Validación de longitud**

```
DADO que un administrador tiene sesión iniciada
CUANDO registra un autor con un nombre que excede el máximo permitido
ENTONCES el sistema rechaza la operación indicando el campo inválido
Y no revela detalle interno de la excepción
```

### US3 — Asociar la foto de un autor (P3)

**Como** administrador, **quiero** subir la fotografía de un autor, **para** que aparezca junto a
su información.

**Escenario 1 — Carga válida**

```
DADO que existe un autor activo sin foto
CUANDO un administrador sube una imagen con formato admitido
ENTONCES el sistema la almacena y la asocia al autor
Y la consulta pública devuelve la referencia de esa imagen
```

**Escenario 2 — Archivo falsificado**

```
DADO que un administrador tiene sesión iniciada
CUANDO sube un archivo cuya extensión dice imagen pero cuyo contenido no lo es
ENTONCES el sistema rechaza la carga
Y no almacena el archivo
```

**Escenario 3 — Reemplazo**

```
DADO que un autor ya tiene una foto asociada
CUANDO un administrador sube una foto nueva para ese autor
ENTONCES el autor queda asociado a la imagen nueva
```

## 3. Requisitos funcionales

### Consulta pública (US1)

| ID | Requisito |
|---|---|
| FR-001 | El sistema DEBE exponer un recurso público que devuelva el título de la sección, el mensaje del equipo y la lista de autores. |
| FR-002 | La lista DEBE incluir únicamente autores activos. |
| FR-003 | La lista DEBE devolverse en un orden determinista definido por un campo de presentación; ante empate, DEBE desempatar por identificador. |
| FR-004 | Cada autor DEBE incluir nombre completo, rol en el proyecto, semblanza opcional, enlace público opcional y referencia a su foto cuando exista. |
| FR-005 | Cuando un autor no tenga foto, el sistema DEBE devolverlo con la referencia vacía y sin error. |
| FR-006 | El recurso de consulta DEBE responder sin requerir autenticación. |
| FR-007 | Cuando no existan autores activos, el sistema DEBE responder con éxito y una lista vacía. |

### Administración (US2)

| ID | Requisito |
|---|---|
| FR-008 | El sistema DEBE permitir al rol Admin registrar un autor. |
| FR-009 | El sistema DEBE permitir al rol Admin actualizar los datos de un autor existente. |
| FR-010 | El sistema DEBE permitir al rol Admin dar de baja un autor mediante baja lógica, conservando el registro. |
| FR-011 | El sistema DEBE rechazar cualquier operación de escritura solicitada por un rol distinto de Admin. |
| FR-012 | El sistema DEBE validar las longitudes máximas de cada campo antes de persistir. |
| FR-013 | El sistema DEBE permitir al rol Admin modificar el orden de presentación de un autor. |
| FR-014 | Un autor recién creado DEBE quedar activo y al final del orden de presentación. |

### Fotografía (US3)

| ID | Requisito |
|---|---|
| FR-015 | El sistema DEBE permitir al rol Admin asociar una imagen a un autor. |
| FR-016 | El sistema DEBE validar el contenido binario del archivo, no su extensión, antes de almacenarlo. |
| FR-017 | El sistema DEBE rechazar archivos que excedan el tamaño máximo admitido. |
| FR-018 | La imagen almacenada DEBE poder recuperarse por el recurso público de archivos ya existente. |
| FR-019 | Al asociar una imagen nueva a un autor que ya tenía una, el sistema DEBE dejar vigente la más reciente. |

### Transversales

| ID | Requisito |
|---|---|
| FR-020 | Las respuestas de error NO DEBEN incluir el mensaje de la excepción ni trazas. |
| FR-021 | El módulo NO DEBE introducir dependencias externas nuevas. |
| FR-022 | Toda consulta de lectura pública DEBE resolverse sin exponer identificadores internos de otros usuarios del sistema. |

## 4. Criterios de éxito

| ID | Criterio | Cómo se mide |
|---|---|---|
| SC-001 | La consulta pública responde en menos de 500 ms en entorno local con hasta 20 autores | Medición manual o con la instrumentación de carga del proyecto |
| SC-002 | Cada requisito funcional tiene al menos un escenario automatizado | `analysis.md`, sección de cobertura |
| SC-003 | Un visitante sin sesión obtiene los créditos completos sin ningún 401 ni 403 | Prueba de integración |
| SC-004 | Un usuario con rol Client o Professional recibe 403 en todas las operaciones de escritura | Prueba de integración |
| SC-005 | El módulo no agrega paquetes al archivo de proyecto del backend | Revisión del diff del PR |
| SC-006 | La cobertura de línea del servicio del módulo no baja del umbral vigente del proyecto | Reporte de ReportGenerator en CI |

## 5. Clarificaciones resueltas

| # | Pregunta | Resolución | Razón |
|---|---|---|---|
| C1 | ¿Los autores se persisten o son contenido estático? | En base de datos, con migración | Permite mantenimiento sin desplegar y ejercita la arquitectura por capas vigente |
| C2 | ¿Dónde vive el mensaje del equipo? | En configuración de la aplicación | Es un texto único; una tabla de una sola fila no se justifica |
| C3 | ¿Cómo se almacenan las fotos? | Reutilizando el almacenamiento de archivos existente | Ya resuelve validación binaria, persistencia y entrega pública |
| C4 | ¿Las acciones de administración se registran en la bitácora? | No en esta iteración | Exigiría ampliar un enumerado transversal usado por otros módulos |
| C5 | ¿Hay interfaz de usuario? | No | El alcance es backend; el contrato queda listo para alimentar `/about` |
| C6 | ¿Qué número de spec se usa? | 007 | Los números 004 a 006 están reservados a los módulos de negocio propuestos |

## 6. Supuestos

- Los autores del sistema no necesariamente son usuarios registrados de la aplicación; la entidad
  es independiente de `Users`.
- El número de autores se mantiene en el orden de decenas, no de miles; no se requiere paginación.
- La sección de créditos es única: no hay variantes por idioma ni por ambiente.
- El administrador que sube una fotografía es el propietario del archivo almacenado.

## 7. Fuera de alcance

| Tema | Por qué queda fuera |
|---|---|
| Interfaz de usuario y pruebas E2E | El alcance acordado es backend |
| Registro en la bitácora de acciones | Requiere ampliar el enumerado `LogAction`, cambio transversal (ver C4) |
| Internacionalización del mensaje y las semblanzas | No hay requisito de multiidioma en el producto |
| Historial de versiones de los créditos | Sin caso de uso |
| Carga masiva de autores | El volumen esperado no lo justifica |
| Edición del mensaje del equipo desde la interfaz | Vive en configuración; cambiarlo requiere despliegue (ver `research.md`, D3) |

## 8. Dependencias

| Depende de | Estado |
|---|---|
| Almacenamiento de archivos (`IFileStorage`, `StoredFiles`) | Existente y en uso |
| Recurso público de descarga de archivos | Existente |
| Validación de imágenes por contenido binario | Existente, reutilizable |
| Política de autorización por rol | Existente |
