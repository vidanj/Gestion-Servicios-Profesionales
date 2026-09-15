# Quickstart — Spec 007, módulo de créditos de autores

> Cómo validar el módulo de punta a punta una vez implementado. Hasta entonces, este documento es
> el guion de aceptación acordado en planeación: describe lo que un revisor debe poder hacer.

---

## Preparación

1. Levantar la base de datos del proyecto y aplicar las migraciones.
2. Levantar el backend con la configuración local habitual.
3. Tener a la mano dos sesiones: una de rol administrador y otra de un rol distinto.

## Validación de US1 — consulta pública

**Sin ninguna sesión iniciada**, solicitar el recurso público de créditos.

| Qué debe verse | Requisito |
|---|---|
| Respuesta con estado de éxito, sin pedir credenciales | FR-006 |
| Un título y un mensaje provenientes de la configuración | FR-001 |
| Una lista de autores ordenada de forma ascendente por su orden de presentación | FR-003 |
| Ningún autor marcado como inactivo | FR-002 |
| Los autores sin fotografía aparecen con su referencia vacía, no con error | FR-005 |

**Prueba del orden determinista:** registrar dos autores con el mismo valor de orden y repetir la
consulta varias veces. El orden relativo entre ambos debe ser siempre el mismo.

**Prueba de lista vacía:** dar de baja a todos los autores y repetir la consulta. Debe responder
con éxito, con el título y el mensaje presentes y la lista vacía (FR-007).

## Validación de US2 — administración

**Con sesión de administrador:**

1. Registrar un autor con nombre y rol válidos. Debe crearse activo y quedar al final de la lista.
2. Actualizar su semblanza. La consulta pública debe reflejar el cambio.
3. Cambiar su orden de presentación. La consulta pública debe reordenar.
4. Darlo de baja. Debe desaparecer de la consulta pública, y consultarlo por su identificador desde
   la sesión de administrador debe seguir devolviéndolo.
5. Darlo de baja otra vez. Debe responder igual, sin error.

**Con sesión de un rol distinto de administrador:** intentar cada una de las operaciones
anteriores. Todas deben responder con acceso denegado (SC-004).

**Validaciones a provocar deliberadamente:**

- Nombre vacío o solo espacios.
- Nombre que excede el máximo.
- Orden negativo.
- Enlace de perfil con un esquema distinto del seguro.

En los cuatro casos la respuesta debe indicar el campo inválido **sin incluir el mensaje de la
excepción** (FR-020). Conviene revisar el cuerpo de la respuesta con atención: este es un defecto
que el proyecto ya arrastra en otros controladores.

## Validación de US3 — fotografía

**Con sesión de administrador:**

1. Subir una imagen válida para un autor. La consulta pública debe devolver su ruta de fotografía.
2. Abrir esa ruta directamente, sin sesión. La imagen debe descargarse (FR-018).
3. Tomar un archivo que no sea imagen, renombrarlo con extensión de imagen y subirlo. Debe
   rechazarse, y no debe quedar ningún archivo almacenado (FR-016).
4. Subir un archivo que exceda el tamaño máximo. Debe rechazarse (FR-017).
5. Subir una imagen distinta para el mismo autor. La consulta pública debe devolver la nueva
   (FR-019).

## Verificación final

| Comprobación | Criterio |
|---|---|
| El archivo de proyecto del backend no cambió | SC-005 |
| Todas las pruebas unitarias y de integración del módulo pasan | SC-002 |
| La integración continua está en verde | — |
| El inventario de recursos del proyecto incluye el módulo | T036, T037 |
| La verificación de convergencia no reporta diferencias | T040 |
