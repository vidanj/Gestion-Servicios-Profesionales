---

description: "Task list for 001-e2e-playwright-mcp"
---

# Tasks: Pruebas automáticas E2E asistidas por IA

**Input**: Design documents from `/specs/001-e2e-playwright-mcp/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Esta feature **es** una suite de pruebas; cada historia se entrega como pruebas E2E.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)

---

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Fijar la versión de `@playwright/mcp` y crear `.mcp.json` en la raíz con el servidor `playwright` (`npx @playwright/mcp@<versión>`); evaluar `npx playwright init-agents` y anotar el resultado en `specs/001-e2e-playwright-mcp/research.md` (R2)
- [ ] T002 [P] Añadir `frontend/e2e-drafts/` a `.gitignore` y crear `frontend/e2e-drafts/.gitkeep` fuera de `testDir`
- [ ] T003 [P] Crear `frontend/tests/README.md` con el procedimiento de [ia-workflow.md](contracts/ia-workflow.md) y la lista de revisión humana (FR-008, FR-009)

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: Ninguna historia puede empezar antes de terminar esta fase.

- [ ] T004 [P] Crear `frontend/tests/fixtures/services.ts` con `ServiceFixture` según data-model.md (`title` requerido "≤ 200", `description` "≤ 1000", `basePrice` "≥ 0.01", `imageUrl` "≤ 2048") y los conjuntos `servicioActivo`, `servicioInactivo`, `catalogoVacio`, `catalogoGrande`
- [ ] T005 [P] Crear `frontend/tests/fixtures/categories.ts` con `CategoryFixture` (`name` "≤ 200")
- [ ] T006 [P] Crear `frontend/tests/fixtures/requests.ts` con un `RequestFixture` por estado (Pending 0, Accepted 1, InProgress 2, Completed 3, Cancelled 4)
- [ ] T007 [P] Crear `frontend/tests/fixtures/session.ts` con sesiones de cliente, profesional (variantes `"Professional"` y `"2"`) y admin, solo con dominios `*.test` (FR-010)
- [ ] T008 Crear `frontend/tests/support/mock-api.ts` con ayudantes `page.route` por recurso según [mock-api.md](contracts/mock-api.md), incluida la ruta comodín `**/api/**` que responde 501 y registra peticiones no simuladas (depende de T004–T006) (FR-002)
- [ ] T009 Crear `frontend/tests/support/session.ts` con `loginAs(page, rol)` usando `page.addInitScript` sobre `token` y `auth-user` (depende de T007)
- [ ] T010 Confirmar contra la API en marcha la serialización de `RequestStatus` y la envoltura de `/api/categories`, `/api/services/my`, `/api/servicerequests/my`, el cuerpo de `PATCH …/toggle` y el código de `DELETE`; corregir `specs/001-e2e-playwright-mcp/contracts/mock-api.md` y los fixtures si difieren (R8)

**Checkpoint**: fixtures, simulación y sesión listos.

---

## Phase 3: User Story 1 - El cliente contrata un servicio desde el catálogo (Priority: P1) 🎯 MVP

**Goal**: Garantía automática del flujo catálogo → detalle → solicitud.

**Independent Test**: `npx playwright test tests/catalogo.spec.ts tests/solicitar-servicio.spec.ts` en verde sin backend.

- [ ] T011 [P] [US1] Añadir `data-testid` (`catalog-grid`, `service-card`, `service-card-title`, `service-card-category`, `service-card-price`, `catalog-empty`) en `frontend/app/dashboard/page.tsx` sin cambiar el comportamiento (FR-004)
- [ ] T012 [P] [US1] Añadir `data-testid` (`service-title`, `request-description`, `request-date`, `request-submit`, `request-success`, `request-error`, `service-unavailable`) en `frontend/app/catalogo/[id]/page.tsx` (FR-004)
- [ ] T013 [US1] Escribir `frontend/tests/catalogo.spec.ts` con los escenarios US1-1 y US1-2 (depende de T008, T009, T011)
- [ ] T014 [US1] Escribir `frontend/tests/solicitar-servicio.spec.ts` con los escenarios US1-3 a US1-6 (depende de T008, T009, T012)
- [ ] T015 [US1] Añadir a `frontend/tests/solicitar-servicio.spec.ts` los casos borde "doble clic no crea dos solicitudes" (contar peticiones `POST`) y "descripción de 1000 caracteres con acentos"

**Checkpoint**: US1 verificable por sí sola.

---

## Phase 4: User Story 2 - El profesional gestiona sus servicios (Priority: P2)

**Goal**: Alta, edición, activación y borrado de servicios, con validaciones.

**Independent Test**: `npx playwright test tests/profesionista.spec.ts` en verde.

- [ ] T016 [P] [US2] Añadir `data-testid` (`my-services-list`, `service-row`, `service-form`, `service-title-input`, `service-category-select`, `service-description-input`, `service-price-input`, `service-submit`, `service-toggle`, `service-edit`, `service-delete`, `field-error`) en `frontend/app/profesionista/page.tsx`
- [ ] T017 [US2] Escribir `frontend/tests/profesionista.spec.ts` con los escenarios US2-1 a US2-6; la validación usa precio `0` y `-1` (regla "≥ 0.01") y título vacío (depende de T008, T009, T016)
- [ ] T018 [US2] Añadir a `frontend/tests/profesionista.spec.ts` el caso borde "cliente en `/profesionista`" y registrar el comportamiento real observado

**Checkpoint**: US1 y US2 funcionan de forma independiente.

---

## Phase 5: User Story 3 - Seguimiento del estado de las solicitudes (Priority: P3)

**Goal**: Transiciones de estado vistas por profesional y cliente.

**Independent Test**: `npx playwright test tests/solicitudes.spec.ts` en verde.

- [ ] T019 [P] [US3] Añadir `data-testid` (`requests-table`, `request-row`, `request-status`, `action-accept`, `action-start`, `action-complete`, `action-cancel`, `status-error`) en `frontend/app/solicitudes/page.tsx`
- [ ] T020 [US3] Escribir `frontend/tests/solicitudes.spec.ts` con los escenarios US3-1 a US3-6, simulando `PUT /api/servicerequests/{id}/status?newStatus={n}` y el `400` de transición inválida (depende de T008, T009, T019)

---

## Phase 6: User Story 4 - Generación y mantenimiento asistidos por IA (Priority: P4)

**Goal**: Procedimiento probado de borrador y diagnóstico con el agente.

**Independent Test**: un borrador generado con el agente pasa tres veces seguidas tras la revisión.

- [ ] T021 [US4] Generar con el agente, siguiendo `frontend/tests/README.md`, el borrador del caso "catálogo con 50 servicios" en `frontend/e2e-drafts/catalogo-grande.spec.ts`; revisarlo con la lista de revisión y moverlo a `frontend/tests/catalogo.spec.ts`; registrar el tiempo en `specs/001-e2e-playwright-mcp/quickstart.md` (SC-005)
- [ ] T022 [US4] Romper a propósito un `data-testid` en una rama local, pedir el diagnóstico al agente según [ia-workflow.md](contracts/ia-workflow.md), anotar el resultado en `frontend/tests/README.md` y revertir el cambio

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T023 Ejecutar `npx playwright test --repeat-each=5` en `frontend/`; corregir cualquier intermitencia en la prueba o el fixture, nunca con `.skip` (FR-003, SC-004)
- [ ] T024 Verificar en la ejecución de `frontend-tests.yml` del PR la duración (< 10 min, SC-003) y que el artefacto `playwright-report` contiene traza y captura de un fallo provocado (FR-005, FR-006, FR-012)
- [ ] T025 Redactar un borrador de issue BUG por cada escenario que falle por comportamiento real (FR-007, SC-006); los issues se abren con confirmación del responsable
- [ ] T026 [P] Añadir a `backend/docs/test-plan.md` los casos CP-E2E-02… ligados a FR-001 y a cada escenario (trazabilidad)
- [ ] T027 Ejecutar `npm run lint` en `frontend/` y resolver avisos en `tests/` sin silenciarlos
- [ ] T028 Ejecutar `/speckit-converge` y cerrar las brechas que reporte

---

## Dependencies & Execution Order

- **Setup (Fase 1)** → **Foundational (Fase 2, bloquea todo)** → US1, US2 y US3 en paralelo → US4 → Polish.
- US4 depende de que exista al menos una suite (US1) para el diagnóstico de T022.
- Dentro de cada historia: `data-testid` → spec.

## Parallel Example: Foundational

```text
Task: "T004 fixtures/services.ts"
Task: "T005 fixtures/categories.ts"
Task: "T006 fixtures/requests.ts"
Task: "T007 fixtures/session.ts"
```

## Implementation Strategy

1. Fases 1 y 2 → 2. US1 (MVP) → **validar con quickstart** → 3. US2 y US3 → 4. US4 → 5. Polish.
