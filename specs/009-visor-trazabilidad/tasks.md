# Tareas — Spec 009, visor de trazabilidad

> **Formato:** `T### [P] [USn] Descripción — ruta (FR-###)`
> `[P]` = puede ejecutarse en paralelo con las demás tareas marcadas de la misma fase.
> Cada tarea se liga a una historia y a los requisitos que cubre (constitución, *Trazabilidad SDD*).
>
> **Entrada:** [spec.md](spec.md) · [plan.md](plan.md) · [research.md](research.md) ·
> [data-model.md](data-model.md)

---

## Fase 0 — Preparación

- [ ] T001 Confirmar en el registro de imágenes la etiqueta exacta de Loki, Tempo (rama 3.0) y Alloy, y anotarlas en el README del entorno — `monitoring/README.md` (FR-030)
- [ ] T002 [P] Capturar una línea real de la API en contenedor y confirmar los nombres de los campos del formato compacto: `@l` ausente en `Information`, `TraceId`, `SpanId` y si existen `@tr` y `@sp`; corregir el modelo de datos si difiere — `specs/009-visor-trazabilidad/data-model.md` (FR-003, FR-004)
- [ ] T003 [P] Comprobar que los puertos 3100, 3200 y 12345 están libres en el equipo de trabajo (FR-031)
- [ ] T004 [P] Comprobar que el montaje del socket de Docker funciona en Docker Desktop para Windows y en Linux (FR-032)
- [ ] T005 [P] Confirmar el subcomando de validación de configuración de Loki, Tempo y Alloy; si alguna herramienta no tiene, la validación será arrancarla y consultar su sonda (FR-027)

**Punto de control:** todo lo que las fases siguientes suponen está comprobado.

## Fase 1 — US1: registros (P1, mínimo viable)

- [ ] T006 [US1] Configurar Loki: un solo proceso, disco local, esquema con índice TSDB, metadatos estructurados permitidos, retención de 7 días con compactador activo — `monitoring/loki/config.yaml` (FR-004, FR-008)
- [ ] T007 [US1] Configurar Alloy: descubrimiento por la API de Docker, filtro del servicio `api` por etiqueta de composición o nombre de contenedor, análisis JSON, nivel con valor por omisión `Information`, `TraceId` y `SpanId` como metadatos estructurados, etiquetas `servicio` y `nivel` únicamente, envío a Loki — `monitoring/alloy/config.alloy` (FR-001, FR-002, FR-003, FR-004, FR-005, FR-006)
- [ ] T008 [US1] Añadir los servicios `loki` y `alloy` a la composición, con versión fijada, nombres de contenedor `gsp-loki` y `gsp-alloy`, volúmenes nombrados (el de Alloy para las posiciones de lectura), socket de Docker en solo lectura, límite de memoria y red `observabilidad` — `monitoring/docker-compose.yml` (FR-007, FR-030, FR-031, FR-032, FR-034)
- [ ] T009 [P] [US1] Aprovisionar el origen de datos `gsp-loki`, sin marcarlo como predeterminado — `monitoring/grafana/provisioning/datasources/loki.yml` (FR-009)
- [ ] T010 [US1] Levantar el entorno, generar tráfico y comprobar a mano en Grafana que los inicios de sesión fallidos se filtran por nivel `Warning` y que el `TraceId` es consultable como campo y no aparece entre las etiquetas (FR-003, FR-004, FR-005, FR-009)

**Punto de control:** US1 funciona sola. El visor ya sustituye a `docker logs`.

## Fase 2 — US2: trazas (P1, independiente de la Fase 1)

- [ ] T011 [US2] Configurar Tempo contra la documentación de la 3.0: modo monolítico, recepción OTLP por gRPC, disco local, retención de bloques de 7 días — `monitoring/tempo/config.yaml` (FR-011)
- [ ] T012 [US2] Añadir el servicio `tempo` a la composición, con versión fijada, nombre de contenedor `gsp-tempo`, volumen nombrado, límite de memoria y solo el puerto 3200 publicado — `monitoring/docker-compose.yml` (FR-013, FR-030, FR-031, FR-034)
- [ ] T013 [US2] Cambiar el exportador de la tubería `traces` de `debug` a OTLP hacia Tempo y reescribir su comentario para que remita a esta spec — `monitoring/otel-collector/config.yaml` (FR-010)
- [ ] T014 [P] [US2] Aprovisionar el origen de datos `gsp-tempo` — `monitoring/grafana/provisioning/datasources/tempo.yml` (FR-014)
- [ ] T015 [US2] Generar tráfico y comprobar a mano que una traza de `/api/Services` muestra tramos de PostgreSQL, que se pueden buscar trazas por duración, error y ruta, y que no hay ninguna traza de `/health` (FR-012, FR-015)

**Punto de control:** US2 funciona sola. La tubería reservada por la spec 008 ya tiene destino.

## Fase 3 — US3: correlación y tablero (P2)

- [ ] T016 [US3] Añadir al origen `gsp-loki` el campo derivado que enlaza el metadato `TraceId` con el origen `gsp-tempo` — `monitoring/grafana/provisioning/datasources/loki.yml` (FR-016)
- [ ] T017 [US3] Añadir al origen `gsp-tempo` la consulta de registros por identificador de traza contra `gsp-loki` — `monitoring/grafana/provisioning/datasources/tempo.yml` (FR-017)
- [ ] T018 [US3] Construir el tablero "Trazabilidad": variable de texto `TraceId`, registros y traza de esa petición, volumen por nivel, errores y advertencias recientes, trazas más lentas — `monitoring/grafana/dashboards/gsp-trazabilidad.json` (FR-018, FR-019, FR-020)
- [ ] T019 [US3] Recorrer a mano los recorridos 1 y 2 del quickstart y comprobar que los registros de una traza tienen todos su mismo `TraceId` (FR-016, FR-017)

**Punto de control:** los dos almacenes funcionan como un solo visor.

## Fase 4 — US4: guiones e integración continua (P3)

- [ ] T020 [P] [US4] Esperar a Loki, Tempo y Alloy e imprimir sus direcciones — `scripts/monitoreo-up.sh`, `scripts/monitoreo-up.ps1` (FR-022)
- [ ] T021 [P] [US4] Incluir los tres volúmenes nuevos en la purga, conservando la confirmación — `scripts/monitoreo-down.sh`, `scripts/monitoreo-down.ps1` (FR-023)
- [ ] T022 [US4] Añadir la sección de trazabilidad a la verificación: sondas de los tres servicios, registros de la API con nivel y `TraceId`, muestra de al menos 5 `TraceId` resuelta en Tempo, ausencia de las contraseñas del tráfico sintético y número de flujos de la API — `scripts/verificar-monitoreo.sh` (FR-024, FR-025, FR-026)
- [ ] T023 [US4] La misma sección en la variante de PowerShell — `scripts/verificar-monitoreo.ps1` (FR-024, FR-025, FR-026)
- [ ] T024 [US4] Validar la configuración de Loki, Tempo y Alloy, con sus imágenes declaradas como variables del flujo — `.github/workflows/monitoring-stack.yml` (FR-027, FR-030)
- [ ] T025 [US4] Ampliar la comprobación de tableros para aceptar `gsp-loki` y `gsp-tempo` y rechazar cualquier otro identificador de origen de datos — `.github/workflows/monitoring-stack.yml` (FR-027)
- [ ] T026 [US4] En el trabajo de humo, esperar a los tres servicios nuevos, ejecutar la verificación extendida y añadir sus registros al paso de diagnóstico — `.github/workflows/monitoring-stack.yml` (FR-028)
- [ ] T027 [US4] Recorrer el paso 6 del quickstart: con Loki detenido, la verificación falla y dice qué falta (FR-024)

**Punto de control:** el visor se levanta, se apaga y se verifica igual en cualquier equipo y en el flujo.

## Fase 5 — Cierre

- [ ] T028 [P] Documentar en el README del entorno los servicios nuevos, sus puertos, sus volúmenes, el consumo de memoria y el riesgo del socket de Docker — `monitoring/README.md` (FR-031, FR-032, FR-033)
- [ ] T029 [P] Actualizar la sección de observabilidad del README raíz: dónde se consultan ahora registros y trazas, y la asimetría de `dotnet run` — `README.md` (FR-033)
- [ ] T030 Cronometrar el recorrido 3 del quickstart y medir los tiempos de SC-003; anotar los resultados en el PR de implementación; corregir el recorrido si algún paso no coincide con la pantalla real (FR-021, SC-001, SC-003)
- [ ] T031 Confirmar que el diff del PR de implementación no toca `backend/` (FR-029, SC-006)
- [ ] T032 Tomar las capturas para la sección 13 del documento: tablero con datos, recorrido 1, recorrido 2 y la verificación en verde

---

## Dependencias

```text
Fase 0
  ├──► Fase 1 (US1) ──┐
  │                   ├──► Fase 3 (US3) ──► Fase 4 (US4) ──► Fase 5
  └──► Fase 2 (US2) ──┘
```

- **Las fases 1 y 2 no se bloquean entre sí.** Tocan la misma composición (T008 y T012), así que si se
  hacen en paralelo conviene integrar una antes de empezar la otra en ese archivo.
- La fase 3 necesita los dos orígenes de datos.
- La fase 4 necesita que exista todo lo que sus guiones verifican.

## Ejemplo de paralelización

Dentro de la Fase 0, T002 a T005 no comparten nada. Dentro de la Fase 4, T020 y T021 tocan guiones
distintos.

## Estrategia de implementación

El **mínimo viable** son las fases 0 a 3: con ellas existe el visor de trazabilidad completo. La
fase 4 es reproducibilidad y la 5, evidencia.

Si hubiera que detenerse, el orden de valor decreciente es: **1 → 2 → 3 → 4**. La fase 1 sola ya
sustituye a `docker logs`; con la 2 se ve dónde se va el tiempo; la 3 es la que justifica el nombre
de *trazabilidad*.
