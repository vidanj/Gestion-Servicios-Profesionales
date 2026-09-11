# Casos de prueba (notación Katalon / Selenium)

> **Informe de planeación** (2026-09-11). Los casos se escriben con la notación de **Katalon Studio**
> (palabras clave `WebUI.*`) y de **Selenium IDE** (`open`, `type`, `click`, `assertText`) para que
> sean portables entre herramientas. **Se ejecutarán con Playwright**, la herramienta autorizada por
> la constitución (Principio V); Katalon y Selenium no se instalan.
>
> Origen de los casos: [plan de pruebas vigente](../../backend/docs/test-plan.md) (CP-SEC-01…03,
> CP-E2E-01), [spec 001](../../specs/001-e2e-playwright-mcp/spec.md) (CP-E2E-02…20) y
> [spec 002](../../specs/002-guia-interactiva-tours/spec.md) (CP-TOUR-01…05).

## 1. Convenciones

- **Objetos de prueba** (Katalon *Test Objects* / localizadores de Selenium): selector CSS
  `[data-testid="<id>"]` o `[data-tour="<id>"]`. Los identificadores provienen de las tareas de las
  specs 001 (T011, T012, T016, T019) y 002 (contrato de anclajes).
- **Datos:** fixtures de la spec 001 (`servicioActivo`, `catalogoVacio`, sesiones `*.test`).
- **Simulación de la API:** en Playwright con `page.route`. Con Katalon o Selenium haría falta un
  servidor de simulación aparte (por ejemplo, WireMock o json-server), porque ninguna de las dos
  intercepta la red de forma nativa. Es una de las razones para ejecutar con Playwright.
- **Prioridad:** Alta = flujo de valor o seguridad; Media = validación y errores; Baja = bordes.

## 2. Matriz de casos

| ID | Módulo | Requisito | Prioridad | Precondición | Pasos resumidos | Resultado esperado | Automatización |
|---|---|---|---|---|---|---|---|
| CP-SEC-01 | Registro | Asignación masiva | Alta | Correo no registrado | Enviar registro con `"role": "Admin"` | Rol `Client`; 201 | Unit/Integración (existente) |
| CP-SEC-02 | Login | Rate limiting | Alta | IP sin peticiones | 6 logins en 60 s | 6.ª → 429 | Integración (existente) |
| CP-SEC-03 | Perfil | Magic bytes | Alta | Autenticado | Subir un script renombrado a `.png` | 400 | Unit (existente) |
| CP-E2E-01 | Login | Autenticación | Alta | Credenciales válidas | Abrir `/login`, escribir, enviar | Token guardado y redirección a `/dashboard` | `login.spec.ts` (existente) |
| CP-E2E-02 | Catálogo | 001 US1-1 | Alta | 2 servicios activos | Abrir `/dashboard` | Cada tarjeta muestra título, categoría y precio | `catalogo.spec.ts` |
| CP-E2E-03 | Catálogo | 001 US1-2 | Media | Catálogo vacío | Abrir `/dashboard` | `catalog-empty` visible, sin error | `catalogo.spec.ts` |
| CP-E2E-04 | Solicitud | 001 US1-3 | Alta | Cliente con sesión; servicio activo | Abrir detalle → descripción → fecha → Enviar | `request-success`; solicitud Pendiente en `/solicitudes` | `solicitar-servicio.spec.ts` |
| CP-E2E-05 | Solicitud | 001 US1-4 | Media | Servicio inactivo | Abrir el detalle | `service-unavailable`; sin botón de enviar | `solicitar-servicio.spec.ts` |
| CP-E2E-06 | Solicitud | 001 US1-5 | Media | API responde 500 | Enviar la solicitud | `request-error`; el formulario conserva lo escrito | `solicitar-servicio.spec.ts` |
| CP-E2E-07 | Solicitud | 001 US1-6 | Media | API responde 401 | Enviar la solicitud | Redirección a `/login` | `solicitar-servicio.spec.ts` |
| CP-E2E-08 | Solicitud | 001 borde | Baja | Servicio activo | Doble clic en Enviar | Una sola petición `POST` | `solicitar-servicio.spec.ts` |
| CP-E2E-09 | Servicios | 001 US2-1 | Alta | Profesional con 3 servicios | Abrir `/profesionista` | Solo sus servicios, con estado | `profesionista.spec.ts` |
| CP-E2E-10 | Servicios | 001 US2-2 | Alta | Profesional | Llenar el formulario válido → Guardar | El servicio aparece en la lista | `profesionista.spec.ts` |
| CP-E2E-11 | Servicios | 001 US2-3 | Media | Profesional | Título vacío o precio `0` / `-1` → Guardar | `field-error` junto al campo; sin petición | `profesionista.spec.ts` |
| CP-E2E-12 | Servicios | 001 US2-4 | Media | Servicio activo | Pulsar `service-toggle` | Estado inactivo | `profesionista.spec.ts` |
| CP-E2E-13 | Servicios | 001 US2-5 | Media | Servicio propio | Editar → Guardar | Datos actualizados | `profesionista.spec.ts` |
| CP-E2E-14 | Servicios | 001 US2-6 | Media | Servicio propio | Eliminar → Confirmar | Desaparece de la lista | `profesionista.spec.ts` |
| CP-E2E-15 | Seguimiento | 001 US3-1 | Alta | Solicitud Pendiente | `action-accept` | Estado Aceptada | `solicitudes.spec.ts` |
| CP-E2E-16 | Seguimiento | 001 US3-2 | Alta | Solicitud Aceptada | `action-start` → `action-complete` | En progreso → Completada | `solicitudes.spec.ts` |
| CP-E2E-17 | Seguimiento | 001 US3-3 | Media | Solicitud no final | `action-cancel` | Cancelada | `solicitudes.spec.ts` |
| CP-E2E-18 | Seguimiento | 001 US3-4 | Media | Solicitud Completada | Abrir `/solicitudes` | Sin acciones de estado | `solicitudes.spec.ts` |
| CP-E2E-19 | Seguimiento | 001 US3-5 | Media | API responde 400 | Intentar la transición | `status-error` con el motivo; el estado no cambia | `solicitudes.spec.ts` |
| CP-E2E-20 | Seguimiento | 001 US3-6 | Media | Cliente con solicitudes | Abrir `/solicitudes` | Estado vigente de cada una | `solicitudes.spec.ts` |
| CP-TOUR-01 | Guía | 002 US1-1 | Alta | Cliente sin recorrido visto | Iniciar sesión | El recorrido inicia en el paso 1 | `tours.spec.ts` |
| CP-TOUR-02 | Guía | 002 US1-3 | Media | Recorrido en curso | Pulsar Esc | Se cierra, queda omitido, la pantalla no cambia | `tours.spec.ts` |
| CP-TOUR-03 | Guía | 002 US1-4 | Alta | Recorrido completado | Recargar | No se reinicia | `tours.spec.ts` |
| CP-TOUR-04 | Guía | 002 US4-1 | Media | Recorrido completado | Pulsar "Ver guía" | Inicia desde el paso 1 | `tours.spec.ts` |
| CP-TOUR-05 | Guía | 002 US2-2 | Baja | Paso sin elemento | Iniciar el recorrido | El paso se omite y el recorrido continúa | `tours.spec.ts` |

## 3. Casos detallados

### CP-E2E-04 — El cliente envía una solicitud

| Campo | Valor |
|---|---|
| Requisito | Spec 001, US1 escenario 3; FR-001 |
| Datos | `servicioActivo` (id 7, precio 450.00); sesión `cliente` |
| Precondición | La API simulada devuelve el servicio 7 y acepta `POST /api/servicerequests` con 201 |

| # | Katalon (WebUI) | Selenium IDE | Verificación |
|---|---|---|---|
| 1 | `WebUI.openBrowser('')` | — | — |
| 2 | `WebUI.navigateToUrl('http://localhost:3000/catalogo/7')` | `open /catalogo/7` | — |
| 3 | `WebUI.verifyElementText(findTestObject('Detalle/service-title'), 'Plomería')` | `assertText css=[data-testid=service-title] Plomería` | Título correcto |
| 4 | `WebUI.setText(findTestObject('Detalle/request-description'), 'Fuga en baño')` | `type css=[data-testid=request-description] Fuga en baño` | — |
| 5 | `WebUI.setText(findTestObject('Detalle/request-date'), '2026-09-20')` | `type css=[data-testid=request-date] 2026-09-20` | — |
| 6 | `WebUI.click(findTestObject('Detalle/request-submit'))` | `click css=[data-testid=request-submit]` | — |
| 7 | `WebUI.verifyElementPresent(findTestObject('Detalle/request-success'), 10)` | `waitForElementVisible css=[data-testid=request-success] 10000` | Confirmación visible |
| 8 | `WebUI.closeBrowser()` | — | — |

Equivalente planeado en Playwright:

```ts
test('envía la solicitud y la muestra como Pendiente', async ({ page }) => {
  await loginAs(page, 'cliente');
  await mockServiceById(page, servicioActivo);
  await mockCreateRequest(page, { status: 201 });
  await page.goto('/catalogo/7');
  await page.getByTestId('request-description').fill('Fuga en baño');
  await page.getByTestId('request-date').fill('2026-09-20');
  await page.getByTestId('request-submit').click();
  await expect(page.getByTestId('request-success')).toBeVisible();
});
```

### CP-E2E-11 — Validación del formulario de servicio

| # | Katalon (WebUI) | Selenium IDE | Verificación |
|---|---|---|---|
| 1 | `WebUI.navigateToUrl('http://localhost:3000/profesionista')` | `open /profesionista` | — |
| 2 | `WebUI.setText(findTestObject('Servicio/service-title-input'), '')` | `type css=[data-testid=service-title-input] ` | — |
| 3 | `WebUI.setText(findTestObject('Servicio/service-price-input'), '-1')` | `type css=[data-testid=service-price-input] -1` | — |
| 4 | `WebUI.click(findTestObject('Servicio/service-submit'))` | `click css=[data-testid=service-submit]` | — |
| 5 | `WebUI.verifyElementPresent(findTestObject('Servicio/field-error'), 5)` | `assertElementPresent css=[data-testid=field-error]` | Error junto al campo |

Regla de origen: `CreateServiceDto` exige título (≤ 200) y `BasePrice ≥ 0.01`. Además se verifica que no
salió ninguna petición `POST /api/services`.

### CP-E2E-19 — Transición de estado rechazada

| # | Katalon (WebUI) | Selenium IDE | Verificación |
|---|---|---|---|
| 1 | `WebUI.navigateToUrl('http://localhost:3000/solicitudes')` | `open /solicitudes` | — |
| 2 | `WebUI.click(findTestObject('Solicitudes/action-accept'))` | `click css=[data-testid=action-accept]` | — |
| 3 | `WebUI.verifyElementText(findTestObject('Solicitudes/status-error'), 'Transición de estado inválida')` | `assertText css=[data-testid=status-error] *Transición de estado inválida*` | Mensaje del servidor |
| 4 | `WebUI.verifyElementText(findTestObject('Solicitudes/request-status'), 'Pendiente')` | `assertText css=[data-testid=request-status] Pendiente` | Estado sin cambio |

### CP-TOUR-01 — Inicio automático del recorrido del cliente

| # | Katalon (WebUI) | Selenium IDE | Verificación |
|---|---|---|---|
| 1 | `WebUI.executeJavaScript("localStorage.clear()", null)` | `executeScript localStorage.clear()` | Navegador limpio |
| 2 | Iniciar sesión como cliente (caso CP-E2E-01) | Ídem | — |
| 3 | `WebUI.verifyElementPresent(findTestObject('Tour/popover'), 10)` | `waitForElementVisible css=.driver-popover 10000` | Recorrido visible |
| 4 | `WebUI.verifyElementText(findTestObject('Tour/progress'), '1 de 6')` | `assertText css=.driver-popover-progress-text 1 de 6` | Paso 1 |

## 4. Proyecto de Katalon equivalente (referencia)

Si en el futuro se aprobara una enmienda para usar Katalon, la estructura sería:

```text
Katalon/
├── Object Repository/   Catalogo/, Detalle/, Servicio/, Solicitudes/, Tour/   (selectores data-testid / data-tour)
├── Test Cases/          CP-E2E-02 … CP-E2E-20, CP-TOUR-01 … 05
├── Test Suites/         TS-Cliente, TS-Profesional, TS-Seguimiento, TS-Guia
└── Profiles/            default (baseUrl = http://localhost:3000)
```

## 5. Trazabilidad

Cada caso cita su escenario (`US#-n`) y su requisito (`FR-###`); la tabla escenario → prueba está en
el [data-model de la spec 001](../../specs/001-e2e-playwright-mcp/data-model.md). Los casos nuevos se
agregarán a `backend/docs/test-plan.md` en la tarea T026 de la spec 001.
