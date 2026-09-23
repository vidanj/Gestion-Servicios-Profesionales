# Plan — Spec 008, monitoreo de la aplicación y despliegue verificado

> **Entrada:** [spec.md](spec.md) y la [constitución](../../.specify/memory/constitution.md) v1.1.0
> **Salida:** este plan, [research.md](research.md), [data-model.md](data-model.md),
> [contracts/](contracts/metricas.md), [quickstart.md](quickstart.md)
>
> Este documento decide el **cómo**. El **qué** y el **por qué** están en el spec.

---

## 1. Contexto técnico

| Aspecto | Decisión |
|---|---|
| Recolección | OpenTelemetry, ya presente en el backend; no se cambia de marco |
| Exposición | OpenTelemetry Collector (distribución *contrib*), que traduce OTLP a formato Prometheus |
| Almacenamiento de series | Prometheus, con volumen nombrado |
| Alertado | Alertmanager, con receptor de consola por omisión |
| Visualización | Grafana, aprovisionada de forma declarativa |
| Sondeo externo | Blackbox exporter contra las sondas de salud ya existentes |
| Dependencias nuevas del backend | Una sola: instrumentación del tiempo de ejecución de .NET, en versión estable |
| Despliegue | GitHub Actions contra el *deploy hook* de la plataforma de alojamiento |
| Pruebas | El marco vigente para unitarias e integración; `promtool` para las reglas de alerta |
| Formato | CSharpier, StyleCop y Roslynator, como el resto del backend |

## 2. Verificación contra la constitución

| Principio | Cómo lo cumple este plan | Estado |
|---|---|---|
| I — Especificación y plan aprobados antes que el código | Los artefactos SDD se entregan y aprueban antes de implementar; el PR de documentación precede al de implementación | Cumple |
| II — Capas del backend inviolables | Las métricas de negocio se emiten **solo** desde `Services/`; el contrato vive en `Interfaces/`; el registro de la dependencia va en `ApplicationServiceExtensions.cs` y no en `Program.cs`. Ningún controlador ni repositorio se toca | Cumple |
| III — Contratos por DTO y autorización explícita | No se añade ni se modifica ningún endpoint. Las etiquetas de métrica excluyen por requisito los datos personales, en la misma línea que la redacción de secretos de los registros | Cumple |
| IV — El esquema cambia solo por migración EF | No hay cambio de esquema: ninguna migración | No aplica |
| V — Pruebas obligatorias y honestas | Cada requisito de US3 tiene prueba unitaria; el arranque y la ausencia de endpoint de métricas tienen prueba de integración; las reglas de alerta tienen sus propios casos. No se borra ni se salta ninguna prueba existente | Cumple |
| VI — Calidad automática sin silenciadores | La única dependencia nueva es estable, precisamente para no introducir advertencias en un build que las trata como errores. Si apareciera una advertencia se retira la tarea, nunca se silencia | Cumple |
| VII — Stack del frontend cerrado | No se toca el frontend | No aplica |
| Seguridad y Datos | El *deploy hook* y la contraseña de Grafana viven en secretos o en el entorno; las variables nuevas se documentan en `.env.example` con valores de ejemplo | Cumple |

**Violaciones que requieran justificación:** ninguna.

**Aclaración sobre la dependencia nueva.** El Principio VII cierra el stack del *frontend*; no
restringe paquetes del backend. Aun así, el plan añade un único paquete y solo porque es estable: la
restricción real que aplica aquí es el Principio VI, y es la que descarta el exportador de Prometheus
en proceso y la instrumentación de Entity Framework Core (ver [research.md](research.md), D1).

## 3. Estructura prevista

```text
monitoring/                                  # entorno de monitoreo, independiente de la aplicación
├── README.md
├── docker-compose.yml                       # colector, Prometheus, Alertmanager, Grafana, sondeo
├── docker-compose.api.yml                   # superposición: conecta la aplicación al colector
├── otel-collector/config.yaml
├── prometheus/
│   ├── prometheus.yml
│   ├── reglas/registros.yml                 # indicadores precalculados
│   ├── reglas/alertas.yml
│   └── pruebas/alertas_test.yml             # casos de prueba de las reglas
├── alertmanager/alertmanager.yml
├── blackbox/blackbox.yml
└── grafana/
    ├── provisioning/datasources/prometheus.yml
    ├── provisioning/dashboards/proveedor.yml
    └── dashboards/{gsp-api,gsp-negocio}.json

backend/SistemaServicios.API/
├── Interfaces/IMetricasDeNegocio.cs         # contrato (Principio II)
├── Telemetry/MetricasDeNegocio.cs           # implementación; hermana de Logging/
├── Extensions/TelemetryConfiguration.cs     # (modificado) medidores y vistas
├── Extensions/ApplicationServiceExtensions.cs  # (modificado) una línea de registro
└── Services/{AuthService,ServiceRequestService,BackupService}.cs  # (modificados)

backend/SistemaServicios.Tests/
├── Unit/EscuchaDeMetricas.cs                # ayudante, espejo de SinkDeMemoria
├── Unit/MetricasDeNegocioTests.cs
├── Unit/{AuthService,ServiceRequestService,BackupService}MetricasTests.cs
├── Unit/TelemetryConfigurationTests.cs      # (modificado)
└── Integration/MetricasTests.cs

.github/workflows/
├── deploy.yml                               # despliegue verificado y reversión
└── monitoring-stack.yml                     # validación del entorno de monitoreo

scripts/                                     # cada uno en sus dos variantes
├── monitoreo-up · monitoreo-down
├── generar-trafico · verificar-monitoreo
├── desplegar
└── README.md

docs/
├── monitoreo-metricas-y-alertas.md          # fuente del documento entregable
└── reporte/{generar_monitoreo.py,enlaces-monitoreo.json,README.md}
```

**No se toca:** `Program.cs`, `docker-compose.yml` de la raíz, ningún controlador, ningún
repositorio, ninguna migración.

## 4. Decisiones de diseño

Las decisiones y sus alternativas descartadas están en [research.md](research.md):

| # | Decisión |
|---|---|
| D1 | Colector intermedio en lugar de exportador de Prometheus dentro del proceso |
| D2 | Entorno de monitoreo en su propio archivo de composición |
| D3 | Sondeo externo como única vía de observar el servicio publicado |
| D4 | Instrumentación del tiempo de ejecución: nombres nuevos en la versión actual de la plataforma |
| D5 | El medidor del limitador de peticiones se registra explícitamente |
| D6 | Reversión indicando el commit al disparar el hook, sin interfaz de programación |
| D7 | Caducidad de series en el colector como mecanismo de detección de ausencia |
| D8 | El acuerdo de latencia hereda el umbral ya fijado por las pruebas de carga |
| D9 | El medidor de negocio es un servicio de instancia única y no puede lanzar |

## 5. Fases de ejecución

| Fase | Contenido | Criterio de salida |
|---|---|---|
| 0 — Preparación | Confirmar versiones, crear el directorio del entorno, declarar variables nuevas | Las versiones existen y los puertos están libres |
| 1 — Historia US1 | Entorno de monitoreo completo, instrumentación adicional, tablero técnico | El tablero muestra latencia, error y saturación con tráfico real |
| 2 — Historia US4 | Despliegue verificado y reversión | Un despliegue real queda sano; una reversión real se mide |
| 3 — Historia US2 | Indicadores precalculados, alertas, gestor y casos de prueba | Detener la aplicación dispara la alerta; restablecerla la resuelve |
| 4 — Historia US3 | Contrato y medidor de negocio, instrumentación de tres servicios, pruebas | Los contadores se mueven con el flujo real y sin datos personales |
| 5 — Historia US5 | Guiones y verificación automatizada del entorno | Una orden levanta; otra responde si falta algo |
| 6 — Cierre | Documento, generador, actualización de documentación previa, cierre de issues | Integración continua en verde y sin diferencias entre artefactos y código |

**Las fases 1 y 2 son independientes** y pueden ejecutarse en paralelo. La fase 1 es el mínimo
viable del monitoreo; la fase 2 lo es del despliegue. Si el trabajo se detuviera tras ambas, el caso
de estudio ya estaría cubierto en sus cinco apartados.

## 6. Riesgos

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| Dar por bueno un despliegue sondeando la instancia anterior, que responde sana | Alta | Espera antes del primer sondeo y exigencia de varias respuestas sanas consecutivas |
| Un arranque en frío se reporta como caída y las alertas dejan de creerse | Alta | Tiempos de espera calculados para absorberlo, y un caso de prueba que lo afirma |
| La dependencia nueva introduce una advertencia y rompe la compilación | Media | Se retira la tarea antes que silenciarla; el Principio VI no admite excepciones |
| Explosión de cardinalidad por una etiqueta mal elegida | Media | Dominios cerrados por requisito, caducidad de series y un criterio de éxito que la vigila |
| El parámetro de commit del hook no se comporta como se espera | Media | Se comprueba a mano antes de escribir el flujo; alternativa por interfaz de programación ya contemplada |
| Consumo de recursos del equipo con dos stacks auxiliares a la vez | Media | Límite de memoria en el colector y advertencia explícita en el README del entorno |

## 7. Lo que este plan no decide

- Dónde se aloja el monitoreo de forma permanente: aquí es local y demostrable, no una plataforma.
- Qué destino reciben las alertas en la práctica (correo, mensajería); se deja configurado y
  desactivado a propósito.
- Cómo se visualizarán las trazas y la auditoría: son los puntos 2 y 3 del caso de estudio. Este
  plan solo deja la tubería de trazas del colector creada y anotada.
- Si producción debe migrar a un plan sin arranque en frío. El monitoreo dará, por primera vez, los
  datos para tomar esa decisión.
