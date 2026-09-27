# Plan de Arquitectura: Spec 010 — Visor de Auditoría

## 1. Decisiones de Arquitectura

### 1.1 Dónde vive la captura: Interceptor de EF Core
Se implementa un `SaveChangesInterceptor` (`AuditSaveChangesInterceptor`) nativo en Entity Framework Core. Opera a nivel de `ChangeTracker` detectando entidades en estado `Added`, `Modified` o `Deleted` antes de persistir la transacción.

### 1.2 Formato de almacenamiento: JSONB en PostgreSQL
Se utilizan columnas `jsonb` en PostgreSQL para `old_values`, `new_values` y `changed_columns`. Esto permite almacenar esquemas dinámicos de cualquier entidad sin crear tablas de réplica rígidas ni requerir migraciones continuas ante cambios de modelos.

### 1.3 Correlación cruzada con Trazabilidad (Spec 008 y Spec 009)
Se incluye la columna `trace_id` en la tabla de auditoría. Permite conectar el circuito completo del caso de estudio:
* **Punto 1 (Métricas):** Alerta ante anomalías de tráfico o errores.
* **Punto 2 (Trazabilidad):** Consulta de logs y trazas mediante el `TraceId`.
* **Punto 3 (Auditoría):** Identificación exacta de qué usuario y qué mutación causó el cambio en la base de datos bajo ese mismo `TraceId`.

## 2. Modelo de Datos Relacional (PostgreSQL)

```sql
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NULL,
    user_email VARCHAR(256) NULL,
    action VARCHAR(32) NOT NULL, -- CREATE, UPDATE, DELETE
    entity_name VARCHAR(128) NOT NULL,
    entity_id VARCHAR(128) NOT NULL,
    old_values JSONB NULL,
    new_values JSONB NULL,
    changed_columns JSONB NULL,
    ip_address VARCHAR(64) NULL,
    trace_id VARCHAR(128) NULL,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX ix_audit_logs_user_id ON audit_logs(user_id);
CREATE INDEX ix_audit_logs_created_at ON audit_logs(created_at_utc DESC);
CREATE INDEX ix_audit_logs_trace_id ON audit_logs(trace_id);