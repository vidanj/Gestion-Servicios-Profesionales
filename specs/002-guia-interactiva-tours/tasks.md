---

description: "Task list for 002-guia-interactiva-tours"
---

# Tasks: Guía interactiva de producto por rol

**Input**: Design documents from `/specs/002-guia-interactiva-tours/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Solicitadas por el spec (FR-013) y exigidas por la constitución (Principio V): E2E con Playwright.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Instalar `driver.js` con versión exacta en `frontend/` (`npm install driver.js@<x.y.z> --save-exact`) → `frontend/package.json`, `frontend/package-lock.json` (R1; excepción de la constitución v1.1.0)
- [ ] T002 [P] Crear la estructura `frontend/src/features/tours/{domain,application,infrastructure,presentation}/`

---

## Phase 2: Foundational (Blocking Prerequisites)

- [ ] T003 [P] Crear `frontend/src/features/tours/domain/tour.model.ts` con `Tour`, `TourStep` (`title` "1 a 60 caracteres", `description` "1 a 200 caracteres", `side` opcional), `TourRole` y `TourResult` ("completed" | "dismissed")
- [ ] T004 [P] Crear `frontend/src/features/tours/application/normalize-role.ts` que acepte "Client"/"Professional"/"Admin" y "1"/"2"/"0" (R7)
- [ ] T005 Crear `frontend/src/features/tours/infrastructure/tour-state.repository.ts` con la clave `gsp-tour:<tourId>:v<version>:<usuario>`, lectura y escritura en `try/catch` y respaldo en memoria (R3, FR-008)
- [ ] T006 [P] Crear `frontend/src/features/tours/presentation/tour-theme.css` con variables CSS del tema de Chakra, incluido el modo oscuro (FR-010)
- [ ] T007 [P] Añadir `data-tour` (`nav-catalogo`, `nav-mis-servicios`, `nav-solicitudes`, `nav-administracion`, `nav-perfil`) en `frontend/src/components/nav/nav.tsx` según [tour-ui-contract.md](contracts/tour-ui-contract.md)
- [ ] T008 Crear o reutilizar (si el piloto 001 ya lo creó) `frontend/tests/support/session.ts` con `loginAs(page, rol)`, incluidas las dos formas del rol

**Checkpoint**: modelo, estado, estilos y anclajes de navegación listos.

---

## Phase 3: User Story 1 - Recorrido de bienvenida del cliente (Priority: P1) 🎯 MVP

**Goal**: El cliente recibe su recorrido la primera vez y no se repite.

**Independent Test**: login como cliente en navegador limpio → recorrido automático → recarga sin recorrido.

- [ ] T009 [US1] Crear `frontend/src/features/tours/domain/tours.catalog.ts` con el recorrido `cliente` v1 (6 pasos de data-model.md; descripciones ≤ 200 caracteres, en español) (FR-004, FR-012)
- [ ] T010 [US1] Crear `frontend/src/features/tours/application/use-tour-controller.ts`: `hasPending`, `start({ auto })`, filtro de pasos sin elemento (R5), sin `element` en viewports < 768 px (R8), registro `completed`/`dismissed` en `onDestroyed` y cierre como `dismissed` al cambiar de ruta (FR-002, FR-003, FR-006)
- [ ] T011 [US1] Crear `frontend/src/features/tours/presentation/tour-launcher.tsx` (`"use client"`): botón "Ver guía" con `data-tour="nav-ver-guia"`, `import()` dinámico de `driver.js` y su CSS, inicio automático si `hasPending` (R2, FR-007)
- [ ] T012 [US1] Montar `<TourLauncher />` en `frontend/src/components/nav/nav.tsx`
- [ ] T013 [P] [US1] Añadir `data-tour` (`catalogo-lista`, `catalogo-tarjeta`) en `frontend/app/dashboard/page.tsx`
- [ ] T014 [US1] Escribir `frontend/tests/tours.spec.ts` con los escenarios US1-1 a US1-4: inicio automático, avanzar/retroceder con progreso, Esc = omitido sin cambios en la pantalla, sin reinicio al recargar (FR-013)

**Checkpoint**: MVP entregable.

---

## Phase 4: User Story 2 - Recorrido del profesional (Priority: P2)

- [ ] T015 [US2] Añadir el recorrido `profesional` v1 a `frontend/src/features/tours/domain/tours.catalog.ts`
- [ ] T016 [P] [US2] Añadir `data-tour` (`servicio-formulario`, `servicio-lista`) en `frontend/app/profesionista/page.tsx`
- [ ] T017 [US2] Añadir a `frontend/tests/tours.spec.ts` los escenarios US2-1 (con rol `"2"`, para cubrir R7) y US2-2 (paso omitido por elemento ausente, FR-006)

---

## Phase 5: User Story 3 - Recorrido del administrador (Priority: P3)

- [ ] T018 [US3] Añadir el recorrido `admin` v1 a `frontend/src/features/tours/domain/tours.catalog.ts`
- [ ] T019 [P] [US3] Añadir `data-tour` (`usuarios-tabla`, `admin-bitacora`, `admin-grafica`, `admin-respaldos`) en `frontend/app/usuarios/page.tsx`
- [ ] T020 [US3] Añadir a `frontend/tests/tours.spec.ts` el escenario US3-1

---

## Phase 6: User Story 4 - Volver a ver la guía (Priority: P4)

- [ ] T021 [US4] Añadir a `frontend/tests/tours.spec.ts` los escenarios US4-1 (relanzar tras completar) y US4-2 (sembrar la clave de la versión anterior y comprobar que la nueva se muestra una vez) (FR-007, FR-008)

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T022 [P] Añadir en `use-tour-controller.ts` el gancho `onPopoverRender` con `role="dialog"`, `aria-labelledby`, `aria-describedby` y foco en el botón principal; prueba solo con teclado en `frontend/tests/tours.spec.ts` (FR-009, SC-004)
- [ ] T023 [P] Añadir a `frontend/tests/tours.spec.ts` los casos borde de viewport 375 px, almacenamiento no disponible y dos usuarios en el mismo navegador
- [ ] T024 Ejecutar `npm run lint` y `npx tsc --noEmit` en `frontend/` sin avisos nuevos ni silenciadores
- [ ] T025 Comprobar que `driver.js` solo se importa desde `frontend/src/features/tours/` (búsqueda en el repo) (FR-014, constitución VII)
- [ ] T026 Validación manual del responsable según `specs/002-guia-interactiva-tours/quickstart.md` y prueba de usabilidad (SC-001, SC-002)
- [ ] T027 Ejecutar `/speckit-converge` y cerrar las brechas que reporte

---

## Dependencies & Execution Order

- Setup → Foundational → US1 (MVP) → US2 y US3 en paralelo → US4 → Polish.
- T009–T012 son secuenciales (catálogo → controlador → launcher → montaje).
- `tours.spec.ts` es un solo archivo: las tareas de prueba de cada historia se hacen en orden.

## Implementation Strategy

MVP = US1. Validar con quickstart antes de sumar los recorridos de los otros roles.
