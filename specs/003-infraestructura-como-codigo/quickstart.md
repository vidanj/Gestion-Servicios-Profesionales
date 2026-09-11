# Quickstart: validar la infraestructura como código

## US1 — Entorno en Codespaces (P1)

1. En GitHub: **Settings → Codespaces → Secrets**, crear `JWT_KEY` y `SMTP_*` para el repositorio.
2. **Code → Codespaces → Create codespace on `dev`**. Cronometrar hasta que termine `post-create.sh`.
3. En la terminal del codespace:

   ```bash
   dotnet run --project backend/SistemaServicios.API &
   curl -s localhost:5000/health/ready        # → {"status":"Healthy",…}
   cd backend && dotnet test                   # → en verde
   cd ../frontend && npm test                  # → en verde
   ```

4. Abrir la pestaña **Ports** y el puerto 3000: la aplicación carga y el login funciona contra la API del codespace.

**Esperado:** ≤ 15 minutos desde el paso 2 (SC-001). Si falta un secreto, `post-create.sh` lo nombra.

## US2 — Staging declarativo (P2)

```powershell
terraform -chdir=infra/envs/staging init
terraform -chdir=infra/envs/staging fmt -check -recursive
terraform -chdir=infra/envs/staging validate
terraform -chdir=infra/envs/staging plan
```

- Abrir un PR con un cambio en `infra/`: el comentario de `infra-plan` muestra el plan.
- Ejecutar **Actions → infra-apply** (environment `infra`, requiere aprobación). Esperado: staging creado en ≤ 30 min (SC-002).
- Cambiar a mano una variable en el panel de Render y volver a correr `plan`: debe aparecer la diferencia (SC-006).
- `terraform destroy` debe negarse a borrar la base (`prevent_destroy`).

## US3 — Despliegue continuo y reversión (P3)

1. Integrar en `dev` un cambio trivial con todos los checks en verde.
2. **Actions → deploy-staging:** imagen `gsp-api:<sha>` publicada, despliegue y sondeo en verde en ≤ 20 min (SC-003).
3. Forzar una versión rota (por ejemplo, una variable obligatoria vacía en una rama de prueba desplegada a mano) y comprobar que el job falla de forma visible.
4. **Reversión:** ejecutar `deploy-staging` con `sha` = el último sano. Esperado: staging sano en ≤ 10 min (SC-004).
5. **Migración fallida:** con una base inalcanzable, `/health/live` responde 200 y `/health/ready` responde 503; el contenedor no muere.

## US4 — Multi-proveedor (P4)

- Tras `infra-apply`, en **Settings → Environments** existen `staging`, `production` (con revisores) e `infra`.
- En **Settings → Secrets**, los nombres de secretos de Actions y Codespaces coinciden con `data-model.md`.
- Revisar que `infra/modules/render-stack` cumple la [interfaz común](contracts/infra-module-interface.md).
