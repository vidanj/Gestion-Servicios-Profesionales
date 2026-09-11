# Research: Pruebas automáticas E2E asistidas por IA

## R1. Herramienta de pruebas E2E

- **Decision**: Seguir con **Playwright** (ya instalado: `@playwright/test` ^1.58.2, 11 specs,
  `playwright.config.ts` y workflow `frontend-tests.yml`).
- **Rationale**: Es la herramienta autorizada por la constitución (Principio V) y la que el equipo
  ya mantiene en CI. Cambiarla duplicaría la suite y el pipeline sin ganancia.

| Criterio | Playwright (actual) | Katalon | Selenium WebDriver | Cypress |
|---|---|---|---|---|
| Licencia | Apache-2.0 | Comercial con plan gratuito limitado | Apache-2.0 | MIT (el panel en la nube es de pago) |
| Encaje con la constitución | Autorizado | Prohibido (otra herramienta) | Prohibido | Prohibido |
| Ya integrado en el repo y en CI | Sí | No | No | No |
| Simulación de red nativa | `page.route` | Limitada / por plugins | No nativa | `cy.intercept` |
| Soporte de agentes de IA | Playwright MCP (árbol de accesibilidad) | Asistentes propios de la plataforma | Vía terceros | Vía terceros |
| Lenguaje | TypeScript (el del frontend) | Groovy / sin código | Varios | JavaScript / TypeScript |
| Esfuerzo de adopción | Nulo | Alto | Alto | Medio |

- **Alternatives considered**: Katalon (plataforma *low-code* orientada a QA; licencia comercial
  para funciones avanzadas); Selenium (maduro, pero sin simulación de red nativa ni espera
  automática); Cypress (buena DX, pero duplicaría la suite existente). Todas descartadas por el
  Principio V y por costo de migración.

## R2. Asistencia de IA

- **Decision**: Playwright MCP como servidor MCP del agente (Claude Code), declarado a nivel de
  proyecto en `.mcp.json` con versión fijada (no `@latest`), para que todo el equipo use el mismo.
- **Rationale**: Opera sobre el **árbol de accesibilidad** de la página y no sobre capturas, lo que
  hace las acciones deterministas y reduce tokens; expone navegación, clics, formularios, red
  (simular peticiones) y generación de localizadores. El agente puede explorar un flujo y proponer
  código de prueba con los mismos localizadores que usaría una persona.
- **Evaluar en T001**: los *Playwright Test Agents* (planificador, generador y reparador), que
  según las notas de versión de Playwright se inicializan con `npx playwright init-agents` para
  distintos agentes. Si están disponibles en la versión instalada, se documentan como alternativa
  en `tests/README.md`.
- **Alternatives considered**: Grabador `npx playwright codegen` (sin IA; útil como respaldo);
  agentes basados en capturas de pantalla (menos deterministas).

## R3. Simulación de la API

- **Decision**: `page.route` con patrones `**/api/<recurso>**`, centralizado en
  `tests/support/mock-api.ts`, y fixtures tipados en `tests/fixtures/`.
- **Rationale**: Es lo que ya usan las 11 specs existentes (`MSWProvider` está comentado en
  `app/layout.tsx`). Los patrones con `**` funcionan aunque `NEXT_PUBLIC_ALLOWED_PATH` no esté
  definido en CI (sin `.env`, la URL resultante es relativa).
- **Alternatives considered**: Reactivar MSW en el navegador (exige tocar el layout y el service
  worker generado; fuera de alcance).

## R4. Selectores

- **Decision**: `data-testid` en kebab-case (convención ya usada en `login.spec.ts`:
  `username-input`, `login-button`, `error-message`) y `getByRole` cuando el rol y el nombre
  accesible sean únicos.
- **Rationale**: Las cuatro páginas objetivo no tienen hoy ningún `data-testid` y usan estilos en
  línea; seleccionar por estilo o posición sería frágil (FR-004).

## R5. Sesión en las pruebas

- **Decision**: `loginAs(page, rol)` siembra con `addInitScript` el `localStorage` que usa la
  aplicación: `token` y `auth-user` (estado persistido de Zustand con `firstName`, `lastName`,
  `email` y `role`).
- **Rationale**: Evita repetir el login en cada prueba y permite probar las dos formas del rol
  (`"Client"` y `"1"`) que hoy coexisten en el store.

## R6. Borradores de IA fuera de la suite

- **Decision**: El agente escribe borradores en `frontend/e2e-drafts/` (ignorado por git y fuera
  de `testDir`); una persona los revisa, los ajusta a los fixtures y ayudantes, y los mueve a
  `tests/`.
- **Rationale**: FR-009: nada generado por IA entra a la suite sin revisión humana.

## R7. Determinismo

- **Decision**: Validar con `npx playwright test --repeat-each=5` (SC-004) antes del PR, y con
  `retries: 2` solo en CI (configuración existente). Un fallo intermitente se corrige en la prueba
  o en el fixture, nunca con `.skip`.

## R8. Forma real de las respuestas

- **Decision**: Tomar las formas de `ServiceDto`, `ServiceRequestDto`, `CategoryDto` y de la
  respuesta paginada `{ data, totalCount, page, size }` de `ServicesController.GetServices`.
- **Pendiente (T010)**: confirmar con la API en marcha la serialización de `RequestStatus`
  (número o nombre) y la envoltura de `/api/Categories`, `/api/Services/my` y
  `/api/ServiceRequests/my`. Si difieren del [contrato](contracts/mock-api.md), se corrige el
  contrato antes de escribir las pruebas.
