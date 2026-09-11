# Quickstart: validar la guía interactiva

## Prerrequisitos

- API y frontend en marcha (`dotnet run --project backend/SistemaServicios.API` y `cd frontend && npm run dev`)
- Un usuario por rol (cliente, profesional, administrador)
- Nada escuchando en el puerto 3000 antes de correr las pruebas (Playwright reutiliza el servidor que encuentre)

## Validación automática

```powershell
cd frontend
npx playwright test tests/tours.spec.ts
```

Esperado: en verde los escenarios de inicio automático, omitir, no repetir, relanzar y paso omitido
por elemento ausente (FR-013).

## Validación manual (la hace el responsable)

| # | Qué abrir | Qué hacer | Qué debe verse |
|---|---|---|---|
| 1 | Ventana privada → `http://localhost:3000/login` | Entrar como **cliente** | Tras llegar al catálogo, el recorrido del cliente inicia solo en el paso 1 de 6 |
| 2 | Mismo recorrido | Usar → y ← | Cada paso resalta un elemento distinto y el progreso cambia |
| 3 | Mismo recorrido | Pulsar Esc en el paso 3 | El recorrido se cierra y la pantalla queda igual que antes |
| 4 | Recargar la página | — | El recorrido **no** vuelve a iniciar |
| 5 | Navegación | Pulsar "Ver guía" | El recorrido inicia desde el paso 1 |
| 6 | DevTools → Application → Local Storage | Borrar la clave `gsp-tour:cliente:v1:<correo>` y recargar | El recorrido vuelve a iniciar |
| 7 | Cerrar sesión y entrar como **profesional** | — | Inicia el recorrido del profesional, no el del cliente |
| 8 | Entrar como **administrador** | — | Inicia el recorrido de administración |
| 9 | DevTools → modo responsive a 375 px | Pulsar "Ver guía" | Los pasos se muestran centrados, sin resaltado, y se leen completos |
| 10 | Tema oscuro | Pulsar "Ver guía" | Textos legibles y colores coherentes con la aplicación |

## Prueba de usabilidad (SC-001)

Cinco personas por rol, sin explicación previa, en un navegador limpio. Tarea tras el recorrido:
cliente → enviar una solicitud; profesional → publicar un servicio; administrador → abrir la
bitácora. Éxito si al menos 4 de 5 lo logran sin ayuda.
