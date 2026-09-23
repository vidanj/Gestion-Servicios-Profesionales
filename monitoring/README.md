# Entorno de monitoreo

Recolección de métricas, alarmas y tableros. Vive en su propio archivo de composición,
**aparte del de la aplicación**, porque el monitoreo tiene que seguir en pie precisamente
cuando la aplicación se cae: si compartieran proyecto, derribar la aplicación se llevaría por
delante la evidencia del incidente.

Documento completo: [`docs/monitoreo-metricas-y-alertas.md`](../docs/monitoreo-metricas-y-alertas.md).
Especificación: [`specs/008-monitoreo-metricas-alertas/`](../specs/008-monitoreo-metricas-alertas/spec.md).

## Arranque

```powershell
.\scripts\monitoreo-up.ps1          # monitoreo + aplicación
.\scripts\monitoreo-up.ps1 -SinApi  # solo el monitoreo
```

O a mano, respetando el orden:

```bash
docker compose -f monitoring/docker-compose.yml up -d                            # 1
docker compose -f docker-compose.yml -f monitoring/docker-compose.api.yml up -d   # 2
```

**El orden importa.** El monitoreo declara la red `gsp-observabilidad`, que la superposición
de la aplicación referencia como externa. Al revés, Docker falla con
`network gsp-observabilidad declared as external, but could not be found`.

Requiere `GRAFANA_ADMIN_PASSWORD` en el `.env`. Sin ella el entorno **no arranca**, y es
deliberado: así no queda instalado con la contraseña por defecto sin que nadie se entere.

## Qué levanta

| Servicio | Imagen | Puerto | Para qué |
|---|---|---|---|
| `otel-collector` | `otel/opentelemetry-collector-contrib:0.161.0` | 4317, 4318, 8889, 8888, 13133 | Recibe OTLP de la API y lo republica en formato Prometheus |
| `prometheus` | `prom/prometheus:v3.14.0` | 9090 | Recoge, evalúa reglas, dispara alarmas |
| `alertmanager` | `prom/alertmanager:v0.34.1` | 9093 | Agrupa, silencia, inhibe |
| `grafana` | `grafana/grafana-oss:13.0.2` | **3001** | Tableros |
| `blackbox` | `prom/blackbox-exporter:v0.28.0` | 9115 | Sondea las sondas de salud desde fuera |

**Grafana en 3001 y no en 3000** porque el 3000 lo ocupa el frontend de Next.js.

**La API no se raspa directamente**: no expone `/metrics` a propósito. Empuja por OTLP al
colector, y es el colector quien publica. El porqué está en
[`research.md`](../specs/008-monitoreo-metricas-alertas/research.md), decisión D1.

Versiones verificadas en Docker Hub el 2026-09-23. Ninguna etiqueta móvil;
`monitoring-stack.yml` lo comprueba en cada cambio.

## Uso

```powershell
.\scripts\generar-trafico.ps1       # puebla los tableros
.\scripts\verificar-monitoreo.ps1   # comprueba el catálogo y las 10 alarmas
.\scripts\monitoreo-down.ps1        # apaga, conservando el histórico
```

| Dónde mirar | Dirección |
|---|---|
| Tableros | http://localhost:3001 |
| Prometheus, objetivos y reglas | http://localhost:9090 |
| Alarmas | http://localhost:9093 |

## Detalles que cuesta descubrir

Cada uno de estos costó una sesión de diagnóstico; están aquí para que no cueste una segunda.

**Las métricas tardan hasta un minuto en aparecer, y no es un fallo.** Hay dos relojes y manda
el lento: la aplicación **exporta** por OTLP cada 60 s por omisión, mientras que Prometheus
**recoge** cada 15 s. Tras generar tráfico hay que esperar ese minuto antes de verificar; si no,
el catálogo sale incompleto y todo parece roto sin estarlo. Para acortarlo en pruebas:

```bash
OTEL_METRIC_EXPORT_INTERVAL=5000   # milisegundos
```

Es lo que hace el flujo de validación, y por eso allí basta con esperar 45 s.

**El sondeo externo busca el host `api`.** Así se llama el servicio de la aplicación en el
archivo de composición raíz. Si se levanta el contenedor a mano con otro nombre, el sondeo no
lo resuelve, `probe_success` queda en 0 y la alarma crítica de indisponibilidad se dispara
aunque la aplicación esté perfectamente sana. Para un contenedor con otro nombre:

```bash
docker network connect --alias api gsp-observabilidad <nombre-del-contenedor>
```

**Los nombres del tiempo de ejecución llevan sufijo `_total`.** Es
`dotnet_thread_pool_queue_length_total`, no `dotnet_thread_pool_queue_length`: el exportador
lo añade porque el instrumento subyacente es un contador. Escribirlo sin sufijo produce un
panel vacío y una alarma que nunca dispara, **sin ningún error visible**. Y en .NET 9 el
prefijo es `dotnet.*`, no `process.runtime.dotnet.*`, así que los tableros publicados escritos
para .NET 6-8 no sirven tal cual.

**La telemetría del propio colector hay que declararla.** En esta versión ya no basta con
`address: 0.0.0.0:8888`; hace falta el bloque `readers`. Sin él, el objetivo `colector`
aparece caído en Prometheus, y eso importa: un colector que descarta datos por falta de
memoria lo haría en silencio, y desde fuera parecería que la aplicación dejó de emitir.

**Una proporción sin numerador no vale cero: desaparece.** La regla de proporción de errores
lleva `or vector(0)` porque, en un sistema sano, la serie de errores 5xx sencillamente no
existe, y sin esa defensa el panel aparece vacío y la alarma queda en un limbo que parece un
fallo de configuración.

**El sondeo del servicio publicado está inactivo hasta que se configura.** Los objetivos se
descubren desde `prometheus/objetivos/render.yml`, que no existe en el repositorio. Es
deliberado: un marcador escrito en la configuración fallaría desde el primer arranque y
dispararía una alarma crítica falsa con la etiqueta de producción. Para activarlo, copiar
`render.yml.ejemplo` a `render.yml`, poner la dirección real y recargar:

```bash
curl -X POST http://localhost:9090/-/reload
```

## Recursos

Son cinco contenedores. **No conviene levantarlo a la vez que el entorno de análisis
estático**, que es igual de pesado. El colector tiene un límite de memoria declarado para que,
si se queda corto, deje de recibir en vez de morir: un colector muerto convierte un incidente
de la aplicación en dos incidentes, y el segundo tapa al primero.

## Apagado

```powershell
.\scripts\monitoreo-down.ps1           # conserva los datos
.\scripts\monitoreo-down.ps1 -Purgar   # los elimina, pidiendo confirmación
```

El histórico de métricas es evidencia; borrarlo es destructivo y por eso la purga confirma.
