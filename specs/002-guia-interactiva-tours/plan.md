# Implementation Plan: Guía interactiva de producto por rol

**Branch**: `002-guia-interactiva-tours` | **Date**: 2026-09-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-guia-interactiva-tours/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Recorridos guiados por rol (cliente, profesional, administrador) construidos con `driver.js`,
encapsulados en una feature nueva `src/features/tours/` con el corte del proyecto
(`domain / application / infrastructure / presentation`). Un componente cliente montado en la
navegación decide el inicio automático tras el login, ofrece el botón "Ver guía" y guarda el
resultado por usuario y versión en `localStorage`. Los elementos se anclan con atributos
`data-tour="…"`, que también podrá usar la suite E2E del piloto 001. No hay cambios de backend.

## Technical Context

**Language/Version**: TypeScript 5 en modo estricto, React 19.2, Next.js 16.1 (App Router)

**Primary Dependencies**: `driver.js` (MIT, **nueva**, versión exacta fijada en T001), Chakra UI v3 (tokens del tema), Zustand (`src/store/auth-store.ts`, existente)

**Storage**: `localStorage` del navegador (clave por recorrido, versión y usuario); sin almacenamiento en el servidor

**Testing**: Playwright (`frontend/tests/tours.spec.ts`) con `page.route` para la API y `addInitScript` para preparar `localStorage`

**Target Platform**: Navegadores de escritorio y móviles modernos; Chromium en CI

**Project Type**: Aplicación web (solo frontend)

**Performance Goals**: La librería se carga con `import()` dinámico solo cuando hay un recorrido que mostrar; sin impacto en la carga inicial de páginas sin recorrido pendiente

**Constraints**: Solo en cliente (sin acceso a `window` durante SSR); sin `any` ni `@ts-ignore`; estilos con variables CSS del tema de Chakra; `driver.js` limitado a esta feature (constitución VII, v1.1.0)

**Scale/Scope**: 3 recorridos de ≤ 7 pasos; ~12 anclajes `data-tour` en 5–6 archivos existentes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación | Resultado |
|---|---|---|
| I. Spec y plan aprobados antes del código | Este plan y `tasks.md` se presentan para aprobación; no se implementa antes | PASA |
| II. Capas del backend | Sin cambios de backend | N/A |
| III. DTOs y autorización | Sin endpoints nuevos; el rol se lee de la sesión existente | N/A |
| IV. Esquema por migración | Sin cambios de esquema (persistencia en el navegador por decisión) | N/A |
| V. Pruebas obligatorias | E2E de inicio automático, omitir, no repetir, relanzar y paso ausente (FR-013) | PASA |
| VI. Calidad automática | ESLint y TypeScript estricto; sin silenciadores | PASA |
| VII. Stack del frontend cerrado | Feature en `src/features/tours/` con el corte del proyecto; `driver.js` amparado por la **excepción v1.1.0**, usado solo aquí, cargado solo en cliente y estilizado con tokens del tema | PASA (con excepción ratificada) |

**Re-evaluación tras el diseño (Phase 1):** sin cambios; el diseño no introduce otra librería de UI
ni estado global nuevo (el estado del recorrido es local al controlador y a `localStorage`).

## Project Structure

### Documentation (this feature)

```text
specs/002-guia-interactiva-tours/
├── plan.md              # este archivo
├── research.md          # decisiones y alternativas
├── data-model.md        # Recorrido, Paso, Estado del recorrido
├── quickstart.md        # validación manual y automática
├── contracts/
│   └── tour-ui-contract.md   # anclajes data-tour, clave de localStorage, API del hook
├── checklists/          # requirements.md, ux.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
frontend/
├── src/features/tours/
│   ├── domain/
│   │   ├── tour.model.ts            # tipos: Tour, TourStep, TourRole, TourResult
│   │   └── tours.catalog.ts         # pasos y textos por rol (FR-012)
│   ├── application/
│   │   ├── normalize-role.ts        # "Client" | "1" → "client" (ver research R7)
│   │   └── use-tour-controller.ts   # auto-inicio, relanzar, registrar resultado
│   ├── infrastructure/
│   │   └── tour-state.repository.ts # localStorage con try/catch y respaldo en memoria
│   └── presentation/
│       ├── tour-launcher.tsx        # botón "Ver guía" + montaje; import() de driver.js
│       └── tour-theme.css           # estilos del popover con variables del tema
├── src/components/nav/nav.tsx       # monta <TourLauncher/>; añade data-tour a los enlaces
├── app/dashboard/page.tsx           # data-tour del catálogo
├── app/profesionista/page.tsx       # data-tour del formulario y la lista de servicios
├── app/solicitudes/page.tsx         # data-tour de la tabla de solicitudes
├── app/usuarios/page.tsx            # data-tour de la administración
└── tests/tours.spec.ts              # E2E del recorrido
```

**Structure Decision**: Aplicación web existente; la feature vive en
`frontend/src/features/tours/` siguiendo el patrón de `src/features/solicitudes/`. El punto de
montaje es `src/components/nav/nav.tsx`, que ya es un componente cliente presente en todas las
páginas autenticadas y ya lee `useAuthStore`.

## Complexity Tracking

Sin violaciones que justificar: la nueva dependencia está cubierta por la enmienda v1.1.0 de la
constitución, ratificada antes de este plan.
