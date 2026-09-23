# Quickstart — Spec 008

> Recorrido de validación para el responsable. No describe cómo se implementa, sino **qué hacer para
> comprobar que funciona**. Cada paso indica qué debería verse; si no se ve eso, la tarea no está
> terminada.
>
> Tiempo estimado: 25 minutos, más 5 minutos de espera en el paso 5.

---

## Antes de empezar

| Requisito | Comprobación |
|---|---|
| Docker en marcha | `docker version` responde |
| Puertos libres | 3001, 9090, 9093, 9115, 4317, 4318, 8889 |
| `.env` con `GRAFANA_ADMIN_PASSWORD` definida | El guion de arranque avisa si falta y se detiene |
| El stack de análisis estático **detenido** | Los dos juntos saturan un equipo de trabajo |

---

## 1. Levantar el entorno

```powershell
.\scripts\monitoreo-up.ps1
```

**Debería verse:** una tabla con las direcciones de cada herramienta y el aviso de que todas
respondieron sanas. El guion no termina hasta que lo hacen; si alguna no responde, lo dice y termina
con error en lugar de dejar un entorno a medias.

## 2. Generar tráfico

```powershell
.\scripts\generar-trafico.ps1 -Duracion 60
```

**Debería verse:** un recuento de las peticiones enviadas por tipo: correctas, no encontradas,
inicios de sesión fallidos, un alta, una solicitud y una transición inválida. El tráfico es
deliberadamente variado: si solo hubiera peticiones correctas, los paneles de error quedarían vacíos
y no se podría comprobar que funcionan.

## 3. Verificar el catálogo

```powershell
.\scripts\verificar-monitoreo.ps1
```

**Debería verse:** una tabla con cada métrica del catálogo marcada como presente, el recuento de
reglas de alerta cargadas y la confirmación del origen de datos y los tableros. El guion termina con
código distinto de cero si falta cualquier cosa, y **nombra cuál**.

## 4. Mirar los tableros

Abrir **http://localhost:3001** e iniciar sesión con la contraseña definida en el `.env`.

| Tablero | Qué debería verse |
|---|---|
| Salud de la API | Disponibilidad, volumen de peticiones, latencia p95, proporción de respuestas de error y saturación, todos con datos del tráfico recién generado |
| Negocio | Solicitudes creadas, embudo de cambios de estado, intentos de autenticación por resultado y respaldos |

**Esto es validación visual y es del responsable** (constitución, *Flujo de Trabajo y Calidad*). Un
panel vacío no significa necesariamente que el tablero esté mal: puede ser que esa métrica no tenga
tráfico todavía. El paso 3 distingue ambos casos, y por eso va antes.

## 5. Provocar una alerta

Es la comprobación más importante del recorrido, porque es la única que demuestra que el monitoreo
sirve para algo cuando nadie está mirando.

```powershell
docker stop gsp-api        # el nombre exacto lo imprime el paso 1
```

Esperar **3 minutos** y abrir **http://localhost:9093**.

**Debería verse:** la alerta de indisponibilidad, en estado disparado, con su resumen, su
descripción y la acción sugerida, todo en español.

```powershell
docker start gsp-api
```

Esperar **2 minutos**.

**Debería verse:** la alerta desaparecida del listado de activas. Se resolvió sola.

> Si la alerta apareciera **antes** de los 3 minutos, el tiempo de espera está mal calibrado y
> volvería a aparecer cada vez que el servicio publicado despierta de una siesta. Es el fallo que
> hace que la gente deje de creerse las alertas.

## 6. Comprobar el despliegue

Solo cuando el secreto del disparador y el ambiente protegido ya estén configurados.

1. Integrar un cambio trivial en la rama de integración.
2. Observar la ejecución del flujo de despliegue.

**Debería verse:** la verificación de las comprobaciones previas, el disparo, la espera y el sondeo
hasta que la sonda responde sana varias veces seguidas. En el resumen: el commit desplegado, el
anterior, la duración y la orden de reversión ya escrita.

## 7. Revertir de verdad

```
Actions → Despliegue → Run workflow → sha = <el commit anterior>
```

**Debería verse:** el mismo recorrido, terminando sano, en **10 minutos o menos**. Anotar el tiempo:
es el criterio SC-009 y es la primera vez que el proyecto lo mide.

## 8. Apagar

```powershell
.\scripts\monitoreo-down.ps1
```

Los datos se conservan entre arranques. Para borrarlos hace falta `-Purgar`, que **pide
confirmación**: el histórico de métricas es evidencia, y borrarlo es destructivo.

---

## Recorrido mínimo

Si solo hay tiempo para una comprobación, es el **paso 5**. Los tableros se ven bonitos con
cualquier configuración; que una alerta dispare cuando debe, se resuelva sola y **no** dispare
durante un arranque en frío es lo que distingue un monitoreo real de una captura de pantalla.
