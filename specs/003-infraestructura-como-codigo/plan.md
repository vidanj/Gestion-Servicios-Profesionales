# Implementation Plan: Software como infraestructura

**Branch**: `003-infraestructura-como-codigo` | **Date**: 2026-09-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-infraestructura-como-codigo/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Tres capas sobre lo que ya existe (Dockerfile multi-etapa, `docker-compose.yml`, sonda
`/health/ready` y el workflow `docker-image.yml`):

1. **Entorno en la nube (P1):** `.devcontainer/` para GitHub Codespaces con PostgreSQL 18, .NET 9,
   Node 24 y Chromium; los secretos llegan como secretos de Codespaces y un script genera el `.env`.
2. **Infraestructura declarativa multi-proveedor (P2, P4):** Terraform en `infra/`, con un módulo
   por proveedor detrás de una interfaz común: `render-stack` (API, frontend, PostgreSQL y grupo de
   variables del staging) y `github-platform` (ambientes `staging` y `production` y nombres de
   secretos de Actions y Codespaces). Estado remoto con bloqueo en HCP Terraform.
3. **Despliegue continuo verificado (P3):** la imagen se publica en GHCR con la etiqueta del SHA;
   `deploy-staging.yml` despliega en cada push a `dev`, sondea `/health/ready` y falla de forma
   visible; la reversión redespliega el SHA anterior. La migración deja de ser fatal en
   `entrypoint.sh` y una sonda nueva informa migraciones pendientes (issue #189).

## Technical Context

**Language/Version**: HCL (Terraform 1.x, versión exacta fijada en `required_version` en T001); YAML (GitHub Actions); Bash (`entrypoint.sh`, `post-create.sh`); C# .NET 9 (sonda de migraciones)

**Primary Dependencies**: Proveedores de Terraform `render-oss/render` (auth `RENDER_API_KEY`, `RENDER_OWNER_ID`) e `integrations/github`; imágenes `mcr.microsoft.com/devcontainers/*` y `postgres:18-alpine`; acciones `docker/build-push-action`, `hashicorp/setup-terraform`

**Storage**: PostgreSQL 18 (staging en Render; efímero en Codespaces); estado de Terraform en HCP Terraform (remoto, con bloqueo)

**Testing**: `terraform fmt -check` y `terraform validate` en CI; `terraform plan` en cada PR; prueba unitaria e integración de la sonda de migraciones (xUnit + `CustomWebApplicationFactory`); prueba de humo de despliegue (sondeo de `/health/ready`)

**Target Platform**: GitHub Codespaces (Linux x64), Render (contenedores), GitHub Actions `ubuntu-latest`

**Project Type**: Infraestructura y entrega continua de una aplicación web existente

**Performance Goals**: Codespace listo en ≤ 15 min (SC-001); despliegue verificado en ≤ 20 min (SC-003); reversión en ≤ 10 min (SC-004)

**Constraints**: Cero secretos en el repo; producción no se toca; los planes gratuitos duermen las instancias (timeout de sondeo ≥ 150 s); ninguna aplicación de Terraform sin aprobación en un GitHub Environment

**Scale/Scope**: 1 ambiente (staging), 2 proveedores (Render, GitHub), 5 workflows nuevos, 1 sonda nueva

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación | Resultado |
|---|---|---|
| I. Spec y plan aprobados | Plan y tareas para aprobación; `terraform apply` además exige aprobación en un environment | PASA |
| II. Capas del backend | La sonda `MigrationsHealthCheck` va en `Services/` y se registra en `ApplicationServiceExtensions.cs`, igual que `DatabaseHealthCheck` | PASA |
| III. DTOs y autorización | Sin endpoints nuevos; `/health/*` siguen anónimos y sin detalles internos | PASA |
| IV. Esquema por migración | No cambia el esquema; cambia **cuándo** se aplica la migración (sigue siendo `efbundle`); sin operaciones destructivas; la base de staging con protección contra borrado | PASA |
| V. Pruebas obligatorias | Prueba unitaria y de integración de la sonda; validación de Terraform en CI | PASA |
| VI. Calidad automática | CSharpier y analizadores para el C#; `terraform fmt -check` para el HCL | PASA |
| VII. Stack del frontend | Sin cambios de frontend | N/A |
| Seguridad y datos | Secretos en GitHub Secrets / Environments / Codespaces y en el estado remoto; escaneo de secretos en CI (SC-005) | PASA |
| Flujo de trabajo | Ramas `ci/` y `chore/`; los workflows no hacen commits ni merges | PASA |

**Re-evaluación tras el diseño (Phase 1):** sin cambios. Los nombres de recursos del proveedor de
GitHub se confirmarán contra la versión fijada (research R4).

## Project Structure

### Documentation (this feature)

```text
specs/003-infraestructura-como-codigo/
├── plan.md
├── research.md
├── data-model.md          # ambientes, secretos, despliegue
├── quickstart.md
├── contracts/
│   ├── infra-module-interface.md   # entradas y salidas comunes por proveedor
│   ├── workflows.md                # disparadores, secretos, concurrencia, salidas
│   └── devcontainer.md             # puertos, secretos, comandos de ciclo de vida
├── checklists/            # requirements.md, operacion.md
└── tasks.md
```

### Source Code (repository root)

```text
.devcontainer/
├── devcontainer.json        # imagen, features, puertos 3000/5000, secretos recomendados
├── docker-compose.yml       # servicio "app" + postgres:18-alpine
└── post-create.sh           # herramientas, .env desde secretos, migraciones, Chromium
infra/
├── README.md
├── modules/
│   ├── render-stack/        # main.tf · variables.tf · outputs.tf
│   └── github-platform/     # main.tf · variables.tf · outputs.tf
└── envs/
    └── staging/             # main.tf · providers.tf · backend.tf · variables.tf · terraform.tfvars.example
.github/workflows/
├── infra-plan.yml           # PR que toca infra/: fmt, validate, plan como comentario
├── infra-apply.yml          # manual; environment "infra" con revisión obligatoria
├── deploy-staging.yml       # push a dev: imagen a GHCR por SHA, despliegue, sondeo
├── promote-production.yml   # manual; environment "production" con revisión
└── secret-scan.yml          # escaneo de secretos en cada PR
entrypoint.sh                # migración no fatal
backend/SistemaServicios.API/Services/MigrationsHealthCheck.cs
backend/SistemaServicios.API/Extensions/ApplicationServiceExtensions.cs   # registro de la sonda
backend/SistemaServicios.Tests/Unit/MigrationsHealthCheckTests.cs
backend/SistemaServicios.Tests/Integration/MigrationsReadinessTests.cs
README.md                    # flujo commit → versión en marcha; crear y destruir staging
```

**Structure Decision**: Nueva carpeta `infra/` en la raíz (estándar raíz + módulos) y
`.devcontainer/` en la raíz, como exige Codespaces. Los workflows nuevos conviven con los cinco
existentes sin modificarlos; `docker-image.yml` sigue siendo la verificación de la imagen en PR.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| Dos proveedores de Terraform (Render + GitHub) para un solo ambiente | Requisito explícito "multivendor" (FR-015, US4) | Un solo proveedor no demuestra portabilidad |
| Registro de imágenes (GHCR) además de Render | Trazabilidad por SHA (FR-016) y reversión inmediata (FR-012) | El deploy hook sin imagen no permite volver a un SHA concreto |
