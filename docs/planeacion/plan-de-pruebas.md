# Plan de pruebas

> **Informe de planeación** (2026-09-11). Amplía el plan formal existente
> ([backend/docs/test-plan.md](../../backend/docs/test-plan.md), 4 casos) con la estrategia completa,
> la ejecución de la suite actual y la evolución planeada en la
> [spec 001](../../specs/001-e2e-playwright-mcp/spec.md).
> Casos detallados: [casos-de-prueba.md](casos-de-prueba.md).

## 1. Objetivo y alcance

Asegurar que cada requisito del sistema de intermediación cliente ↔ profesional tenga al menos una
prueba automatizada que lo verifique, en el nivel más barato posible, y que la suite corra sola en
cada cambio.

- **Dentro:** backend (servicios, repositorios, pipeline HTTP), frontend (flujos E2E contra
  respuestas simuladas) y la imagen Docker (arranque y sonda de salud).
- **Fuera de esta iteración:** pruebas de carga, pruebas contra producción y pruebas manuales de
  usabilidad, salvo la prueba de usabilidad de la spec 002 (SC-001).

## 2. Estrategia (pirámide de pruebas)

| Nivel | Herramienta | Qué valida | Ubicación | Comando | Dónde corre |
|---|---|---|---|---|---|
| Unitario | xUnit + Moq + FluentAssertions | Reglas de negocio de servicios y repositorios | `backend/SistemaServicios.Tests/Unit/` | `dotnet test --filter "FullyQualifiedName~Unit"` | `backend-tests.yml` |
| Integración | xUnit + `WebApplicationFactory` (InMemory DB + JWT de prueba) | Pipeline HTTP completo: autorización, middleware, rate limiting, serialización | `backend/SistemaServicios.Tests/Integration/` | `dotnet test --filter "FullyQualifiedName~Integration"` | `backend-tests.yml` |
| E2E | **Playwright** con `page.route` | Flujos de usuario en el navegador, sin backend real | `frontend/tests/*.spec.ts` | `cd frontend && npm test` | `frontend-tests.yml` |
| Humo de contenedor | Docker + `curl` | La imagen arranca, aplica migraciones y responde `Healthy` | `.github/workflows/docker-image.yml` | — | `docker-image.yml` |
| Calidad estática | CSharpier, StyleCop, Roslynator, ESLint, CodeQL | Formato, estilo y seguridad | — | `dotnet tool run csharpier check backend/` | `backend-lint.yml`, `codeql.yml` |

**Reglas (constitución, Principio V):** lógica nueva → prueba unitaria; endpoint nuevo o
modificado → prueba de integración; los E2E nunca tocan la base ni el backend reales; ninguna prueba
se borra, se salta ni se reescribe para que pase.

## 3. Entornos

| Entorno | Datos | Configuración |
|---|---|---|
| Local | InMemory (backend) / respuestas simuladas (E2E) | `.env` local; puerto 3000 libre antes de correr E2E |
| CI (`ubuntu-latest`) | Ídem | Variables ficticias en el workflow; Chromium |
| Codespaces (planeado, spec 003) | PostgreSQL 18 efímero | Secretos de Codespaces |

## 4. Criterios de entrada y salida

- **Entrada:** spec aprobado (`/speckit-analyze` sin CRITICAL) y fixtures o datos definidos.
- **Salida de un PR:** CI en verde, pruebas nuevas para cada FR tocado, cero `.skip`/`.only`,
  cobertura del backend sin reducción injustificada.
- **Salida de una feature:** todos los escenarios de aceptación del spec automatizados y
  `/speckit-converge` sin brechas.

## 5. Inventario actual

| Nivel | Archivos | Módulos cubiertos |
|---|---|---|
| Unitario | 20 | Auth, Token, Email y su cola, Users, UserLogs, Perfil, archivos, respaldos, telemetría |
| Integración | 9 + factory | Auth, Admin, Perfil, Files, Health, cabeceras reenviadas, CORS, Swagger, Logging |
| E2E | 11 | about, login, register, recovery, perfil, contraseña, usuarios, registrados, gráfica, logs, respaldos |
| **Sin pruebas** | — | Servicios, Categorías, Solicitudes y Calificaciones (todos los niveles); E2E de catálogo, detalle, profesionista y solicitudes |

## 6. Ejecución de la suite (evidencia)

Ejecución local del 2026-09-11, antes de cualquier cambio, sobre `dev` @ 0159a9b.

| Suite | Comando | Resultado | Duración |
|---|---|---|---|
| E2E (Playwright, Chromium) | `cd frontend && npx playwright test` | **108 pasaron, 0 fallaron** (11 specs) | 1.1 min de pruebas; 85 s con el arranque de Next.js |
| Backend (unitarias + integración) | `dotnet test backend/SistemaServicios.sln` | **No ejecutable en este equipo:** `global.json` exige el SDK 9.0.311 y solo está instalado el 9.0.201. Evidencia en CI: `backend-tests.yml` en **verde** sobre `dev` @ 0159a9b ([run 34547314151](https://github.com/vidanj/Gestion-Servicios-Profesionales/actions/runs/34547314151)) | — |
| Formato y análisis del backend | `backend-lint.yml` | Verde en `dev` @ 0159a9b | — |
| Imagen Docker (arranque + `/health/ready`) | `docker-image.yml` | Verde en `dev` @ 0159a9b | — |

> **[CAPTURA: salida de `npx playwright test`]**
>
> **[CAPTURA: salida de `dotnet test`]**
>
> **[CAPTURA: último run en verde de `frontend-tests.yml` y `backend-tests.yml` en GitHub Actions]**

## 7. Evolución planeada (spec 001)

| Qué | Detalle | Tareas |
|---|---|---|
| Flujos nuevos | US1 cliente (catálogo → solicitud), US2 profesional (servicios), US3 seguimiento | T011–T020 |
| Escenarios | 18 de aceptación + 6 casos borde → CP-E2E-02 … CP-E2E-20 | [casos-de-prueba.md](casos-de-prueba.md) |
| Infraestructura de pruebas | Fixtures tipados desde los DTOs, ayudantes `page.route`, sesión con `addInitScript` | T004–T010 |
| IA | Playwright MCP para explorar, generar borradores y diagnosticar; revisión humana obligatoria | T001, T021–T022 |
| Metas | 15 de 17 pantallas con E2E; suite < 10 min en CI; 0 intermitentes en 5 corridas | SC-002 … SC-004 |

## 8. Cómo se generan specs y pruebas para módulos o requerimientos nuevos (PR mínimo)

El PR de adopción de SDD instala las **skills** de Spec Kit en `.claude/skills/`. Con eso, un
requerimiento nuevo sigue siempre el mismo camino y produce sus pruebas planeadas:

```text
/speckit-specify  <requerimiento en lenguaje natural>    → specs/NNN-nombre/spec.md (US, FR, SC)
/speckit-clarify                                         → preguntas y decisiones registradas
/speckit-plan     <stack y restricciones>                → plan, research, data-model, contracts, quickstart
/speckit-checklist pruebas                               → checklist de calidad de los requisitos
/speckit-tasks                                           → tasks.md con tareas de prueba por historia
/speckit-analyze                                         → cobertura FR → tareas y conflictos con la constitución
```

Ejemplo del siguiente módulo (brecha B1, aún sin crear):
`/speckit-specify Cotización: el cliente solicita una cotización de un servicio, el profesional la responde con precio y duración estimados, y el cliente la acepta para convertirla en solicitud.`

## 9. Roles y calendario

| Actividad | Responsable | Semana |
|---|---|---|
| Fundaciones de la spec 001 (fixtures, simulación, sesión) | Integrante asignado al issue de la spec 001 | 1 |
| US1 (MVP) + validación con quickstart | Ídem | 1–2 |
| US2 y US3 | Ídem | 2–3 |
| Borradores con IA y diagnóstico (US4) | Ídem | 3 |
| Revisión de casos y marcado de checklists | Revisor del PR | Continua |

## 10. Riesgos

| Riesgo | Mitigación |
|---|---|
| Las respuestas simuladas divergen de la API real | Contrato `mock-api.md` sacado de los DTOs; confirmación contra la API (T010) |
| Pruebas intermitentes | `--repeat-each=5` antes del PR; nunca `.skip` |
| Servidor viejo en el puerto 3000 | Liberar el puerto antes y después |
| La IA genera pruebas frágiles | Borradores fuera de `testDir` y lista de revisión humana |

## 11. Métricas

Escenarios automatizados / escenarios del spec · pantallas con E2E · duración de la suite en CI ·
fallos intermitentes por corrida · defectos encontrados por las pruebas nuevas.
