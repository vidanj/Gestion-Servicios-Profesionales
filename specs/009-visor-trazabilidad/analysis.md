# Análisis — Spec 009

> Reporte de consistencia entre [spec.md](spec.md), [plan.md](plan.md) y [tasks.md](tasks.md), y de
> cumplimiento de la [constitución](../../.specify/memory/constitution.md) v1.1.0, con la estructura
> de `/speckit-analyze`.
> **Fecha:** 2026-09-24 · **Estado:** 6 hallazgos: 3 medios con mitigación definida y 3 bajos.
> **Sin hallazgos CRITICAL: la implementación puede proceder una vez aprobada la planeación.**

---

## 1. Cobertura de requisitos

Los 34 requisitos funcionales tienen al menos una tarea asignada.

| Grupo | Requisitos | Tareas | Con verificación automatizada |
|---|---|---|---|
| US1 — Registros | FR-001 … FR-009 | T002, T006–T010 | FR-003, FR-004, FR-005 (T022) |
| US2 — Trazas | FR-010 … FR-015 | T011–T015 | FR-010 (T022, por la resolución de identificadores) |
| US3 — Correlación y tablero | FR-016 … FR-021 | T016–T019, T030 | FR-018 (T025); la correlación subyacente, por T022 |
| US4 — Operación y verificación | FR-022 … FR-028 | T020–T027 | Todos: son la verificación |
| Transversales | FR-029 … FR-034 | T001, T003, T004, T008, T012, T024, T028, T029, T031 | FR-030 (comprobación de etiquetas móviles ya existente en el flujo) |

**Sin verificación automatizada, por naturaleza:** los enlaces de la interfaz de Grafana (FR-016,
FR-017, FR-019). Un clic no se prueba sin automatizar un navegador contra Grafana, y eso añadiría una
dependencia desproporcionada. Se compensa en dos niveles: T022 prueba automáticamente que los
identificadores **coinciden** entre almacenes —que es lo que puede fallar en silencio—, y T019 y T030
prueban los enlaces a mano, con resultado anotado en el PR.

Los siete criterios de éxito tienen forma de medirse: SC-002, SC-004 y SC-005 en T022; SC-001 y
SC-003 en T030; SC-006 en T031; SC-007 en T026.

## 2. Hallazgos

### A1 — MEDIO · La spec depende de dos PR sin fusionar

La spec cita la 008 (que vive en la rama del PR #252) y modifica archivos que crea el PR #253. Mientras
ninguno esté en `dev`, los enlaces a `specs/008-…` no resuelven y el diff de este PR incluye los
cambios de #253.

**Mitigación:** acordada con el responsable. El PR apunta a `dev` y no se fusiona hasta que #252 y
#253 lo estén. Tras su fusión, el diff queda limitado a esta spec.

### A2 — MEDIO · El socket de Docker da control del anfitrión

Montarlo en solo lectura no limita las operaciones de la API de Docker (research, D9).

**Mitigación:** riesgo aceptado para un entorno que solo corre en el equipo de quien lo levanta y en
ejecutores efímeros de integración continua. Imagen oficial con versión fijada, sin puerto de control
publicado, y el riesgo escrito en el README del entorno (FR-032). El proxy de solo lectura queda
anotado como paso previo a alojar el entorno fuera.

### A3 — MEDIO · Asimetría entre registros y trazas fuera de contenedor

Con la API en `dotnet run`, las trazas llegan al visor y los registros no. Quien trabaja así podría
concluir que la recolección de registros está rota.

**Mitigación:** documentado como caso límite en el spec, en el quickstart y en el README raíz (T029).
La verificación se ejecuta siempre con la API en contenedor, que es el caso soportado.

### A4 — BAJO · Nombres de campos del registro por confirmar

El modelo de datos describe el formato compacto de Serilog a partir de su documentación, no a partir de una línea
capturada de esta API.

**Mitigación:** T002 lo confirma antes de configurar el recolector y corrige el modelo si difiere.

### A5 — BAJO · Configuración de Tempo 3.0 distinta de la mayoría de guías

La mayoría de los ejemplos disponibles son de la rama 2.x (research, D5).

**Mitigación:** T011 se escribe contra la documentación de la 3.0 y T024 valida la configuración en
integración continua.

### A6 — BAJO · Registros perdidos si Loki cae mucho tiempo

**Mitigación:** aceptado y declarado en el spec (§5). El visor es diagnóstico; la evidencia de
acciones de usuario es `UserLog`, que no depende de este entorno.

## 3. Cumplimiento de la constitución

| Principio | Resultado |
|---|---|
| I — Especificación antes que código | Cumple: planeación en PR propio, previo al de implementación |
| II, III, IV | No aplican: sin cambios de backend, endpoints ni esquema |
| V — Pruebas honestas | Cumple: la verificación de extremo a extremo falla si la correlación no funciona, y T027 comprueba que la verificación también detecta fallos |
| VI — Calidad sin silenciadores | Cumple: sin dependencias nuevas en el backend y configuración validada en integración continua |
| VII — Stack del frontend | No aplica: sin cambios al frontend |
| Seguridad y Datos | Cumple, con el riesgo A2 documentado |
| Trazabilidad SDD | Cumple: cada tarea referencia su historia y sus requisitos |

## 4. Antes de implementar

1. Aprobación explícita del responsable sobre esta planeación (Principio I).
2. Fusión de #252 y #253 en `dev`, o acuerdo de implementar sobre `ci/251-monitoreo-y-despliegue`
   sabiendo que el PR de implementación heredará la misma condición.
3. Si el equipo ejecuta `/speckit-analyze` sobre estos artefactos, su salida reemplaza este reporte.
