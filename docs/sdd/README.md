# Spec-Driven Development — índice del informe

| # | Punto del informe | Documento |
|---|---|---|
| 1 | Guía completa de SDD, beneficios y seguimiento | [sdd-implementation.md](sdd-implementation.md) |
| 2 | Propuesta original con comparación Kiro vs Spec Kit y propuesta SDD + Skills | [sdd-proposal.md](sdd-proposal.md) |
| 3 | Guía de seguimiento de la instalación de Spec Kit | [guia-instalacion-spec-kit.md](guia-instalacion-spec-kit.md) |
| 4 | Ingeniería inversa y diagnóstico del estado actual | [estado-actual/ingenieria-inversa.md](estado-actual/ingenieria-inversa.md) |
| 5 | Diagrama ER (Mermaid) | [estado-actual/er-diagram.md](estado-actual/er-diagram.md) |
| 6 | Trazabilidad | [estado-actual/trazabilidad.md](estado-actual/trazabilidad.md) |
| 7 | Análisis: qué falta para un SDD formal | [estado-actual/analisis-brechas.md](estado-actual/analisis-brechas.md) |
| 8 | Constitución del proyecto | [../../.specify/memory/constitution.md](../../.specify/memory/constitution.md) |
| 9 | Piloto a) Pruebas automáticas (Playwright + MCP) | [../../specs/001-e2e-playwright-mcp/](../../specs/001-e2e-playwright-mcp/spec.md) |
| 10 | Piloto b) Guía interactiva (driver.js) | [../../specs/002-guia-interactiva-tours/](../../specs/002-guia-interactiva-tours/spec.md) |
| 11 | Piloto c) Software como infraestructura (Codespaces, Terraform, CI/CD) | [../../specs/003-infraestructura-como-codigo/](../../specs/003-infraestructura-como-codigo/spec.md) |
| 12 | Spec 007 — Módulo de créditos de autores (planeación) | [../../specs/007-creditos-autores/](../../specs/007-creditos-autores/spec.md) |

Cada piloto contiene: `spec.md`, `checklists/`, `plan.md`, `research.md`, `data-model.md`,
`contracts/`, `quickstart.md`, `tasks.md` y `analysis.md` (reporte de `/speckit-analyze`).

La spec 007 sigue la misma estructura que los pilotos, con una diferencia de origen: no nace de
una brecha detectada por ingeniería inversa sino de una necesidad nueva del producto. Es el primer
ejercicio del ciclo sobre una capacidad de producto, no transversal. La numeración salta de 003 a
007 porque 004, 005 y 006 están reservados a los módulos de negocio propuestos en el
[análisis de brechas](estado-actual/analisis-brechas.md).
