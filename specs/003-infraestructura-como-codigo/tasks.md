---

description: "Task list for 003-infraestructura-como-codigo"
---

# Tasks: Software como infraestructura

**Input**: Design documents from `/specs/003-infraestructura-como-codigo/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: La sonda nueva exige prueba unitaria y de integración (Principio V); la infraestructura se valida con `fmt`, `validate`, `plan` y las pruebas de humo del quickstart.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Crear `infra/envs/staging/providers.tf` con `required_version` de Terraform y versiones fijadas de `render-oss/render` e `integrations/github`
- [ ] T002 [P] (Manual, administrador) Crear en HCP Terraform la organización y el workspace `gsp-staging` en modo de ejecución local, y guardar `TF_API_TOKEN` en el environment `infra` de GitHub
- [ ] T003 [P] Reemplazar el `JWT_KEY` de ejemplo de `.env.example` por un marcador (`cambiar-por-una-clave-de-al-menos-32-caracteres`) (R8)
- [ ] T004 Confirmar los nombres de recursos del proveedor `integrations/github` en la versión fijada y actualizar `specs/003-infraestructura-como-codigo/research.md` (R4) y `contracts/infra-module-interface.md` si difieren

---

## Phase 2: Foundational (Blocking Prerequisites)

- [ ] T005 Crear `infra/envs/staging/backend.tf` con el backend remoto de HCP Terraform (workspace `gsp-staging`) (FR-006)
- [ ] T006 [P] Crear `.github/workflows/secret-scan.yml` con gitleaks sobre el diff de cada PR (SC-005)
- [ ] T007 [P] (Manual, administrador) Proteger `dev` exigiendo `backend-lint`, `backend-tests`, `frontend-tests` y `docker-image` antes del merge (R6)

**Checkpoint**: estado remoto, escaneo de secretos y rama protegida.

---

## Phase 3: User Story 1 - Entorno de trabajo completo en la nube (Priority: P1) 🎯 MVP

**Goal**: Codespace con API, base de datos y frontend en ≤ 15 min.

**Independent Test**: crear un codespace desde `dev` y ejecutar el quickstart de US1.

- [ ] T008 [P] [US1] Crear `.devcontainer/docker-compose.yml` con los servicios `app` y `db` (`postgres:18-alpine`, volumen con nombre) según [devcontainer.md](contracts/devcontainer.md)
- [ ] T009 [P] [US1] Crear `.devcontainer/devcontainer.json`: features .NET 9 y Node 24, `forwardPorts` 3000 y 5000 con etiquetas, `secrets` recomendados (`JWT_KEY`, `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `SMTP_FROM`), `postCreateCommand` y `postStartCommand` (FR-001)
- [ ] T010 [US1] Crear `.devcontainer/post-create.sh` (LF): `dotnet tool restore`, `dotnet restore backend`, `npm ci` en `frontend/`, `npx playwright install --with-deps chromium`, generación del `.env` con `DB_*` locales y `NEXT_PUBLIC_ALLOWED_PATH`/`ALLOWED_ORIGINS` calculados, salida con el nombre del secreto faltante y `dotnet ef database update` (FR-002, FR-003)
- [ ] T011 [US1] Validar en un codespace real el quickstart de US1 y registrar el tiempo en `specs/003-infraestructura-como-codigo/quickstart.md` (SC-001)

**Checkpoint**: MVP entregable sin tocar Render.

---

## Phase 4: User Story 2 - Staging declarativo y reproducible (Priority: P2)

**Goal**: staging creado, modificado y destruido solo por código revisado.

**Independent Test**: `plan` en un PR, `infra-apply` aprobado y recreación idéntica.

- [ ] T012 [P] [US2] Crear `infra/modules/render-stack/variables.tf` con la [interfaz común](contracts/infra-module-interface.md) (`api_secrets` marcado `sensitive`)
- [ ] T013 [US2] Crear `infra/modules/render-stack/main.tf`: `render_project`, `render_postgres` con `lifecycle { prevent_destroy = true }`, `render_env_group`, `render_web_service` de la API desde `api_image` y `render_web_service` del frontend (Node, raíz `frontend`, solo variables `NEXT_PUBLIC_*`) (FR-004, FR-007, R9)
- [ ] T014 [P] [US2] Crear `infra/modules/render-stack/outputs.tf` con `api_url`, `frontend_url`, `health_url` y `database_id`
- [ ] T015 [US2] Crear `infra/envs/staging/main.tf`, `variables.tf` y `terraform.tfvars.example` (sin valores sensibles) que usen `render-stack`
- [ ] T016 [US2] Crear `.github/workflows/infra-plan.yml` según [workflows.md](contracts/workflows.md): `fmt -check`, `validate` y `plan` como comentario del PR (FR-005)
- [ ] T017 [US2] Crear `.github/workflows/infra-apply.yml` (`workflow_dispatch`, environment `infra` con revisión, concurrencia sin cancelar) (FR-005, FR-006)
- [ ] T018 [US2] Primera aplicación aprobada; comprobar la detección de un cambio manual en el panel (SC-006) y que `destroy` se niega a borrar la base (FR-007); registrar los tiempos en quickstart.md (SC-002)

---

## Phase 5: User Story 3 - Despliegue continuo verificado y reversible (Priority: P3)

**Goal**: merge a `dev` → staging verificado, o fallo visible, en ≤ 20 min; reversión en ≤ 10 min.

**Independent Test**: merge trivial a `dev` y reversión manual por SHA.

- [ ] T019 [P] [US3] Crear `backend/SistemaServicios.API/Services/MigrationsHealthCheck.cs`: *Unhealthy* si hay migraciones pendientes, *Healthy* si no; si el proveedor no es relacional (InMemory en pruebas) responde *Healthy*; sin detalles internos en la respuesta (R7)
- [ ] T020 [US3] Registrar `MigrationsHealthCheck` con la etiqueta de readiness en `backend/SistemaServicios.API/Extensions/ApplicationServiceExtensions.cs`, junto a `DatabaseHealthCheck` (Principio II)
- [ ] T021 [P] [US3] Crear `backend/SistemaServicios.Tests/Unit/MigrationsHealthCheckTests.cs` (pendientes → Unhealthy; ninguna → Healthy; no relacional → Healthy)
- [ ] T022 [P] [US3] Crear `backend/SistemaServicios.Tests/Integration/MigrationsReadinessTests.cs` con `CustomWebApplicationFactory`: `/health/ready` sigue respondiendo `Healthy` en el entorno de pruebas (archivo nuevo; no se modifica `HealthChecksTests.cs`)
- [ ] T023 [US3] Modificar `entrypoint.sh` para que un fallo de `efbundle` se registre y el script continúe con `exec dotnet SistemaServicios.API.dll` (issue #189, FR-013)
- [ ] T024 [US3] Prueba de concepto de R5 (deploy hook con imagen vs `terraform apply -var image_tag`) y registrar la decisión en `specs/003-infraestructura-como-codigo/research.md`
- [ ] T025 [US3] Crear `.github/workflows/deploy-staging.yml`: imagen `ghcr.io/<owner>/gsp-api:<sha>`, despliegue según T024, sondeo de `health_url` cada 10 s hasta 300 s, concurrencia `deploy-staging` sin cancelar y `workflow_dispatch` con entrada `sha` para la reversión (FR-009, FR-010, FR-011, FR-016)
- [ ] T026 [US3] Ejecutar una reversión real en staging y registrar el tiempo (FR-012, SC-004)
- [ ] T027 [US3] Crear el esqueleto `.github/workflows/promote-production.yml` con el environment `production` y revisión obligatoria, sin pasos de despliegue hasta importar producción (FR-014)

---

## Phase 6: User Story 4 - Operación multi-proveedor (Priority: P4)

- [ ] T028 [P] [US4] Crear `infra/modules/github-platform/{variables,main,outputs}.tf`: environments `staging`, `production` e `infra` con revisores, y secretos de Actions y de Codespaces por nombre, con valores de `secret_values` (FR-008)
- [ ] T029 [US4] Integrar `github-platform` en `infra/envs/staging/main.tf` y aplicar con aprobación; verificar los environments y los nombres de secretos en GitHub
- [ ] T030 [US4] Documentar en `infra/README.md` cómo añadir un proveedor que cumpla la interfaz común (FR-015)

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T031 Actualizar `README.md`: flujo de commit a versión en marcha, crear y destruir staging, reversión, y aviso de que las migraciones destructivas no se revierten con la imagen (FR-017)
- [ ] T032 Ejecutar `dotnet tool run csharpier format backend/` y `cd backend && dotnet test`; reportar la salida tal cual
- [ ] T033 Validación completa del responsable según `specs/003-infraestructura-como-codigo/quickstart.md`
- [ ] T034 Ejecutar `/speckit-converge` y cerrar las brechas que reporte

---

## Dependencies & Execution Order

- Setup → Foundational → US1 (independiente de Render) → US2 → US3 (necesita `health_url` de US2) → US4 → Polish.
- T019–T023 (sonda y `entrypoint.sh`) no dependen de Terraform y pueden hacerse en paralelo con US2.
- T002 y T007 son manuales y requieren a un administrador.

## Parallel Example: User Story 3

```text
Task: "T019 MigrationsHealthCheck.cs"
Task: "T021 MigrationsHealthCheckTests.cs"
Task: "T022 MigrationsReadinessTests.cs"
```

## Implementation Strategy

MVP = US1 (Codespaces), sin riesgo para producción. Después, staging (US2) y despliegue (US3) en ramas `ci/` separadas.
