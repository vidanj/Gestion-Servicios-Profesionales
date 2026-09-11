# Specification Analysis Report: 001-e2e-playwright-mcp

> Salida de `/speckit-analyze` (2026-09-11), guardada para el informe. El comando es de solo
> lectura: **no se aplicó ninguna corrección**; la remediación requiere aprobación.
> Prerrequisitos: `check-prerequisites.ps1 -RequireSpec -RequireTasks -IncludeTasks` → OK
> (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `tasks.md`).

## Findings

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| I1 | Inconsistency | MEDIUM | spec.md FR-003, SC-004; contracts/ia-workflow.md; tasks.md T023 | El umbral de determinismo varía: 3 ejecuciones (FR-003, revisión de borradores) frente a 5 (SC-004, T023) | Unificar: 3 por prueba nueva y 5 de la suite completa antes del PR, escrito igual en los cuatro lugares |
| U1 | Underspecification | MEDIUM | spec.md Edge Cases; tasks.md | Los casos borde "respuesta lenta" y "lista con muchas solicitudes" no tienen tarea | Añadir los casos a T015 y T020, o sacarlos del alcance de forma explícita |
| C1 | Coverage | LOW | spec.md FR-011 | FR-011 (un solo comando) no tiene tarea; se cumple con el `npm test` existente | Añadir la verificación a T024 |
| A1 | Ambiguity | LOW | spec.md US1-6 | "Sesión vencida" no dice si se simula con un 401 del servidor o con la ausencia de token | Precisarlo en T014 (401 en `POST /api/servicerequests`) |
| U2 | Underspecification | LOW | spec.md US1-3; tasks.md T014 | El escenario verifica en otra pantalla ("Solicitudes"); T014 no menciona simular `GET /api/servicerequests/my` | Nombrar la ruta simulada en T014 |
| A2 | Ambiguity | LOW | tasks.md T001; contracts/ia-workflow.md | Marcador `<versión>` de `@playwright/mcp` | Intencional: lo resuelve T001 |

## Coverage Summary

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| FR-001 | Sí | T013, T014, T015, T017, T018, T020 | 18 escenarios |
| FR-002 | Sí | T008 | Comodín 501 para rutas no simuladas |
| FR-003 | Sí | T023 | Ver I1 |
| FR-004 | Sí | T011, T012, T016, T019 | |
| FR-005 | Sí | T024 | Workflow existente |
| FR-006 | Sí | T024 | |
| FR-007 | Sí | T025 | |
| FR-008 | Sí | T001, T003, T021 | |
| FR-009 | Sí | T002, T003, T021 | |
| FR-010 | Sí | T004–T007 | |
| FR-011 | **No** | — | C1 |
| FR-012 | Sí | T024 | |
| SC-002 | Sí | T011, T012, T016, T019 | 11 → 15 pantallas |
| SC-003 | Sí | T024 | |
| SC-004 | Sí | T023 | |
| SC-005 | Sí | T021 | |

**Constitution Alignment Issues:** ninguno. Playwright es la herramienta autorizada (V), las pruebas
usan respuestas simuladas (V) y no se añaden librerías a la aplicación (VII).

**Unmapped Tasks:** ninguna. T026 (trazabilidad en `test-plan.md`), T027 (lint, Principio VI) y T028
(`converge`) son transversales.

## Metrics

- Total Requirements: 12 FR (+ 5 SC con trabajo construible)
- Total Tasks: 28
- Coverage % (FR con ≥ 1 tarea): **92 %** (11/12)
- Ambiguity Count: 2
- Duplication Count: 0
- Critical Issues Count: **0**

## Next Actions

- Sin hallazgos CRITICAL: se puede pasar a `/speckit-implement` con la aprobación del responsable.
- Antes, conviene resolver I1 y U1 (edición manual de `spec.md` y `tasks.md`) para llegar al 100 %
  de cobertura que fija la guía.
