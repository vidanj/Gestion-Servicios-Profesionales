# Investigación Técnica: Spec 010 — Visor de Auditoría

## 1. Comparativa de Estrategias en .NET / PostgreSQL

| Criterio | Triggers en BD (SQL) | Librería Externa (Audit.NET) | Interceptor Nativo EF Core (Seleccionado) |
| :--- | :--- | :--- | :--- |
| **Contexto de Usuario** | Complejo (requiere variables de sesión de PostgreSQL) | Directo vía middleware | Directo mediante `IHttpContextAccessor` |
| **Dependencias Externas** | Ninguna | Paquetes NuGet externos con riesgo de preestreno | Cero dependencias adicionales (nativo de EF Core) |
| **Mantenimiento y Reglas** | Disperso en scripts SQL | Configuración centralizada | Integrado con el ciclo de vida de EF Core y C# |
| **Compatibilidad con CI/CD** | Difícil de probar en xUnit | Puede generar advertencias en análisis estático | Totalmente compatible con la política de cero advertencias |

## 2. Decisiones de Rendimiento e Inmutabilidad
* **Almacenamiento JSONB:** Evita replicar esquemas de tablas relacionales (patrón shadow tables) y reduce la complejidad de migraciones.
* **Política Append-Only:** La tabla `audit_logs` no expone métodos de modificación ni borrado; solo permite inserciones automáticas.