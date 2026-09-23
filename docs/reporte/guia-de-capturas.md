# Guía de diagramas y capturas del documento

El documento generado deja **24 recuadros amarillos**: dos para diagramas y veintidós para
capturas. Esta guía dice qué va en cada uno y cómo obtenerlo, paso a paso.

> **Cómo se pegan.** Abrir `.github/DRAFTS/Monitoreo_Metricas_SLO_y_Alertas.docx`, buscar el
> recuadro amarillo por su texto (Ctrl+B en Word), hacer clic dentro, pegar la imagen y borrar
> el texto del marcador. El recuadro mantiene el alto mínimo, así que la imagen queda encuadrada.
>
> **Antes de empezar:** actualizar el índice (clic derecho sobre él → *Actualizar campo* →
> *Actualizar toda la tabla*).

---

## Bloque A — Diagramas (2)

**Ya están generados** en `docs/reporte/diagramas/`:

| Archivo | Va en | Marcador |
|---|---|---|
| `01-arquitectura-telemetria.png` | Sección 2 | `[DIAGRAMA: API → colector → recolector → tableros y alarmas]` |
| `02-flujo-del-pipeline.png` | Sección 4.1 | `[DIAGRAMA: del commit a la versión verificada, con sus puertas]` |

El segundo es alto y estrecho: conviene pegarlo a página completa, o partirlo en dos si la
maquetación lo pide.

### Si hay que regenerarlos

Las fuentes son los `.mmd` del mismo directorio. **Opción A, sin instalar nada:** abrir
<https://mermaid.live>, pegar el contenido del `.mmd` (sin las líneas que empiezan por `%%`),
y usar *Actions → PNG*. Subir el zoom a 3× antes de descargar, o el texto saldrá borroso al
imprimir.

**Opción B, desde la línea de órdenes:**

```powershell
npx -y @mermaid-js/mermaid-cli `
  -i docs/reporte/diagramas/01-arquitectura-telemetria.mmd `
  -o docs/reporte/diagramas/01-arquitectura-telemetria.png `
  -b white -w 1600
```

Si la descarga de Chromium falla o tarda demasiado, se puede reutilizar el que ya instaló
Playwright creando un archivo `puppeteer.json`:

```json
{ "executablePath": "C:\\Users\\<usuario>\\AppData\\Local\\ms-playwright\\chromium-1208\\chrome-win64\\chrome.exe" }
```

y añadiendo `-p puppeteer.json` a la orden.

> **Aviso:** una línea que sea exactamente `%%` rompe el analizador de Mermaid. Los comentarios
> de estos archivos llevan `%% .` por ese motivo.

---

## Bloque B — Issues y solicitudes de cambio (10)

Todas se toman en el navegador. **Recortar solo la zona útil**, sin la barra del navegador ni
las pestañas: una captura de pantalla completa deja el contenido ilegible al reducirla.

### 1. Issue #250 — documentación

1. Abrir <https://github.com/vidanj/Gestion-Servicios-Profesionales/issues/250>
2. Comprobar que se ven el título, el estado y la columna derecha con las etiquetas
   `documentation`, `priority: P2`, `size: M`, `estimate: 5`.
3. Recortar desde el título hasta el final del primer párrafo de la descripción, **incluyendo
   la columna de etiquetas**. Eso es lo que demuestra el uso de las plantillas del proyecto.

### 2. PR #252 — conversación

1. Abrir <https://github.com/vidanj/Gestion-Servicios-Profesionales/pull/252>
2. Pestaña **Conversation**.
3. Recortar la cabecera (título, estado *Open*, rama origen → destino) y el principio del
   cuerpo, donde se ve la plantilla rellenada.

### 3. PR #252 — archivos

1. Mismo PR, pestaña **Files changed**.
2. En el árbol de la izquierda, desplegar `specs/008-monitoreo-metricas-alertas/`.
3. Recortar de modo que se vean **el árbol completo del spec** y el contador
   `+3813 −9 · 17 files`.

### 4. PR #252 — comprobaciones en verde

1. Mismo PR, al final de la pestaña **Conversation**, o la pestaña **Checks**.
2. Recortar el bloque de comprobaciones con todas en verde.

### 5. Issue #251 — implementación

Igual que la captura 1, en
<https://github.com/vidanj/Gestion-Servicios-Profesionales/issues/251>. Aquí las etiquetas son
`ci`, `backend`, `priority: P1`, `size: L`, `estimate: 8`.

### 6. PR #253 — conversación

1. Abrir <https://github.com/vidanj/Gestion-Servicios-Profesionales/pull/253>
2. Pestaña **Conversation**.
3. Recortar la cabecera **y** la zona donde aparece la tabla «Verificado contra el sistema en
   marcha». Es la parte que más aporta al documento.

### 7. PR #253 — archivos

1. Pestaña **Files changed**.
2. Desplegar `monitoring/` en el árbol.
3. Recortar mostrando el árbol y el contador `+4972 −64 · 50 files`.

### 8. PR #253 — comprobaciones en verde

1. Pestaña **Checks** del PR #253.
2. Recortar la lista completa: son 13 comprobaciones, **incluida «Levantar el entorno y
   comprobar las métricas»**, que es la que demuestra que el monitoreo funciona en el pipeline
   y no solo en un equipo.

### 9. Issue #189 — absorbido

1. Abrir <https://github.com/vidanj/Gestion-Servicios-Profesionales/issues/189>
2. **Esta captura se toma después de integrar el PR #253**, cuando el issue aparezca cerrado con
   el aviso de que lo cerró ese PR.
3. Recortar el título con la insignia de cerrado y la línea que enlaza el PR.

### 10. Issue #187 — cerrado por cambio de enfoque

1. Abrir <https://github.com/vidanj/Gestion-Servicios-Profesionales/issues/187>
2. Bajar hasta el comentario titulado **«Superado por cambio de enfoque (spec 008)»**.
3. Recortar el comentario **con su tabla de versiones**: es la prueba de que los dos paquetes
   nunca publicaron versión estable, que es el argumento central de la sección 2.

---

## Bloque C — Ejecuciones del pipeline (8)

Todas salen de la pestaña **Actions** del repositorio, o de **Checks** dentro del PR #253.
Para cada una: abrir el job, **desplegar los pasos** y recortar la columna de pasos con sus
tiempos y sus marcas verdes.

| # | Job | Dónde | Qué debe verse |
|---|---|---|---|
| 11 | Formato y análisis | `backend-lint.yml` | El paso de CSharpier y analizadores en verde |
| 12 | Pruebas y cobertura | `backend-tests.yml` | **El resumen con 350 pruebas y 0 fallos** |
| 13 | Frontend | `frontend-tests.yml` | Compilación y Playwright en verde |
| 14 | Imagen | `docker-image.yml` | El paso «Esperar a que la sonda responda sana» |
| 15 | Validación del monitoreo | `monitoring-stack.yml`, job 1 | Los pasos de `promtool`, incluido **«Casos de prueba de las reglas»** |
| 18 | Humo del monitoreo | `monitoring-stack.yml`, job 2 | La salida del paso «Verificar el catálogo de métricas y las reglas», con la tabla completa en `[ok]` |

> La 18 es la más valiosa de este bloque: es la misma tabla que produce el guion en local, pero
> emitida por el pipeline. Para verla, abrir el job y desplegar ese paso concreto.

### 16 y 17 — Despliegue y reversión

**No se pueden tomar todavía.** El flujo `deploy.yml` está escrito y validado, pero nunca se ha
ejecutado porque necesita accesos que solo tiene quien administra el servicio. Pasos, en orden:

1. **Obtener el disparador.** Panel del proveedor → servicio de la API → *Settings* →
   *Deploy Hook* → copiar la URL.
2. **Guardarlo como secreto.** Repositorio → *Settings* → *Secrets and variables* → *Actions* →
   *New repository secret* → nombre `RENDER_DEPLOY_HOOK_URL`.
3. **Declarar la dirección pública.** En la pestaña *Variables* de esa misma pantalla, crear
   `RENDER_SERVICE_URL` con la dirección del servicio (sin barra final).
4. **Crear el ambiente.** *Settings* → *Environments* → *New environment* → nombre `production`
   → marcar *Required reviewers* y añadirse.
5. **Comprobar el parámetro a mano, antes de confiar en él.** En una consola:

   ```powershell
   curl -X POST "<URL del hook>&ref=<sha de un commit anterior>"
   ```

   Verificar **en el panel del proveedor** que la construcción que arranca corresponde a *ese*
   commit y no al último de la rama. Si no fuera así, la reversión automática no funcionaría y
   hay que pasar al plan B descrito en `research.md` (D6).
6. **Captura 16:** integrar un cambio trivial en `dev`, esperar a que corra `deploy.yml` y
   recortar **la página de resumen del job**, donde aparecen el commit desplegado, el anterior,
   la duración y la orden de reversión ya escrita.
7. **Captura 17:** *Actions* → *Despliegue* → *Run workflow* → poner en `sha` el commit sano
   anterior → ejecutar. Recortar el resumen igual que antes.
8. **Anotar los dos tiempos** y escribirlos en `docs/reporte/enlaces-monitoreo.json`, campos
   `duracion_despliegue` y `duracion_reversion`. Luego regenerar el documento.

---

## Bloque D — El sistema funcionando (4)

Estas son las que de verdad demuestran que el monitoreo sirve. Requieren levantar el entorno.

### Preparación (una sola vez)

```powershell
# 1. Añadir al .env:
#    GRAFANA_ADMIN_PASSWORD=la-que-prefieras

# 2. Levantar todo
.\scripts\monitoreo-up.ps1

# 3. Poblar con tráfico variado
.\scripts\generar-trafico.ps1 -Duracion 60

# 4. ESPERAR UN MINUTO. La aplicación exporta cada 60 s por omisión;
#    si se mira antes, los paneles salen vacíos sin que nada esté roto.

# 5. Comprobar que no falta nada
.\scripts\verificar-monitoreo.ps1
```

### 21. Tablero de salud de la API

1. Abrir <http://localhost:3001> e iniciar sesión (`admin` y la contraseña del `.env`).
2. Menú → *Dashboards* → carpeta **Gestión de Servicios Profesionales** → **GSP · Salud de la API**.
3. Arriba a la derecha, fijar el rango en **Last 1 hour**.
4. Recortar el tablero entero, sin el menú lateral de Grafana.
5. Comprobar antes de disparar: las cuatro cifras de arriba deben tener valores, y los paneles
   de latencia y errores, línea dibujada. Si algo está vacío, falta tráfico o falta esperar.

### 22. Tablero de negocio

Igual, pero en **GSP · Negocio**. Debe verse el panel de intentos de autenticación con sus tres
resultados y el de transiciones de estado con barras.

### 19. Alarma disparada

Esta es **la captura más importante de todo el documento**.

1. Con el entorno en marcha, detener la aplicación:

   ```powershell
   docker stop gsp-api   # el nombre exacto lo imprime monitoreo-up
   ```

2. **Esperar tres minutos.** La alarma pasa por un estado intermedio antes de dispararse; esa
   espera es deliberada y es lo que evita que un arranque en frío genere un aviso falso.
3. Abrir <http://localhost:9093>.
4. Recortar la alarma **desplegada**, de modo que se lean el nombre `ApiNoDisponible`, la
   severidad `critica`, y las tres anotaciones: resumen, descripción y **acción sugerida**.

> También vale la pena una variante en <http://localhost:9090/alerts>, donde se ve el estado
> *Pending* antes de *Firing*. Si la maquetación da espacio, es una buena segunda imagen.

### 20. Alarma resuelta

1. Volver a arrancar la aplicación:

   ```powershell
   docker start gsp-api
   ```

2. Esperar dos minutos.
3. Recargar <http://localhost:9093>: la alarma debe **haber desaparecido sola**, sin que nadie
   la cierre a mano.
4. Recortar la lista vacía. Para que se entienda, conviene capturarla junto al reloj o dejar
   visible el filtro, de modo que se vea que es la misma pantalla de antes.

### 18 (variante local). Salida del guion de verificación

Si se prefiere la versión local a la del pipeline, recortar la salida de
`.\scripts\verificar-monitoreo.ps1` con la tabla completa en verde y el mensaje final
**«Catálogo completo. El monitoreo está operativo.»**

### Al terminar

```powershell
.\scripts\monitoreo-down.ps1
```

---

## Lista de comprobación

| # | Captura | Estado |
|---|---|---|
| A1 | Diagrama de arquitectura de telemetría | **Ya generado** |
| A2 | Diagrama del flujo del pipeline | **Ya generado** |
| 1 | Issue #250 | Pendiente |
| 2 | PR #252 — conversación | Pendiente |
| 3 | PR #252 — archivos | Pendiente |
| 4 | PR #252 — comprobaciones | Pendiente |
| 5 | Issue #251 | Pendiente |
| 6 | PR #253 — conversación | Pendiente |
| 7 | PR #253 — archivos | Pendiente |
| 8 | PR #253 — comprobaciones | Pendiente |
| 9 | Issue #189 cerrado | Tras integrar el PR #253 |
| 10 | Issue #187 con su comentario | Pendiente |
| 11–15 | Ejecuciones de los cinco flujos | Pendiente |
| 16 | Despliegue verificado | **Bloqueada**: falta configurar el secreto |
| 17 | Reversión ejecutada | **Bloqueada**: ídem |
| 18 | Verificación del catálogo | Pendiente |
| 19 | Alarma disparada | Pendiente |
| 20 | Alarma resuelta | Pendiente |
| 21 | Tablero de salud | Pendiente |
| 22 | Tablero de negocio | Pendiente |

Las dos bloqueadas son las únicas que no dependen de tomar una imagen, sino de un acceso. El
documento las deja marcadas en amarillo a propósito: una evidencia que todavía no existe tiene
que verse que no existe.
