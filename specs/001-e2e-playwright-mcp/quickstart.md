# Quickstart: validar las pruebas E2E

## Antes de empezar

Playwright reutiliza el servidor que encuentre en el puerto 3000 y puede servir código viejo.
Comprobarlo y liberarlo antes y después de las pruebas:

```powershell
Get-NetTCPConnection -LocalPort 3000 -State Listen -ErrorAction SilentlyContinue |
  ForEach-Object { Stop-Process -Id $_.OwningProcess -Confirm:$false }
```

## Ejecutar

```powershell
cd frontend
npx playwright test tests/catalogo.spec.ts tests/solicitar-servicio.spec.ts   # US1
npx playwright test tests/profesionista.spec.ts                                # US2
npx playwright test tests/solicitudes.spec.ts                                  # US3
npx playwright test                                                             # suite completa
npx playwright test --repeat-each=5                                            # SC-004 (determinismo)
npm run test:report                                                            # reporte HTML
```

Esperado: todo en verde, sin backend ni base de datos en marcha. Si un escenario falla porque la
aplicación se comporta distinto al spec, **no** se ajusta la prueba: se abre un issue BUG con la
salida y la traza (FR-007).

## Agente de IA (US4)

```powershell
claude mcp list        # debe listar "playwright" (desde .mcp.json)
```

Seguir [contracts/ia-workflow.md](contracts/ia-workflow.md) con un escenario nuevo y cronometrar
hasta tener el borrador revisado (SC-005: < 30 min).

## CI

Abrir un PR con cambios en `frontend/`: el workflow **Frontend — Compilación y Pruebas E2E** debe
pasar en menos de 10 minutos (SC-003) y publicar `playwright-report` como artefacto si falla.

## Registro de resultados

| Fecha | Suite completa | `--repeat-each=5` | Duración en CI | Borrador con IA (min) | Defectos abiertos |
|---|---|---|---|---|---|
| — | — | — | — | — | — |
