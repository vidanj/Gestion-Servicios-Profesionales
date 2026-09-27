# Tareas de Implementación: Spec 010 — Visor de Auditoría

## Fase 1: Persistencia y Modelo de Datos
- [ ] T001: Crear la entidad AuditLog.cs en SistemaServicios.API/Data/Entities/ con campos JSONB.
- [ ] T002: Mapear AuditLog en AppDbContext.cs configurando tipos de columna jsonb e índices.
- [ ] T003: Crear la migración de Entity Framework Core AddAuditLogsTable.

## Fase 2: Interceptor y Captura Automática
- [ ] T004: Diseñar interfaz ICurrentUserService para extraer UserId, Email e IpAddress desde IHttpContextAccessor.
- [ ] T005: Implementar AuditSaveChangesInterceptor para capturar estados Added, Modified y Deleted.
- [ ] T006: Redactar campos sensibles (hashes de contraseñas, tokens) para excluirlos del JSON de auditoría.
- [ ] T007: Extraer el TraceId activo de OpenTelemetry para correlación.
- [ ] T008: Registrar el interceptor en el contenedor de dependencias en Program.cs.

## Fase 3: Endpoints Administrativos
- [ ] T009: Crear DTOs de lectura (AuditLogSummaryDto, AuditLogDetailDto, AuditLogFilterDto).
- [ ] T010: Implementar AuditLogsController.cs con directiva [Authorize(Roles = "Administrador")].
- [ ] T011: Agregar soporte de paginación y ordenamiento descendente por fecha.

## Fase 4: Pruebas Automatizadas
- [ ] T012: Pruebas unitarias para validar registro automático de mutaciones en base de datos.
- [ ] T013: Validar que contraseñas y tokens no aparezcan en old_values ni new_values.
- [ ] T014: Validar restricción de acceso (403 Forbidden para usuarios no administradores).