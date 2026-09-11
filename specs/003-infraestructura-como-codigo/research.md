# Research: Software como infraestructura

## R1. Entorno de trabajo en la nube

- **Decision**: GitHub Codespaces con `.devcontainer/` basado en Docker Compose: un servicio `app`
  (imagen base de devcontainers con las features de .NET 9 y Node 24) y un servicio `db`
  (`postgres:18-alpine`, la misma versión que producción y que `docker-image.yml`).
- **Rationale**: Elegido por el responsable. Compose permite tener PostgreSQL real (las pruebas de
  integración usan InMemory, pero la API y las migraciones necesitan PostgreSQL). Los puertos 3000
  y 5000 se reenvían; en el codespace, `NEXT_PUBLIC_ALLOWED_PATH` y `ALLOWED_ORIGINS` se calculan a
  partir de `CODESPACE_NAME` y `GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN`.
- **Alternatives considered**: Gitpod u otros entornos (fuera de la decisión); instalar PostgreSQL
  dentro del contenedor principal (mezcla responsabilidades y complica las actualizaciones).

## R2. Secretos del entorno de trabajo

- **Decision**: Declarar en `devcontainer.json` los secretos recomendados (`JWT_KEY`, `SMTP_HOST`,
  `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `SMTP_FROM`); `post-create.sh` escribe el `.env`
  (ignorado por git) con esos valores más los locales del codespace (`DB_*` apuntando a `db`), y
  termina con un mensaje claro si falta un secreto obligatorio (FR-002).
- **Rationale**: La aplicación falla al arrancar si faltan JWT o SMTP (decisión deliberada del
  proyecto); mejor detectarlo al crear el entorno.

## R3. Herramienta de infraestructura y estado

- **Decision**: Terraform 1.x con estado remoto en **HCP Terraform** (plan gratuito), con bloqueo
  de estado y el token en el secreto `TF_API_TOKEN`.
- **Rationale**: El estado no puede vivir en el repo (FR-006); HCP Terraform da bloqueo sin
  necesidad de una cuenta de nube adicional.
- **Alternatives considered**: Backend S3 o Azure Blob (exigen otra cuenta de nube); estado local
  (prohibido por FR-006); OpenTofu (compatible, pero Terraform fue la herramienta pedida).

## R4. Proveedores de Terraform

- **Decision**:
  - `render-oss/render`: `render_project`, `render_postgres`, `render_env_group`,
    `render_web_service` (API desde imagen de GHCR y frontend Node). Autenticación con
    `RENDER_API_KEY` y `RENDER_OWNER_ID`.
  - `integrations/github`: ambientes del repositorio (`staging`, `production`, `infra`) con
    revisores obligatorios, y secretos de Actions, de ambiente y de Codespaces declarados por
    nombre; los valores se inyectan como variables sensibles desde el workflow, nunca desde el
    repositorio.
- **Pendiente de verificar (T004)**: nombres exactos de los recursos del proveedor de GitHub en la
  versión fijada (se esperan `github_repository_environment`, `github_actions_secret`,
  `github_actions_environment_secret` y `github_codespaces_secret`); no se pudo confirmar con la
  documentación disponible al planear.
- **Rationale**: Render es el proveedor de producción; GitHub es el segundo proveedor elegido
  (Codespaces y la plataforma de CI).

## R5. Mecanismo de despliegue y reversión

- **Decision**: `deploy-staging.yml` construye la imagen, la publica en GHCR como
  `ghcr.io/<owner>/gsp-api:<sha>` y actualiza el servicio de staging para usar esa imagen. La
  reversión redespliega la etiqueta del SHA anterior (`workflow_dispatch` con el SHA como entrada).
- **Rationale**: FR-016 (trazable al commit) y FR-012 (reversión probada). El issue #189 dejó
  anotado que publicar en un registro solo aporta si Render tira de él: con un servicio basado en
  imagen, eso ocurre.
- **Pendiente de verificar (T020)**: si la actualización de la imagen se hace mejor con el deploy
  hook de Render o con `terraform apply -var image_tag=<sha>` sobre el servicio de staging. El
  plan admite ambos; se elige el que permita el sondeo posterior en ≤ 20 min.

## R6. Disparo solo con CI en verde

- **Decision**: Proteger `dev` exigiendo los checks existentes (`backend-lint`, `backend-tests`,
  `frontend-tests`, `docker-image`) antes del merge; `deploy-staging.yml` corre en `push` a `dev`.
  Como no se puede integrar sin checks en verde, el despliegue hereda esa garantía.
- **Alternatives considered**: `workflow_run` encadenado a cuatro workflows (frágil: dispara una
  vez por cada workflow y exige coordinarlos).

## R7. Migración desacoplada del arranque (issue #189)

- **Decision**: En `entrypoint.sh`, ejecutar `efbundle` sin abortar el script si falla (registrar
  el error y continuar con `exec dotnet …`), y agregar `MigrationsHealthCheck` a la sonda de
  readiness, que responde *Unhealthy* mientras haya migraciones pendientes.
- **Rationale**: Hoy `set -e` hace que un fallo de migración deje el contenedor muerto sin
  escuchar (confirmado en el borrador del issue #189). Con el cambio, `/health/live` responde 200 y
  `/health/ready` responde 503: el fallo es legible desde fuera y el despliegue falla de forma
  visible.
- **Riesgo**: con varias réplicas, todas intentarían migrar; staging tiene una sola réplica.
  Documentado para producción.

## R8. Escaneo de secretos

- **Decision**: `secret-scan.yml` con gitleaks sobre el diff del PR.
- **Rationale**: SC-005 independientemente de la visibilidad del repositorio (el escaneo nativo de
  GitHub depende del plan y de la visibilidad).
- **Hallazgo relacionado**: `.env.example` contiene un `JWT_KEY` con forma de clave real; se
  reemplazará por un marcador antes de activar el escaneo, para que no bloquee.

## R9. Frontend en staging

- **Decision**: Servicio web Node en Render que ejecuta `npm run build && npm run start` desde
  `frontend/`, con `NEXT_PUBLIC_ALLOWED_PATH` apuntando a la URL de la API de staging.
- **Hallazgo relacionado**: `next.config.ts` pasa todo el `.env` a `env`. En staging solo se
  definirán variables `NEXT_PUBLIC_*` en el servicio del frontend, y la corrección del
  `next.config.ts` se registra como brecha B6.
