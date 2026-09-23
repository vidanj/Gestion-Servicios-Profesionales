# Tareas — Spec 008, monitoreo y despliegue verificado

> **Formato:** `T### [P] [USn] Descripción — ruta (FR-###)`
> `[P]` = puede ejecutarse en paralelo con las demás tareas marcadas de la misma fase.
> Cada tarea se liga a una historia y a los requisitos que cubre (constitución, *Trazabilidad SDD*).
>
> **Entrada:** [spec.md](spec.md) · [plan.md](plan.md) · [research.md](research.md) ·
> [data-model.md](data-model.md) · [contracts/](contracts/metricas.md)

---

## Fase 0 — Preparación

- [ ] T001 Confirmar en el repositorio de imágenes la etiqueta exacta de las cinco imágenes del entorno y anotarlas — `monitoring/README.md` (FR-010)
- [ ] T002 [P] Confirmar la versión estable del paquete de instrumentación del tiempo de ejecución y fijarla al nivel del resto de paquetes de telemetría (FR-007, FR-050)
- [ ] T003 [P] Comprobar que los puertos 3001, 9090, 9093, 9115, 4317, 4318 y 8889 están libres en el equipo de trabajo (FR-012)
- [ ] T004 [P] Añadir `GRAFANA_ADMIN_PASSWORD` y un valor de ejemplo para `OTEL_EXPORTER_OTLP_ENDPOINT` — `.env.example` (FR-052)
- [ ] T005 Crear el directorio del entorno con su README: qué levanta, en qué puertos, con qué credenciales, cómo se apaga y la advertencia de no levantarlo junto al stack de análisis estático — `monitoring/README.md` (FR-012)

**Punto de control:** las versiones existen, los puertos están libres y las variables declaradas.

---

## Fase 1 — US1: stack de monitoreo y métricas técnicas (P1, mínimo viable)

- [ ] T006 [US1] Escribir el archivo de composición del entorno con los cinco servicios, sus volúmenes nombrados y la red propia, con cabecera que explique por qué vive separado del de la aplicación — `monitoring/docker-compose.yml` (FR-003, FR-010, FR-011, FR-012)
- [ ] T007 [US1] Escribir la superposición que conecta la aplicación al colector sin modificar el archivo raíz — `monitoring/docker-compose.api.yml` (FR-001, FR-012)
- [ ] T008 [US1] Configurar el colector: recepción OTLP, límite de memoria, agrupación, publicación en formato Prometheus y caducidad de series; dejar creada y anotada la tubería de trazas para el punto 2 del caso de estudio — `monitoring/otel-collector/config.yaml` (FR-001, FR-003, FR-053)
- [ ] T009 [US1] Configurar la recolección: periodicidad, orígenes del colector y de su propia telemetría, y los dos grupos de sondeo externo — `monitoring/prometheus/prometheus.yml` (FR-004, FR-009)
- [ ] T010 [P] [US1] Configurar el sondeo externo con dos módulos, uno de ellos con plazo ampliado para el arranque en frío, ambos afirmando el cuerpo de la respuesta y no solo el código — `monitoring/blackbox/blackbox.yml` (FR-009, FR-020)
- [ ] T011 [P] [US1] Aprovisionar el origen de datos de forma declarativa — `monitoring/grafana/provisioning/datasources/prometheus.yml` (FR-005)
- [ ] T012 [P] [US1] Aprovisionar el proveedor de tableros, impidiendo la edición desde la interfaz para que el archivo versionado siga siendo la verdad — `monitoring/grafana/provisioning/dashboards/proveedor.yml` (FR-005)
- [ ] T013 [US1] Construir el tablero técnico con los paneles de disponibilidad, volumen, latencia, proporción de error y saturación — `monitoring/grafana/dashboards/gsp-api.json` (FR-006)
- [ ] T014 [US1] Añadir la dependencia de instrumentación del tiempo de ejecución — `backend/SistemaServicios.API/SistemaServicios.API.csproj` (FR-007, FR-050)
- [ ] T015 [US1] Registrar la instrumentación del tiempo de ejecución y el medidor del limitador de peticiones, con comentarios que expliquen por qué el segundo no viaja con la instrumentación general — `backend/SistemaServicios.API/Extensions/TelemetryConfiguration.cs` (FR-007, FR-008)
- [ ] T016 [US1] Añadir la identidad de versión y ambiente al recurso, de forma condicional para que su ausencia no rompa el arranque — `backend/SistemaServicios.API/Extensions/TelemetryConfiguration.cs` (FR-002)
- [ ] T017 [US1] Ampliar las pruebas de telemetría: se resuelve sin colector, el medidor del limitador queda registrado y la instrumentación nueva no altera el arranque — `backend/SistemaServicios.Tests/Unit/TelemetryConfigurationTests.cs` (FR-002, FR-007, FR-008)
- [ ] T018 [US1] Levantar el entorno con la aplicación y comprobar a mano que las métricas de los apartados 1.1, 1.2 y 1.4 del catálogo llegan al recolector (FR-003, FR-004)

**Punto de control (US1 entregable):** el tablero muestra latencia, error y saturación con tráfico
real, sin haber tocado la lógica de negocio.

---

## Fase 2 — US4: despliegue verificado y reversión (P1, independiente de la Fase 1)

> **Bloqueante humano.** T019, T020 y T021 dependen de accesos que solo tiene el responsable.
> Conviene arrancarlas el primer día, en paralelo con la Fase 1.

- [ ] T019 [US4] Obtener el disparador de despliegue de la plataforma y guardarlo como secreto (FR-034)
- [ ] T020 [US4] Declarar la dirección pública a sondear como variable de la plataforma de integración continua (FR-037)
- [ ] T021 [US4] Crear el ambiente protegido con revisor obligatorio (FR-043)
- [ ] T022 [US4] **Comprobar a mano** que indicar un commit al disparar redespliega ese commit y no el último de la rama; si no fuera así, aplicar la alternativa prevista en research D6 (FR-040, FR-041)
- [ ] T023 [US4] Escribir el flujo de despliegue: disparadores automático y manual, agrupación de concurrencia sin cancelación y ambiente protegido — `.github/workflows/deploy.yml` (FR-036, FR-040, FR-043)
- [ ] T024 [US4] Añadir la puerta que verifica que las cuatro comprobaciones de integración continua están en verde para **ese mismo commit** — `.github/workflows/deploy.yml` (FR-035)
- [ ] T025 [US4] Añadir el disparo con el secreto enmascarado y la salida descartada — `.github/workflows/deploy.yml` (FR-034)
- [ ] T026 [US4] Añadir la espera inicial y el sondeo con exigencia de varias respuestas sanas consecutivas y plazo dimensionado para el arranque en frío — `.github/workflows/deploy.yml` (FR-037, FR-038)
- [ ] T027 [US4] Añadir el paso opcional que distingue fallo de construcción de fallo de arranque, que se omite con aviso si no hay credenciales — `.github/workflows/deploy.yml` (FR-039)
- [ ] T028 [US4] Añadir el resumen con commit desplegado y anterior, duración, orden de reversión lista para copiar y advertencia sobre migraciones destructivas — `.github/workflows/deploy.yml` (FR-039, FR-041, FR-042)
- [ ] T029 [US4] Escribir el guion local equivalente, en sus dos variantes — `scripts/desplegar.ps1`, `scripts/desplegar.sh` (FR-047, FR-048, FR-049)
- [ ] T030 [US4] Ejecutar **un despliegue real supervisado** y registrar su duración (FR-037, SC-008)
- [ ] T031 [US4] Ejecutar **una reversión real** y registrar su duración (FR-041, SC-009)

**Punto de control (US4 entregable):** integrar en la rama de integración despliega y verifica solo,
y existe una reversión probada con su tiempo medido.

---

## Fase 3 — US2: niveles de servicio y alertas (requiere Fase 1)

- [ ] T032 [US2] Definir los indicadores compartidos como reglas de registro, para que tablero y alerta no calculen lo mismo por separado — `monitoring/prometheus/reglas/registros.yml` (FR-023)
- [ ] T033 [US2] Escribir las alertas de disponibilidad: servicio caído, base de datos inalcanzable con el proceso vivo, y ausencia de métricas — `monitoring/prometheus/reglas/alertas.yml` (FR-015, FR-016, FR-019)
- [ ] T034 [US2] Escribir las alertas de error y latencia, con la condición de volumen mínimo y los dos niveles de latencia — `monitoring/prometheus/reglas/alertas.yml` (FR-017, FR-018)
- [ ] T035 [US2] Escribir las alertas de saturación de memoria y de trabajo pendiente — `monitoring/prometheus/reglas/alertas.yml` (FR-019)
- [ ] T036 [US2] Revisar que **toda** alerta declara severidad, componente, resumen, descripción con la razón del umbral, y acción — `monitoring/prometheus/reglas/alertas.yml` (FR-014)
- [ ] T037 [US2] Calibrar los tiempos de espera para que absorban el arranque en frío — `monitoring/prometheus/reglas/alertas.yml` (FR-020)
- [ ] T038 [US2] Configurar el gestor de alertas: agrupación, inhibición de las derivadas cuando ya disparó la causa raíz, y receptor de consola con el destino externo documentado y desactivado — `monitoring/alertmanager/alertmanager.yml` (FR-021)
- [ ] T039 [US2] Escribir los casos de prueba de las reglas, incluido el que afirma que un arranque en frío **no** dispara nada y el que afirma que sin tráfico no hay alerta de error — `monitoring/prometheus/pruebas/alertas_test.yml` (FR-022, SC-005)
- [ ] T040 [US2] Escribir el flujo de validación del entorno: configuración de las cinco herramientas, casos de prueba de las reglas, validez de los tableros y ausencia de etiquetas móviles — `.github/workflows/monitoring-stack.yml` (FR-010, FR-013, FR-022)
- [ ] T041 [US2] Comprobar el recorrido completo de caída, alerta, restablecimiento y resolución (SC-004)

**Punto de control (US2 entregable):** detener la aplicación avisa; restablecerla resuelve el aviso.

---

## Fase 4 — US3: métricas de negocio (requiere Fase 1)

- [ ] T042 [US3] Declarar el contrato del medidor con un método por hecho de negocio — `backend/SistemaServicios.API/Interfaces/IMetricasDeNegocio.cs` (FR-025)
- [ ] T043 [US3] Implementar el medidor: instancia única, instrumentos creados una sola vez, liberación de recursos, nombre del medidor como constante compartida y ningún método que propague excepciones — `backend/SistemaServicios.API/Telemetry/MetricasDeNegocio.cs` (FR-025, FR-033)
- [ ] T044 [US3] Registrar la dependencia en el punto único de composición, nunca en el arranque — `backend/SistemaServicios.API/Extensions/ApplicationServiceExtensions.cs` (FR-025)
- [ ] T045 [US3] Registrar el medidor de negocio y acotar los tramos del histograma de duración a segundos realistas — `backend/SistemaServicios.API/Extensions/TelemetryConfiguration.cs` (FR-032)
- [ ] T046 [US3] Instrumentar los intentos de autenticación y las altas, distinguiendo los tres resultados de inicio de sesión sin alterar la respuesta homogénea que ve el cliente — `backend/SistemaServicios.API/Services/AuthService.cs` (FR-024, FR-030, FR-031)
- [ ] T047 [US3] Instrumentar la creación de solicitudes y los cambios de estado con origen, destino y resultado — `backend/SistemaServicios.API/Services/ServiceRequestService.cs` (FR-024, FR-028, FR-029)
- [ ] T048 [US3] Instrumentar los respaldos por resultado y su duración, incluida la duración cuando falla — `backend/SistemaServicios.API/Services/BackupService.cs` (FR-024, FR-032)
- [ ] T049 [US3] Escribir el ayudante de pruebas que captura mediciones y etiquetas, siguiendo el patrón del que ya existe para los registros y sin añadir dependencias — `backend/SistemaServicios.Tests/Unit/EscuchaDeMetricas.cs` (FR-050)
- [ ] T050 [P] [US3] Probar el medidor: nombres, etiquetas, ausencia de datos personales y que su uso tras liberar recursos no lanza — `backend/SistemaServicios.Tests/Unit/MetricasDeNegocioTests.cs` (FR-026, FR-027, FR-033)
- [ ] T051 [P] [US3] Probar la instrumentación de autenticación, incluido que un fallo del medidor no cambia el resultado del inicio de sesión — `backend/SistemaServicios.Tests/Unit/AuthServiceMetricasTests.cs` (FR-030, FR-031, FR-033)
- [ ] T052 [P] [US3] Probar la instrumentación de solicitudes, con una transición válida y otra rechazada — `backend/SistemaServicios.Tests/Unit/ServiceRequestServiceMetricasTests.cs` (FR-028, FR-029)
- [ ] T053 [P] [US3] Probar la instrumentación de respaldos en éxito y en fallo — `backend/SistemaServicios.Tests/Unit/BackupServiceMetricasTests.cs` (FR-032)
- [ ] T054 [US3] Probar de integración: un flujo real mueve el contador, la aplicación arranca sin colector y **el endpoint de métricas responde no encontrado** — `backend/SistemaServicios.Tests/Integration/MetricasTests.cs` (FR-001, FR-002)
- [ ] T055 [US3] Construir el tablero de negocio — `monitoring/grafana/dashboards/gsp-negocio.json` (FR-005)
- [ ] T056 [US3] Añadir las alertas de negocio: intentos de autenticación fallidos sostenidos y respaldo fallido — `monitoring/prometheus/reglas/alertas.yml` (FR-014)
- [ ] T057 [US3] Comprobar la cardinalidad tras una hora de tráfico (SC-010)

**Punto de control (US3 entregable):** los contadores se mueven con el flujo real y ninguna etiqueta
contiene datos personales.

---

## Fase 5 — US5: guiones y humo automatizado (requiere Fases 1, 3 y 4)

- [ ] T058 [P] [US5] Guion de arranque del entorno, que espera a que cada herramienta responda y se detiene si falta la contraseña del tablero — `scripts/monitoreo-up.{ps1,sh}` (FR-044, FR-048, FR-049)
- [ ] T059 [P] [US5] Guion de apagado, con purga de volúmenes bajo confirmación explícita — `scripts/monitoreo-down.{ps1,sh}` (FR-044, FR-048)
- [ ] T060 [P] [US5] Guion de tráfico sintético representativo — `scripts/generar-trafico.{ps1,sh}` (FR-046, FR-048)
- [ ] T061 [US5] Guion de verificación del catálogo completo, de las reglas cargadas y del aprovisionamiento — `scripts/verificar-monitoreo.{ps1,sh}` (FR-045, FR-048)
- [ ] T062 [US5] Índice de los guiones con su propósito y sus variables — `scripts/README.md` (FR-049)
- [ ] T063 [US5] Añadir al flujo de validación el trabajo que levanta el entorno completo, genera tráfico y ejecuta **el mismo guion de verificación** que se usa en local — `.github/workflows/monitoring-stack.yml` (FR-045)

**Punto de control (US5 entregable):** una orden levanta el entorno y otra responde si falta algo.

---

## Fase 6 — Cierre

- [ ] T064 Redactar la fuente del documento entregable con los cinco apartados que exige el caso de estudio — `docs/monitoreo-metricas-y-alertas.md` (FR-053, SC-011)
- [ ] T065 Añadir los apartados reservados del visor de trazabilidad y del visor de auditoría, indicando qué existe ya y qué falta, sin desarrollarlos — `docs/monitoreo-metricas-y-alertas.md` (FR-053)
- [ ] T066 [P] Escribir el generador del documento y su archivo de datos — `docs/reporte/generar_monitoreo.py`, `docs/reporte/enlaces-monitoreo.json` (SC-011)
- [ ] T067 [P] Documentar cómo se ejecuta el generador y dónde cae la salida — `docs/reporte/README.md`
- [ ] T068 [P] Añadir al inventario de herramientas las filas del colector, el recolector, el gestor de alertas, el tablero, el sondeo y los dos flujos nuevos — `docs/planeacion/parametros-configuracion.md`
- [ ] T069 [P] Actualizar la estrategia de despliegue: lo que era "planeado" ya existe, y las métricas de entrega tienen por fin fuente de medición — `docs/planeacion/estrategia-despliegue.md`
- [ ] T070 [P] Documentar en el README las variables nuevas, cómo levantar el entorno y el flujo de despliegue y reversión — `README.md` (constitución, *Documentación*)
- [ ] T071 Cerrar el issue superado por cambio de enfoque, con un comentario que enlace la decisión y explique por qué el camino elegido es otro (research D1)
- [ ] T072 Formatear el backend y comprobar que la compilación termina sin advertencias y sin silenciadores (FR-051, SC-007)
- [ ] T073 Ejecutar la suite completa de pruebas del backend (FR-033, SC-006)
- [ ] T074 Generar el documento final y comprobar que ningún artefacto generado quedó sin ignorar
- [ ] T075 Ejecutar `/speckit-analyze` y resolver los hallazgos críticos, si los hubiera
- [ ] T076 Ejecutar `/speckit-converge` y confirmar que no quedan diferencias entre los artefactos y el código

---

## Dependencias y orden

```text
Fase 0
  ├──► Fase 1 (US1) ──┬──► Fase 3 (US2) ──┐
  │                   └──► Fase 4 (US3) ──┼──► Fase 5 (US5) ──► Fase 6
  └──► Fase 2 (US4) ───────────────────────┘
```

- **Las fases 1 y 2 no se bloquean entre sí** y deberían avanzar en paralelo: la segunda arranca con
  tareas que dependen de accesos externos y conviene no dejarlas para el final.
- Las fases 3 y 4 dependen ambas de la 1, pero no entre sí.
- La fase 5 necesita que exista todo lo que sus guiones verifican.

## Ejemplo de paralelización

Dentro de la Fase 4, una vez hechas T042 a T048, las cuatro tareas de prueba unitaria (T050 a T053)
tocan archivos distintos y no comparten estado.

## Estrategia de implementación

El **mínimo viable** son las fases 0, 1 y 2. Con eso el caso de estudio ya tiene sus cinco
apartados: pipeline justificado, entorno, niveles de servicio, métricas y parámetros. Las fases 3 y 4
son las que convierten la entrega en monitoreo de verdad —alertas que avisan y métricas que hablan
del negocio— y la 5 es reproducibilidad.

Si hubiera que detenerse, el orden de valor decreciente es: **1 y 2 → 3 → 4 → 5**.
