# Parámetros de configuración de las herramientas

> **Informe de planeación** (2026-09-11). Para cada herramienta: para qué se usa, cómo se instala,
> qué parámetros se configuran y en qué estado está su implementación. Las herramientas marcadas como
> *planeadas* se implementan desde el `tasks.md` de su spec, con aprobación previa (constitución,
> Principio I).
>
> **Relacionados:** [guía de instalación de Spec Kit](../sdd/guia-instalacion-spec-kit.md) ·
> [constitución](../../.specify/memory/constitution.md) · specs [001](../../specs/001-e2e-playwright-mcp/plan.md),
> [002](../../specs/002-guia-interactiva-tours/plan.md) y [003](../../specs/003-infraestructura-como-codigo/plan.md)

## Resumen

| Herramienta | Uso en el proyecto | Estado | Dónde se define |
|---|---|---|---|
| GitHub Spec Kit 1.0.6 | Spec-Driven Development | **Instalada** | `.specify/` |
| Claude Code (skills) | Agente que ejecuta el ciclo SDD | **Instalada** | `.claude/skills/` |
| Playwright 1.58 | Pruebas E2E | **En uso** (11 specs) | `frontend/playwright.config.ts` |
| Playwright MCP | Generar y diagnosticar pruebas con IA | Planeada (spec 001, T001) | `.mcp.json` |
| Katalon / Selenium | Notación de los casos de prueba | Evaluadas, **no se instalan** | [casos-de-prueba.md](casos-de-prueba.md) |
| driver.js | Recorridos guiados por rol | Planeada (spec 002, T001) | `frontend/src/features/tours/` |
| GitHub Codespaces | Entorno de trabajo en la nube | Planeada (spec 003, US1) | `.devcontainer/` |
| Terraform | Infraestructura declarativa multi-proveedor | Planeada (spec 003, US2/US4) | `infra/` |
| GitHub Actions | CI/CD | **En uso** (5 workflows) + 5 planeados | `.github/workflows/` |
| Docker | Imagen de la API y verificación en CI | **En uso** | `Dockerfile`, `docker-compose.yml` |

---

## 1. GitHub Spec Kit

**Planeación de uso.** Todo cambio relevante nace de un spec versionado:
`/speckit-specify → clarify → plan → checklist → tasks → analyze → implement → converge`.

**Instalación.**

```powershell
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@v1.0.6
specify init --here --force --integration claude --script ps --non-interactive
```

| Parámetro | Valor | Efecto |
|---|---|---|
| `--here` / `--force` | Activos | Inicializa en el repo existente sin tocar el código de la aplicación |
| `--integration` | `claude` | Instala las skills en `.claude/skills/speckit-*` |
| `--script` | `ps` | Scripts PowerShell en `.specify/scripts/powershell/` |
| `init-options.json` → `feature_numbering` | `sequential` | Directorios `specs/001-…`, `002-…` |
| `init-options.json` → `ai_skills` | `true` | Integración en modo skills |
| `integration.json` → `invoke_separator` | `-` | Invocación `/speckit-plan` (no `/speckit.plan`) |
| `SPECIFY_FEATURE_DIRECTORY` | `specs/<NNN-nombre>` | Fija la feature sobre la que trabaja la skill |
| `.specify/feature.json` | Local (ignorado) | Puntero a la feature activa |
| Constitución | v1.1.0 | 7 principios; el Principio VII incluye la excepción de driver.js |

**Implementación.** Hecha: `.specify/`, constitución y tres specs piloto.

## 2. Claude Code y skills

**Planeación de uso.** Las personas invocan las skills en Claude Code; el agente nunca implementa
sin aprobación.

| Parámetro | Valor |
|---|---|
| Skills instaladas | `speckit-constitution`, `-specify`, `-clarify`, `-plan`, `-checklist`, `-tasks`, `-analyze`, `-implement`, `-converge`, `-taskstoissues` |
| Versionado (`.gitignore`) | `.claude/*` ignorado y `!.claude/skills/` versionado |
| Carpeta | `.claude/` en minúsculas (se renombró desde `.CLAUDE/` para que funcione en Linux y macOS) |
| Guías locales del agente | `CLAUDE.md` y `.claude/RULES.md` (no versionados; no pueden contradecir la constitución) |
| Skills propias propuestas (fase 4) | `gsp-ingenieria-inversa`, `gsp-backend-feature`, `gsp-e2e-mcp`, `gsp-pr-sdd` |

## 3. Playwright (pruebas E2E)

**Instalación.** Ya presente: `@playwright/test ^1.58.2`, navegadores con
`npx playwright install --with-deps chromium`.

| Parámetro (`playwright.config.ts`) | Valor | Motivo |
|---|---|---|
| `testDir` | `./tests` | Specs `frontend/tests/*.spec.ts` |
| `use.baseURL` | `http://localhost:3000` | Rutas relativas en las specs nuevas |
| `use.trace` | `on-first-retry` | Traza solo al reintentar (costo) |
| `use.screenshot` | `only-on-failure` | Evidencia de fallos (FR-006 de la spec 001) |
| `retries` | `2` en CI, `0` en local | En local los fallos intermitentes se ven tal cual |
| `workers` | `1` en CI | Las specs comparten el puerto 3000 |
| `forbidOnly` | `true` en CI | Evita dejar un `test.only` olvidado |
| `reporter` | `list` + `html` en CI | Reporte como artefacto |
| `projects` | Solo `chromium` | Decisión vigente de costo/beneficio |
| `webServer.command` | `npm run start` (CI) / `npm run dev` (local) | CI prueba la compilación de producción |
| `webServer.reuseExistingServer` | `!CI` | En local reutiliza el servidor: **liberar el puerto 3000 antes de probar** |
| `webServer.timeout` | `120000` ms | Arranque de Next.js |

Simulación de la API: `page.route('**/api/…')` (el `MSWProvider` está desactivado en `app/layout.tsx`).

## 4. Playwright MCP (IA)

**Planeación de uso.** Explorar un flujo, generar borradores de prueba en `frontend/e2e-drafts/` y
diagnosticar pruebas rotas; un humano revisa antes de pasar el borrador a la suite.

```json
{
  "mcpServers": {
    "playwright": { "command": "npx", "args": ["@playwright/mcp@<versión fijada>"] }
  }
}
```

| Parámetro | Valor |
|---|---|
| Ámbito | Proyecto (`.mcp.json` versionado); alternativa personal: `claude mcp add playwright npx @playwright/mcp@<versión>` |
| Versión | Fija, nunca `@latest` (tarea T001 de la spec 001) |
| Modo | Árbol de accesibilidad (sin capturas), acciones deterministas |
| Opciones de navegador y modo sin interfaz | Se definen en T001 según la versión fijada |
| Salida del agente | Solo `frontend/e2e-drafts/` (ignorada por git y fuera de `testDir`) |

## 5. Katalon Studio y Selenium

**Decisión.** Se evaluaron y **no se instalan**: la constitución (Principio V) autoriza una sola
herramienta E2E, Playwright, que ya está integrada en CI. Los casos de prueba se redactan con la
notación de Katalon (palabras clave `WebUI.*`) y de Selenium IDE (`open`, `type`, `click`,
`assertText`) para que sean portables ([casos-de-prueba.md](casos-de-prueba.md)).

| Aspecto | Katalon | Selenium | Playwright (elegida) |
|---|---|---|---|
| Instalación | Katalon Studio (IDE) + licencia para funciones avanzadas | WebDriver + driver por navegador | `npm` (ya instalado) |
| Simulación de la API | No nativa | No nativa | `page.route` |
| Requisito para ejecutarla aquí | Enmienda de la constitución | Enmienda de la constitución | Ninguno |

## 6. driver.js (guía interactiva)

**Instalación planeada.** `npm install driver.js@<x.y.z> --save-exact` en `frontend/`; CSS
`driver.js/dist/driver.css` importado dinámicamente solo en cliente.

| Parámetro | Valor planeado | Requisito |
|---|---|---|
| `steps[].element` | `[data-tour="<id>"]` (contrato de anclajes) | FR-004 |
| `steps[].popover.title` / `description` | ≤ 60 / ≤ 200 caracteres, en español | FR-004 |
| `steps[].popover.side` | `bottom` por defecto | — |
| `showProgress` / `progressText` | `true` / `"{{current}} de {{total}}"` | FR-005 |
| `nextBtnText` / `prevBtnText` / `doneBtnText` | `Siguiente` / `Anterior` / `Terminar` | UX |
| `allowClose` | `true` (cerrar = omitido) | FR-003 |
| `allowKeyboardControl` | `true` (←, →, Esc) | FR-009 |
| `popoverClass` | `gsp-tour` (estilos con variables del tema de Chakra) | FR-010 |
| `onPopoverRender` | Añade `role="dialog"`, `aria-labelledby`, `aria-describedby` y el foco | FR-009 |
| `onDestroyed` | Registra `completed` o `dismissed` | FR-008 |
| Estado | `localStorage` `gsp-tour:<tourId>:v<versión>:<usuario>` | FR-008 |
| Alcance | Solo `src/features/tours/` (excepción de la constitución v1.1.0) | FR-014 |

## 7. GitHub Codespaces (devcontainer)

| Parámetro (`.devcontainer/devcontainer.json`) | Valor planeado |
|---|---|
| `dockerComposeFile` / `service` | `docker-compose.yml` / `app` (+ `db`: `postgres:18-alpine`) |
| `features` | .NET 9 y Node 24 (features oficiales de devcontainers) |
| `forwardPorts` | `3000` (frontend), `5000` (API) |
| `secrets` (recomendados) | `JWT_KEY`, `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `SMTP_FROM` |
| `postCreateCommand` | `bash .devcontainer/post-create.sh` (herramientas, `.env`, migraciones, Chromium) |
| `postStartCommand` | Espera a `db` con `pg_isready` |
| Variables calculadas | `NEXT_PUBLIC_ALLOWED_PATH` y `ALLOWED_ORIGINS` a partir de `CODESPACE_NAME` y `GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN` |
| Extensiones sugeridas | C# Dev Kit, ESLint, Playwright, HashiCorp Terraform, Claude Code |

## 8. Terraform (multi-proveedor)

| Parámetro | Valor planeado |
|---|---|
| `required_version` | Terraform 1.x (versión exacta en T001 de la spec 003) |
| Proveedores | `render-oss/render` (API, frontend, PostgreSQL, grupo de variables) · `integrations/github` (ambientes y secretos) |
| Estado remoto | HCP Terraform, workspace `gsp-staging`, con bloqueo |
| Autenticación | `RENDER_API_KEY`, `RENDER_OWNER_ID`, `TF_API_TOKEN`, `GH_ADMIN_TOKEN` (solo en GitHub Environments) |
| Módulos | `infra/modules/render-stack`, `infra/modules/github-platform` (interfaz común de entradas y salidas) |
| Protección de datos | `prevent_destroy` en `render_postgres` |
| Ambiente | Solo `infra/envs/staging` (producción queda fuera de esta iteración) |

## 9. GitHub Actions

| Workflow | Disparador | Parámetros clave | Estado |
|---|---|---|---|
| `backend-lint.yml` | push `main`/`dev`/`feat/**`/`chore/**`/`fix/**`; PR con `backend/**` | .NET 9, `csharpier check`, `AnalysisLevel=latest-recommended` | En uso |
| `backend-tests.yml` | Ídem | Variables JWT/SMTP/DB ficticias, cobertura con ReportGenerator, artefactos 30/14 días | En uso |
| `frontend-tests.yml` | push (+ `ci/**`); PR con `frontend/**` | Node 24, `npm ci`, build, Chromium, `timeout-minutes: 15` | En uso |
| `docker-image.yml` | push (+ `ci/**`); PR con `Dockerfile`/`backend/**` | Buildx con caché GHA, `postgres:18-alpine`, sondeo de `/health/ready` 150 s | En uso |
| `codeql.yml` + `dependabot.yml` | Programado / PR | Análisis de seguridad y actualizaciones | En uso |
| `infra-plan.yml` / `infra-apply.yml` | PR con `infra/**` / manual | Environment `infra` con revisión, concurrencia sin cancelar | Planeado |
| `deploy-staging.yml` | push a `dev` / manual con `sha` | GHCR por SHA, sondeo de 300 s, concurrencia `deploy-staging` | Planeado |
| `promote-production.yml` | Manual | Environment `production` con revisión | Planeado |
| `secret-scan.yml` | PR | gitleaks sobre el diff | Planeado |

## 10. Docker

| Parámetro | Valor |
|---|---|
| Etapas | `sdk:9.0` (build) → bundle de migraciones `efbundle` → `aspnet:9.0` (runtime) |
| Cliente de PostgreSQL | `postgresql-client-18` (respaldos con `pg_dump`) |
| `ASPNETCORE_URLS` / `EXPOSE` | `http://+:10000` / `10000` (Render) |
| `HEALTHCHECK` | `/health/ready` · intervalo 30 s · timeout 5 s · `start-period` 60 s · 3 reintentos |
| `BACKUP_DIR` | `/var/backups/gsp` (montar como volumen) |
| `docker-compose.yml` | API detrás de NGINX, `FORWARDED_LIMIT=1` |
| Cambio planeado | `entrypoint.sh` con migración no fatal y sonda de migraciones pendientes (issue #189, spec 003) |

## 11. Variables de entorno de la aplicación

Definidas en `.env.example` (el `.env` real nunca se versiona). El inventario completo por
ambiente está en el [modelo de datos de la spec 003](../../specs/003-infraestructura-como-codigo/data-model.md).
**Sensibles:** `DB_PASSWORD`, `JWT_KEY`, `SMTP_USER`, `SMTP_PASSWORD`.
**Hallazgo:** `.env.example` trae un `JWT_KEY` con forma de clave real; se reemplaza por un
marcador (spec 003, T003).
