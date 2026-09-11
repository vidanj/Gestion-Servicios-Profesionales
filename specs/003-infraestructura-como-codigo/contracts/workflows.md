# Contrato: workflows de infraestructura y despliegue

| Workflow | Disparador | Environment | Concurrencia | Secretos | Resultado |
|---|---|---|---|---|---|
| `infra-plan.yml` | `pull_request` a `dev`/`main` con cambios en `infra/**` | — (solo lectura) | `infra-plan-${{ github.ref }}`, cancela en curso | `TF_API_TOKEN`, `RENDER_API_KEY`, `RENDER_OWNER_ID`, `GH_ADMIN_TOKEN` | `fmt -check`, `validate` y `plan` publicado como comentario del PR |
| `infra-apply.yml` | `workflow_dispatch` (entrada: `environment`) | `infra` (revisión obligatoria) | `infra-apply`, **sin** cancelar | Los anteriores + valores de `api_secrets` | `terraform apply` del plan aprobado |
| `deploy-staging.yml` | `push` a `dev`; `workflow_dispatch` con entrada `sha` (reversión) | `staging` | `deploy-staging`, **sin** cancelar | `RENDER_API_KEY`, `GITHUB_TOKEN` | Imagen `gsp-api:<sha>` en GHCR, despliegue y sondeo de `health_url` |
| `promote-production.yml` | `workflow_dispatch` (entrada: `sha`) | `production` (revisión obligatoria) | `deploy-production`, **sin** cancelar | Por definir al importar producción | Fuera de alcance en esta iteración (esqueleto documentado) |
| `secret-scan.yml` | `pull_request` | — | `secret-scan-${{ github.ref }}` | — | Falla si detecta un secreto en el diff |

## Verificación de salud (`deploy-staging.yml`)

- Sondea `health_url` cada 10 s durante un máximo de **300 s** (el arranque en frío del plan
  gratuito dura decenas de segundos; `docker-image.yml` ya usa 150 s en local).
- Éxito: el cuerpo contiene `"status":"Healthy"`.
- Fallo: el job termina en rojo e imprime la última respuesta y la URL de los logs del servicio.
- Nunca imprime secretos ni la cadena de conexión.

## Reversión

```text
Actions → deploy-staging → Run workflow → sha = <sha anterior sano>
```

La imagen de ese SHA ya existe en GHCR; no se reconstruye. **Las migraciones destructivas no se
revierten con la imagen** (documentado en el README).
