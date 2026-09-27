# Especificación 010: Visor de Auditoría (Bitácora de Base de Datos)

## 1. Propósito y Alcance
Esta especificación define el diseño e implementación del **Visor de Auditoría (Punto 3 del caso de estudio)** para la plataforma *Gestión de Servicios Profesionales*.
A diferencia del monitoreo de infraestructura (spec 008) y de las trazas de diagnóstico técnico (spec 009), la auditoría se enfoca en la **actividad de los usuarios sobre las entidades de negocio**, registrando quién ejecutó cada acción, en qué momento, qué valores existían previamente y cuáles fueron los valores resultantes, con capacidad de consulta administrativa e inspección para reversión de estados.

## 2. Historias de Usuario
* **US-001 (Auditoría automática de mutaciones):** Como sistema, requiero interceptar de forma transparente cada creación, actualización y eliminación de entidades de negocio para persistir un registro histórico inmutable sin acoplar la lógica de los controladores.
* **US-002 (Consulta y filtrado administrativo):** Como Administrador, requiero consultar la bitácora con filtros por fecha, usuario, entidad y tipo de operación para inspeccionar acciones críticas en la plataforma.
* **US-003 (Inspección de diferencias de estado):** Como Administrador, requiero ver el detalle de los valores previos (`OldValues`) y nuevos (`NewValues`) de un registro para comprender el impacto exacto de un cambio.
* **US-004 (Inmutabilidad y preservación):** Como oficial de seguridad/auditoría, requiero que los registros de la bitácora no puedan ser alterados ni eliminados por ningún rol de usuario.
* **US-005 (Trazabilidad y correlación):** Como equipo de soporte, requiero que cada registro de auditoría guarde el `TraceId` de la petición HTTP para poder vincular una acción de negocio con los registros de Serilog y las trazas de Tempo.

## 3. Requerimientos Funcionales
* **FR-001:** El sistema debe registrar en la tabla `audit_logs` toda operación de creación (`CREATE`), modificación (`UPDATE`) y baja lógica o física (`DELETE`) en las entidades `Users`, `Services`, `ServiceRequests` y `Ratings`.
* **FR-002:** El registro debe capturar el identificador del usuario autenticado a partir de las claims del token JWT (`UserId`, `Email`). Si la operación ocurre en un contexto no autenticado (ej. registro público), se registrará como `Sistema / Anónimo`.
* **FR-003:** Las modificaciones (`UPDATE`) deben almacenar una instantánea serializada en JSON de los campos antes del cambio (`OldValues`) y después del cambio (`NewValues`), junto con la lista de propiedades afectadas (`ChangedColumns`).
* **FR-004:** La captura de auditoría debe implementarse mediante un interceptor de Entity Framework Core (`AuditSaveChangesInterceptor`) en la capa de persistencia para garantizar cobertura total sin intervención manual en los servicios de aplicación.
* **FR-005:** Los secretos de seguridad (contraseñas en texto plano, hashes de contraseña, tokens de restablecimiento) deben redactarse o excluirse estrictamente del payload de auditoría.
* **FR-006:** Se expondrá un endpoint administrativo `GET /api/AuditLogs` con paginación obligatoria y filtros opcionales por rango de fechas (`from`, `to`), identificador de usuario (`userId`), nombre de entidad (`entityName`) y acción (`action`).
* **FR-007:** La tabla de auditoría debe ser de solo anexado (*append-only*); la aplicación no dispondrá de endpoints ni permisos de actualización o eliminación sobre ella.

## 4. Criterios de Aceptación
* **SC-001:** Al crear, editar o eliminar una entidad auditada, se genera un registro en `audit_logs` dentro de la misma transacción de base de datos.
* **SC-002:** Un `UPDATE` sobre el perfil o estado de una solicitud guarda exactamente los valores anteriores y nuevos en formato JSON estructurado.
* **SC-003:** El endpoint `GET /api/AuditLogs` responde `403 Forbidden` a usuarios no autenticados o con rol distinto de `Administrador`.
* **SC-004:** En el registro de auditoría queda grabado el `TraceId` activo de OpenTelemetry correspondiente a la solicitud HTTP.
* **SC-005:** Ningún registro de auditoría contiene contraseñas ni tokens sensibles en sus campos JSON.
