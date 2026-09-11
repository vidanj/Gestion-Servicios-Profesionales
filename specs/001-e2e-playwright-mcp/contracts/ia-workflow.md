# Contrato: flujo de trabajo con el agente de IA (Playwright MCP)

## Configuración (proyecto)

`.mcp.json` en la raíz del repositorio:

```json
{
  "mcpServers": {
    "playwright": {
      "command": "npx",
      "args": ["@playwright/mcp@<versión fijada en T001>"]
    }
  }
}
```

Equivalente personal (no versionado): `claude mcp add playwright npx @playwright/mcp@<versión>`.

## Generar un borrador (FR-008)

1. Levantar la app en local (`cd frontend && npm run dev`) **sin** otro proceso en el puerto 3000.
2. Pedir al agente, con el escenario en *Given/When/Then* del spec:

   ```text
   Usa el servidor MCP de Playwright para explorar http://localhost:3000<ruta>.
   Escenario: <copiar el escenario del spec>.
   Escribe un borrador en frontend/e2e-drafts/<nombre>.spec.ts que:
   - use los fixtures de frontend/tests/fixtures y los ayudantes de frontend/tests/support,
   - simule la API solo con page.route según specs/001-e2e-playwright-mcp/contracts/mock-api.md,
   - localice elementos por data-testid o getByRole,
   - no use esperas fijas (waitForTimeout).
   No modifiques archivos fuera de frontend/e2e-drafts/.
   ```

3. Revisar el borrador con la [lista de revisión](#revisión-humana-fr-009), moverlo a
   `frontend/tests/` y ejecutarlo con `--repeat-each=3`.

## Diagnosticar una prueba rota (US4-2)

```text
La prueba "<título>" de frontend/tests/<archivo> falla con: <salida>.
Usa el servidor MCP de Playwright para reproducirla y dime si la causa es (a) un cambio de la
interfaz, (b) un fixture desactualizado o (c) un defecto real. No edites la prueba.
```

## Revisión humana (FR-009)

- [ ] Cubre exactamente el escenario del spec, citado en el título o en un comentario.
- [ ] Usa fixtures y ayudantes compartidos; no hay datos inline duplicados.
- [ ] Selectores por `data-testid` o rol; sin CSS ni XPath.
- [ ] Sin `waitForTimeout`, `.only` ni `.skip`.
- [ ] Pasa tres veces seguidas en local.
- [ ] Si falla por comportamiento real, se abre un issue BUG y la prueba **no** se ajusta.
