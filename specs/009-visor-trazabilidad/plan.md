# Plan — Spec 009, visor de trazabilidad

> **Entrada:** [spec.md](spec.md) y la [constitución](../../.specify/memory/constitution.md) v1.1.0
> **Salida:** este plan, [research.md](research.md), [data-model.md](data-model.md),
> [quickstart.md](quickstart.md)
>
> Este documento decide el **cómo**. El **qué** y el **por qué** están en el spec.

---

## 1. Contexto técnico

| Aspecto | Decisión |
|---|---|
| Punto de partida | Entorno de monitoreo de la spec 008 (rama `ci/251-monitoreo-y-despliegue`, PR #253) |
| Almacén de registros | Loki, un solo proceso, disco local |
| Almacén de trazas | Tempo 3.0, modo monolítico, disco local |
| Recolección de registros | Grafana Alloy, leyendo la salida del contenedor de la API por la API de Docker |
| Recolección de trazas | La existente: API → colector de OpenTelemetry; solo cambia el destino |
| Visualización | El Grafana de la spec 008, con dos orígenes de datos y un tablero nuevos |
| Cambios en el backend | **Ninguno** |
| Pruebas | Guion de verificación de extremo a extremo y validación de configuración en integración continua |

## 2. Verificación contra la constitución

| Principio | Cómo lo cumple este plan | Estado |
|---|---|---|
| I — Especificación y plan aprobados antes que el código | Esta planeación se entrega en su propio PR (#255) y se aprueba antes del de implementación | Cumple |
| II — Capas del backend inviolables | No se toca el backend | No aplica |
| III — Contratos por DTO y autorización explícita | No se añade ni modifica ningún endpoint | No aplica |
| IV — El esquema cambia solo por migración EF | Sin cambios de esquema | No aplica |
| V — Pruebas obligatorias y honestas | No hay código de aplicación nuevo que probar con pruebas unitarias. Lo que se añade es configuración, y se prueba por ejecución: validación de cada herramienta en integración continua y una verificación de extremo a extremo que falla si la correlación no funciona (research, D10). No se borra ni se salta ninguna prueba existente | Cumple |
| VI — Calidad automática sin silenciadores | Ninguna dependencia nueva en el backend. La configuración nueva se valida en integración continua; una validación que falle se corrige, no se desactiva | Cumple |
| VII — Stack del frontend cerrado | No se toca el frontend: el visor vive en Grafana | No aplica |
| Seguridad y Datos | Sin credenciales nuevas. El acceso al socket de Docker se documenta como riesgo aceptado para el entorno local (research, D9). La verificación comprueba que las contraseñas del tráfico sintético no llegan al almacén | Cumple, con riesgo documentado |
| Flujo de trabajo | Rama `docs/` con número de issue, PR hacia `dev` con `Resuelve #255`. README actualizado en el PR de implementación (FR-033) | Cumple |

**Violaciones que requieran justificación:** ninguna.

## 3. Estructura prevista

Solo se listan archivos nuevos (**+**) y modificados (**~**).

```text
monitoring/
├── README.md                                   ~ servicios, puertos, riesgo del socket
├── docker-compose.yml                          ~ servicios loki, tempo y alloy; tres volúmenes
├── otel-collector/config.yaml                  ~ tubería traces: debug → Tempo
├── loki/config.yaml                            + almacén de registros
├── tempo/config.yaml                           + almacén de trazas
├── alloy/config.alloy                          + recolección de registros de la API
└── grafana/
    ├── provisioning/datasources/loki.yml       + origen gsp-loki, con enlace a trazas
    ├── provisioning/datasources/tempo.yml      + origen gsp-tempo, con enlace a registros
    └── dashboards/gsp-trazabilidad.json        + tablero

scripts/
├── monitoreo-up.sh / .ps1                      ~ espera a los tres servicios nuevos
├── monitoreo-down.sh / .ps1                    ~ purga de los tres volúmenes nuevos
└── verificar-monitoreo.sh / .ps1               ~ sección de trazabilidad

.github/workflows/monitoring-stack.yml          ~ validación de configuración, uid de tableros, humo

README.md                                       ~ sección de observabilidad: dónde consultar ahora

backend/                                        (sin cambios)
```

## 4. Decisiones de diseño

Las decisiones y sus alternativas descartadas están en [research.md](research.md):

| # | Decisión |
|---|---|
| D1 | Loki y Tempo sobre el Grafana existente |
| D2 | Los registros se leen de Docker, sin tocar el backend |
| D3 | `TraceId` es metadato estructurado, no etiqueta |
| D4 | El nivel `Information` hay que suponerlo |
| D5 | Tempo 3.0 en modo monolítico con almacenamiento local |
| D6 | El receptor de Tempo no se publica en el anfitrión |
| D7 | Retención de 7 días para registros y trazas |
| D8 | Solo se recolecta la API |
| D9 | El socket de Docker: riesgo aceptado y documentado |
| D10 | La prueba que demuestra la correlación va de extremo a extremo |
| D11 | La frontera con la bitácora de auditoría |

## 5. Fases de ejecución

| Fase | Contenido | Criterio de salida |
|---|---|---|
| 0 — Preparación | Confirmar versiones, campos reales del registro, puertos, socket en Windows y subcomandos de validación | Todo lo que el resto supone está comprobado |
| 1 — US1 | Loki, Alloy y su origen de datos | Los registros del tráfico sintético se filtran por nivel en Grafana |
| 2 — US2 | Tempo, cambio del colector y su origen de datos | Una traza de la ruta de servicios muestra tramos de PostgreSQL |
| 3 — US3 | Enlaces en ambos sentidos y tablero | Desde un error se llega a su traza y de vuelta a sus registros |
| 4 — US4 | Guiones e integración continua | Verificación en verde en local y en el flujo; falla si se apaga Loki |
| 5 — Cierre | README, recorridos cronometrados, diff del backend, capturas | Los siete criterios de éxito medidos |

**Las fases 1 y 2 son independientes** y pueden avanzar en paralelo. La 3 necesita ambas.

## 6. Riesgos

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| Copiar configuración de Tempo 2.x en la 3.0 | Alta | Se escribe contra la documentación de la 3.0; la validación en integración continua la rechaza si no arranca |
| Los registros llegan sin nivel porque `@l` falta en `Information` | Alta si no se atiende | Valor por omisión explícito (research, D4), confirmado sobre una línea real en T002 |
| Enlaces que no llevan a ninguna parte por un identificador mal extraído | Media | Verificación de extremo a extremo (research, D10) |
| El socket de Docker no se monta igual en Windows | Media | Se comprueba en T004 antes de escribir la composición |
| Consumo de memoria del equipo con tres servicios más | Media | Límite declarado por servicio (FR-034) y aviso en el README del entorno |
| La spec depende de dos PR sin fusionar (#252 y #253) | Segura | El PR no se fusiona hasta que lo estén; mientras tanto su diff incluye los cambios de #253 |

## 7. Lo que este plan no decide

- Si la pantalla de auditoría enlazará directamente al visor. Es decisión de la spec 010; esta deja el
  destino preparado.
- Dónde se alojaría el visor de forma permanente. Aquí es local y demostrable, igual que el resto del
  monitoreo.
- Si en el futuro se añaden alertas sobre registros. Hoy las alertas son las de la spec 008.
