# Quickstart — Spec 009

> Recorrido de validación para el responsable. No describe cómo se implementa, sino **qué hacer para
> comprobar que funciona**. Cada paso indica qué debería verse; si no se ve eso, la tarea no está
> terminada.
>
> Tiempo estimado: 20 minutos.

---

## Antes de empezar

| Requisito | Comprobación |
|---|---|
| Lo mismo que el [quickstart de la spec 008](../008-monitoreo-metricas-alertas/quickstart.md) | Docker en marcha, `.env` con `GRAFANA_ADMIN_PASSWORD`, el stack de análisis estático detenido |
| Puertos libres, además de los de la 008 | 3100, 3200, 12345 |
| La API se ejecuta **en contenedor** | `monitoreo-up` la levanta así. Con `dotnet run` llegan las trazas, pero no los registros (spec, §5) |

---

## 1. Levantar el entorno

```powershell
.\scripts\monitoreo-up.ps1
```

**Debería verse:** además de las herramientas de la spec 008, Loki, Tempo y el recolector marcados
como sanos, y sus direcciones en la tabla final.

## 2. Generar tráfico y verificar

```powershell
.\scripts\generar-trafico.ps1 -Url http://localhost:8080 -Duracion 60
.\scripts\verificar-monitoreo.ps1
```

**Debería verse**, en la sección de trazabilidad de la verificación:

- Los tres servicios nuevos responden sanos.
- Hay registros de la API, con nivel y con `TraceId`.
- Los `TraceId` muestreados de los registros existen como trazas.
- Ningún registro contiene las contraseñas del tráfico sintético.

El guion termina con error, y nombra qué falla, si cualquiera de estas comprobaciones no se cumple.

## 3. Recorrido 1 — De un error a su traza

Abrir **http://localhost:3001** → tablero **Trazabilidad**.

1. En el panel de errores y advertencias recientes, elegir un inicio de sesión fallido (respuesta 401).
2. Desplegar la línea y pulsar el enlace de su `TraceId`.

**Debería verse:** la traza de esa petición, con el tramo HTTP del inicio de sesión y, debajo, los
tramos de las consultas a PostgreSQL que hizo para buscar al usuario, cada uno con su duración.

## 4. Recorrido 2 — De una traza lenta a sus registros

1. En el panel de trazas más lentas, abrir la primera.
2. Desde la traza, pedir sus registros.

**Debería verse:** solo los registros de esa petición, todos con el mismo `TraceId`. Si aparece un
registro con otro identificador, la consulta de registros de la traza está mal filtrada.

## 5. Recorrido 3 — Desde la bitácora de auditoría

Este es el recorrido que responde "el administrador ve una acción en la bitácora y quiere saber qué
pasó técnicamente en esa petición". **Cronometrarlo: es el criterio SC-001.**

1. Con el rol de administrador, abrir la bitácora de acciones (`/usuarios/logs`) en el frontend.
2. Copiar el `TraceId` de una fila reciente.
3. En Grafana, tablero **Trazabilidad**, pegarlo en el campo `TraceId`.

**Debería verse:** en menos de un minuto desde el paso 1, los registros y la traza de esa petición,
juntos en el mismo tablero.

> Si la fila no tiene `TraceId`, la acción ocurrió fuera de una petición (una tarea en segundo plano)
> o antes de que existiera la columna. No es un fallo del visor: no hay traza que mostrar.

## 6. Comprobar que la verificación sí detecta fallos

```powershell
docker stop gsp-loki
.\scripts\verificar-monitoreo.ps1
docker start gsp-loki
```

**Debería verse:** la verificación termina con error y dice que el almacén de registros no responde.
Una verificación que pasa siempre no verifica nada.

## 7. Apagar

```powershell
.\scripts\monitoreo-down.ps1
```

Los datos se conservan entre arranques. `-Purgar` borra también los volúmenes de registros, trazas y
posiciones del recolector, y **pide confirmación**.

---

## Recorrido mínimo

Si solo hay tiempo para una comprobación, es el **recorrido 1**. Ver registros en un almacén o
trazas en otro no demuestra nada que `docker logs` no diera ya; pasar de un error concreto a la
historia completa de su petición con un clic es lo que hace de esto un visor de trazabilidad.
