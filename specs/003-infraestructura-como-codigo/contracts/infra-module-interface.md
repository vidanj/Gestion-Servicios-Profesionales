# Contrato: interfaz común de módulos de infraestructura

Todo módulo de proveedor en `infra/modules/<proveedor>-stack/` que ejecute la aplicación expone
las mismas entradas y salidas (FR-015). Añadir un proveedor consiste en escribir otro módulo con
esta interfaz; `infra/envs/<ambiente>/main.tf` solo cambia la fuente del módulo.

## Entradas

| Variable | Tipo | Requerida | Descripción |
|---|---|:--:|---|
| `environment` | `string` | Sí | `staging` o `production` |
| `name_prefix` | `string` | Sí | Prefijo de recursos, p. ej. `gsp` |
| `region` | `string` | Sí | Región del proveedor |
| `api_image` | `string` | Sí | Referencia completa de la imagen (`ghcr.io/…:<sha>`) |
| `frontend_repo_path` | `string` | Sí | Ruta del frontend en el repo (`frontend`) |
| `api_settings` | `map(string)` | Sí | Variables no sensibles de la API |
| `api_secrets` | `map(string)`, `sensitive` | Sí | Variables sensibles de la API (JWT, SMTP) |
| `database_plan` | `string` | No | Plan de la base (por defecto, el más económico) |
| `service_plan` | `string` | No | Plan de los servicios |

## Salidas

| Salida | Descripción |
|---|---|
| `api_url` | URL pública de la API |
| `frontend_url` | URL pública del frontend |
| `health_url` | `${api_url}/health/ready` (la usa el despliegue para verificar) |
| `database_id` | Identificador de la base en el proveedor |

## Módulo de plataforma (`infra/modules/github-platform/`)

| Entrada | Tipo | Descripción |
|---|---|---|
| `repository` | `string` | `owner/repo` |
| `environments` | `map(object({ reviewers = list(string), protected = bool }))` | `staging`, `production`, `infra` |
| `actions_secret_names` | `set(string)` | Nombres de secretos de Actions a crear |
| `codespaces_secret_names` | `set(string)` | Nombres de secretos recomendados en el devcontainer |
| `secret_values` | `map(string)`, `sensitive` | Valores provistos por el workflow `infra-apply` |

## Reglas

- Ningún módulo lee archivos `.env` ni contiene valores sensibles por defecto.
- Los recursos de datos llevan `prevent_destroy`.
- `terraform fmt -check` y `terraform validate` deben pasar para todos los módulos.
