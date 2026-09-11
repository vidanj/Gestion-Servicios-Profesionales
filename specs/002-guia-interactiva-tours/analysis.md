# Specification Analysis Report: 002-guia-interactiva-tours

> Salida de `/speckit-analyze` (2026-09-11), guardada para el informe. El comando es de solo
> lectura: **no se aplicó ninguna corrección**; la remediación requiere aprobación.
> Prerrequisitos: `check-prerequisites.ps1 -RequireSpec -RequireTasks -IncludeTasks` → OK.

## Findings

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| U1 | Underspecification | **HIGH** | spec.md US2, US3; research.md R5–R6; data-model.md (catálogo); tasks.md T010, T017, T020 | El recorrido inicia en la primera página autenticada (hoy `/dashboard`) y omite los pasos sin elemento. Con eso, el recorrido del **profesional** pierde `servicio-formulario` y `servicio-lista`, y el del **administrador** pierde `usuarios-tabla` y `admin-*`: en el inicio automático solo quedarían los pasos de la navegación. No se cumple US2 ("le muestra dónde publicar un servicio, cómo activarlo…") | Iniciar cada recorrido en la página principal de su rol (profesional → `/profesionista`, admin → `/usuarios`), o hacer que el primer paso lleve allí. Actualizar R6, data-model, T010 y las pruebas T017 y T020 |
| C1 | Coverage | LOW | spec.md FR-005; tasks.md T010–T011 | Ninguna tarea dice explícitamente que se muestre el progreso (opción de la librería) | Añadirlo a T010 |
| C2 | Coverage | LOW | spec.md FR-011; tasks.md T014 | Nada comprueba que el recorrido no dispara peticiones ni acciones | En T014, afirmar que no hubo peticiones a la API durante el recorrido |
| C3 | Coverage | LOW | spec.md FR-010; tasks.md T026 | El modo oscuro solo se valida a mano | Aceptable: la validación visual es manual por regla del proyecto |
| T1 | Terminology | LOW | spec.md, plan.md, tasks.md | "recorrido", "tour" y "guía" se usan como sinónimos | Glosario: "recorrido" en el spec, "Ver guía" en la UI, `tour` en el código |
| S1 | Security/Privacy | LOW | data-model.md (clave de estado) | La clave de `localStorage` incluye el correo del usuario | Ya se guarda en `auth-user`; opcionalmente usar un hash |
| A1 | Ambiguity | LOW | tasks.md T001 | Marcador `<x.y.z>` de la versión de `driver.js` | Intencional: lo resuelve T001 |

## Coverage Summary

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| FR-001 | Sí | T009, T015, T018 | Ver U1 |
| FR-002 | Sí | T010, T014 | |
| FR-003 | Sí | T010, T014 | |
| FR-004 | Sí | T009, T015, T018 | |
| FR-005 | Parcial | T014 | C1 |
| FR-006 | Sí | T010, T017 | |
| FR-007 | Sí | T011, T021 | |
| FR-008 | Sí | T005, T021 | |
| FR-009 | Sí | T022 | |
| FR-010 | Sí | T006, T026 | C3 |
| FR-011 | Parcial | T014 | C2 |
| FR-012 | Sí | T009 | |
| FR-013 | Sí | T014, T017, T020, T021, T023 | |
| FR-014 | Sí | T025 | |
| SC-002 | Sí | T009, T015, T018, T026 | |
| SC-003 | Sí | T014, T021 | |
| SC-004 | Sí | T022 | |

**Constitution Alignment Issues:** ninguno. `driver.js` está amparado por la excepción v1.1.0 del
Principio VII, y sus condiciones (solo en `src/features/tours/`, carga en cliente y tokens del tema)
están cubiertas por T002, T011, T006 y T025.

**Unmapped Tasks:** ninguna.

## Metrics

- Total Requirements: 14 FR (+ 3 SC con trabajo construible)
- Total Tasks: 27
- Coverage % (FR con ≥ 1 tarea): **100 %** (2 parciales)
- Ambiguity Count: 1
- Duplication Count: 0
- Critical Issues Count: **0** (1 HIGH)

## Next Actions

- Sin CRITICAL, pero **U1 (HIGH) debe resolverse antes de `/speckit-implement`**: afecta al valor de
  US2 y US3. Sugerido: `/speckit-plan` para ajustar R6 y regenerar `tasks.md`.
