# Implementation Plan: Pruebas automáticas E2E asistidas por IA

**Branch**: `001-e2e-playwright-mcp` | **Date**: 2026-09-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-e2e-playwright-mcp/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Ampliar la suite Playwright existente con los flujos sin cobertura (catálogo → solicitud,
servicios del profesional y seguimiento de solicitudes), usando datos simulados compartidos
(fixtures) y ayudantes de simulación de la API con `page.route`, y atributos `data-testid` en las
pantallas que hoy no tienen ninguno. Se configura Playwright MCP como servidor del agente de Claude
Code, a nivel de proyecto, para explorar la aplicación y proponer borradores de prueba en una
carpeta fuera de la suite; una persona los revisa antes de incorporarlos. CI no cambia: sigue
corriendo `frontend-tests.yml` en Chromium.

## Technical Context

**Language/Version**: TypeScript 5; Node 24 (CI); Next.js 16.1 / React 19.2 (aplicación bajo prueba)

**Primary Dependencies**: `@playwright/test` ^1.58.2 (existente); Playwright MCP (`@playwright/mcp`, versión fijada en T001) como herramienta de desarrollo del agente, **no** como dependencia de la aplicación

**Storage**: N/A (datos simulados en memoria por prueba)

**Testing**: Playwright Test con `page.route` y `page.addInitScript`; `webServer` existente (`npm run dev` en local, `npm run start` en CI)

**Target Platform**: Chromium (CI) — decisión vigente del proyecto

**Project Type**: Aplicación web (pruebas del frontend)

**Performance Goals**: Suite completa < 10 min en CI (SC-003); cada spec nuevo < 60 s en local

**Constraints**: Sin backend ni base de datos reales (Principio V); sin `.skip` ni pruebas reescritas para pasar; selectores por `data-testid` o rol accesible; ninguna credencial real en los fixtures

**Scale/Scope**: 18 escenarios de aceptación + 6 casos borde; 4 archivos de spec nuevos; ~30 atributos `data-testid` en 4 páginas

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación | Resultado |
|---|---|---|
| I. Spec y plan aprobados | Se presenta para aprobación; nada se implementa antes | PASA |
| II–IV. Backend | Sin cambios en el backend ni en el esquema | N/A |
| V. Pruebas obligatorias y honestas | Playwright es la herramienta autorizada; se ejecuta contra respuestas simuladas; los fallos por comportamiento real se reportan como defectos (FR-007); Katalon, Selenium y Cypress **no se instalan** | PASA |
| VI. Calidad automática | ESLint y TypeScript estricto también en `tests/`; sin `any` en los fixtures | PASA |
| VII. Stack del frontend | Solo se añaden atributos `data-testid`; sin librerías nuevas en la aplicación. Playwright MCP es tooling del agente, fuera del bundle | PASA |
| Seguridad y datos | Fixtures sin datos reales; `.mcp.json` sin secretos | PASA |

**Re-evaluación tras el diseño (Phase 1):** sin cambios.

## Project Structure

### Documentation (this feature)

```text
specs/001-e2e-playwright-mcp/
├── plan.md
├── research.md
├── data-model.md          # fixtures y mapa escenario → prueba
├── quickstart.md
├── contracts/
│   ├── mock-api.md        # respuestas simuladas por endpoint (de los DTOs reales)
│   └── ia-workflow.md     # uso del agente con Playwright MCP y reglas de revisión
├── checklists/            # requirements.md, e2e.md
└── tasks.md
```

### Source Code (repository root)

```text
.mcp.json                              # servidor "playwright" para Claude Code (proyecto)
.gitignore                             # + frontend/e2e-drafts/
frontend/
├── e2e-drafts/                        # borradores generados por IA (fuera de testDir, ignorado)
├── tests/
│   ├── README.md                      # procedimiento de generación y revisión con IA
│   ├── fixtures/
│   │   ├── services.ts  categories.ts  requests.ts  session.ts
│   ├── support/
│   │   ├── mock-api.ts                # page.route por recurso
│   │   └── session.ts                 # loginAs(rol) con addInitScript
│   ├── catalogo.spec.ts               # US1 (escenarios 1–2)
│   ├── solicitar-servicio.spec.ts     # US1 (escenarios 3–6 + doble clic)
│   ├── profesionista.spec.ts          # US2
│   └── solicitudes.spec.ts            # US3
└── app/
    ├── dashboard/page.tsx             # + data-testid del catálogo
    ├── catalogo/[id]/page.tsx         # + data-testid del detalle y formulario
    ├── profesionista/page.tsx         # + data-testid de servicios
    └── solicitudes/page.tsx           # + data-testid de solicitudes
backend/docs/test-plan.md              # + casos CP-E2E-02… ligados a FR
```

**Structure Decision**: Se conserva `testDir: './tests'` y la configuración de
`frontend/playwright.config.ts`; los ayudantes nuevos viven en `tests/support/` y
`tests/fixtures/`. Los borradores de IA quedan fuera de `testDir` para que nunca se ejecuten sin
revisión.

## Complexity Tracking

Sin violaciones que justificar.
