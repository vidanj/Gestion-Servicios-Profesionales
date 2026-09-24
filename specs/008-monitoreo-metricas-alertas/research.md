# Research — Spec 008

> Decisiones técnicas con sus alternativas descartadas. Cada una responde a una pregunta que el
> [spec](spec.md) deja abierta a propósito, porque es del **cómo**.
>
> Las consultas a los repositorios de paquetes se hicieron el **2026-09-23**; donde una decisión
> depende de una versión, se indica para que pueda reevaluarse.

---

## D1 — Colector intermedio en lugar de exportador de Prometheus dentro del proceso

**Pregunta:** ¿cómo llegan las métricas de la API a Prometheus?

**Decisión:** la API exporta por OTLP y un OpenTelemetry Collector publica en formato Prometheus. La
aplicación **no** expone un endpoint de métricas propio.

**Razones:**

1. **El exportador en proceso no tiene versión estable.** Consulta del 2026-09-23:

   | Paquete | Última versión | Última **estable** |
   |---|---|---|
   | `OpenTelemetry.Exporter.Prometheus.AspNetCore` | `1.19.1-beta.1` | **ninguna** |
   | `OpenTelemetry.Instrumentation.EntityFrameworkCore` | `1.19.0-beta.1` | **ninguna** |
   | `OpenTelemetry.Exporter.OpenTelemetryProtocol` | `1.19.1` | `1.19.1` (ya instalado en `1.17.0`) |

   Ninguno de los dos primeros ha publicado jamás una versión estable. El issue #187 suponía que era
   cuestión de esperar a que salieran de preestreno; año y medio después siguen ahí. El Principio VI
   prohíbe silenciar advertencias, y `Directory.Build.props` las trata como errores: el paquete no
   cabe. El exportador OTLP, en cambio, es estable y **ya está instalado**.

2. **Un endpoint de métricas en una API pública es superficie de información.** Revela rutas,
   volúmenes de uso y versiones. Protegerlo exigiría autenticación propia o filtrado por red, y la
   plataforma de alojamiento no ofrece lo segundo.

3. **El colector desacopla.** La aplicación empuja y no le importa quién consume. Sustituir
   Prometheus por otro sistema no toca una línea de C#.

**Alternativas descartadas:**

| Alternativa | Por qué no |
|---|---|
| Exportador de Prometheus en proceso | Solo existe en preestreno; choca con el Principio VI |
| `prometheus-net.AspNetCore` (estable) | Convive un segundo sistema de métricas junto al de OpenTelemetry ya instalado: dos catálogos, dos convenciones de nombres y el doble de superficie que mantener |
| No hacer nada y esperar | Es lo que se decidió en #187 y llevó a que la instrumentación existiera sin que nadie la consumiera |

**Consecuencia verificable:** una prueba de integración afirma que `GET /metrics` responde 404. Sin
ella, alguien añadiría el endpoint más adelante sin advertir que contradice esta decisión.

**Cuándo reevaluar:** si el exportador publica una versión estable y aparece una razón para que la
aplicación sirva sus propias métricas (por ejemplo, un Prometheus que solo sepa raspar y no reciba).

---

## D2 — Entorno de monitoreo en su propio archivo de composición

**Pregunta:** ¿se añaden los servicios al archivo de composición existente o se crea uno nuevo?

**Decisión:** uno nuevo, en `monitoring/`, siguiendo el precedente de `sonarqube/`.

**Razones:**

1. **Ciclo de vida inverso.** El monitoreo tiene que seguir en pie precisamente cuando la aplicación
   se cae. Si comparten proyecto de composición, derribar la aplicación se lleva por delante la
   evidencia del incidente.
2. **Coste de arranque.** El archivo raíz existe con un propósito declarado en su propia cabecera:
   verificar el comportamiento de las cabeceras reenviadas contra un proxy real. Añadirle cinco
   servicios convierte una comprobación de veinte segundos en un arranque de dos minutos para quien
   solo quiere probar el proxy.
3. **Precedente vigente.** `sonarqube/docker-compose.yml` ya estableció la convención: herramienta
   auxiliar, archivo propio, cabecera explicativa y volúmenes nombrados.

**Cómo se conectan sin acoplarse:** un archivo de superposición añade la variable del colector al
servicio de la aplicación y lo une a la red del monitoreo, declarada como externa. El archivo raíz no
se modifica.

**Alternativa descartada:** un único archivo con perfiles de composición. Reduce el número de
archivos pero no resuelve el problema del ciclo de vida, que es la razón principal.

---

## D3 — Sondeo externo como única vía de observar el servicio publicado

**Pregunta:** ¿cómo se observa el servicio desplegado, si su contenedor no puede alcanzar un colector
que corre en el equipo de una persona?

**Decisión:** no se intenta. El servicio publicado se observa **desde fuera**, sondeando sus sondas
de salud con un *blackbox exporter*.

**Razones:** el colector es local y no tiene dirección pública; exponerlo sería abrir un receptor de
telemetría a internet. El sondeo externo, en cambio, da exactamente los dos datos que el caso de
estudio necesita del entorno real: **disponibilidad** y **duración del arranque en frío**.

**Detalle que importa:** no basta con comprobar el código 200. La sonda de preparación responde con
un cuerpo que indica el estado, y un estado degradado podría devolver 200. Por eso el módulo de
sondeo afirma también el contenido del cuerpo.

**Consecuencia:** el monitoreo tiene dos alcances distintos y el documento debe decirlo sin
ambigüedad — métricas internas detalladas del entorno local, y disponibilidad observada desde fuera
del entorno publicado.

---

## D4 — Los nombres de la instrumentación del tiempo de ejecución cambiaron

**Pregunta:** ¿qué métricas produce `OpenTelemetry.Instrumentation.Runtime` y cómo se llaman?

**Decisión:** se añade el paquete en versión **estable**, y el catálogo y las alertas usan los
nombres `dotnet.*`.

**Dato verificado (2026-09-23):** el paquete tiene 34 versiones publicadas; la última estable es
`1.19.0`, y `1.17.0` —la que empata con los otros cuatro paquetes de OpenTelemetry ya instalados—
también es estable. Se fija **1.17.0** por coherencia con el resto del archivo de proyecto.

**El detalle que hace falta documentar:** en la versión actual de la plataforma, este paquete ya no
recolecta por su cuenta ni emite los nombres `process.runtime.dotnet.*`. Lo que hace es registrar el
medidor integrado de la plataforma, cuyos instrumentos se llaman `dotnet.*`. Casi toda la
documentación y los tableros publicados que se encuentran buscando fueron escritos para versiones
anteriores y usan los nombres viejos: copiarlos produce un tablero que se aprovisiona sin error y
aparece permanentemente vacío.

**Consecuencia:** el catálogo de [data-model.md](data-model.md) es la referencia, y el guion de
verificación comprueba cada nombre contra el sistema real en lugar de darlo por bueno.

---

## D5 — El medidor del limitador de peticiones se registra explícitamente

**Pregunta:** ¿por qué no aparecen las métricas del limitador si la instrumentación de ASP.NET Core
ya está activada?

**Decisión:** se añade el medidor del limitador por su nombre, además de la instrumentación general.

**Razón:** son medidores distintos. La instrumentación general cubre peticiones, conexiones y
enrutamiento, pero el limitador publica en un medidor propio que hay que pedir explícitamente. Sin
esa línea no hay forma de ver cuántas peticiones rechaza el limitador de autenticación, que es
justamente la señal que distingue "nadie entra" de "alguien está probando contraseñas".

**Por qué importa aquí:** el proyecto ya tiene un limitador configurado sobre el inicio de sesión,
con un límite parametrizable. Esta es la métrica que permite comprobar si ese límite está bien
calibrado, en lugar de discutirlo.

---

## D6 — Reversión indicando el commit al disparar el hook

**Pregunta:** ¿cómo se vuelve a una versión anterior?

**Decisión:** se redespliega el commit sano anterior, indicándolo como parámetro al disparar el
*deploy hook*. No se usa la interfaz de programación de la plataforma.

**Razones:** el hook es un único secreto, no requiere gestionar credenciales de interfaz de
programación, y hace que desplegar y revertir sean **la misma operación** con distinto argumento —
lo que significa que la reversión se ejercita cada vez que se despliega, en lugar de ser un camino
que nadie ha recorrido hasta el día del incidente.

**Riesgo asumido y cómo se cubre:** el comportamiento del parámetro de commit se comprueba a mano
antes de escribir el flujo automatizado. Si no funcionara como se espera, la alternativa es la
interfaz de programación de la plataforma, cuyo secreto ya está previsto como opcional en el
contrato del despliegue.

**Límite documentado:** una migración destructiva **no** se revierte redesplegando la imagen
anterior. El resultado de cada despliegue lo advierte expresamente, y la recuperación de datos pasa
por los guiones de respaldo, que son destructivos y exigen confirmación (Principio IV).

---

## D7 — La caducidad de series es el mecanismo de detección de ausencia

**Pregunta:** ¿cómo se distingue "la aplicación va bien y está inactiva" de "la aplicación dejó de
informar"?

**Decisión:** el colector caduca las series que llevan cinco minutos sin recibir datos.

**Razón:** sin caducidad, el colector seguiría publicando el último valor conocido
indefinidamente. Un tablero mostraría cifras plausibles de una aplicación que lleva horas muerta, y
ninguna alerta se dispararía, porque los datos siguen llegando. Al caducar, la serie desaparece, y
la regla que comprueba la ausencia puede detectarlo.

**Consecuencia de diseño:** la alerta de ausencia de métricas espera más que la caducidad, para no
confundir un hueco de recolección con una ausencia real.

---

## D8 — El acuerdo de latencia hereda el umbral de las pruebas de carga

**Pregunta:** ¿qué umbral de latencia se declara como nivel de servicio?

**Decisión:** dos niveles. Objetivo interno **1,5 s**; acuerdo formal **5 s**, heredado del plan de
pruebas de carga ya vigente en el proyecto.

**Razón:** el proyecto ya fijó un umbral de 5 s en el percentil 95 como criterio de fallo de sus
pruebas de carga. Declarar aquí un acuerdo distinto crearía dos verdades sobre la misma magnitud, y
la primera vez que discreparan nadie sabría cuál rige. Heredarlo mantiene una sola fuente.

**Por qué además hay un objetivo interno más estricto:** 5 s es el punto en el que la experiencia ya
es mala; esperar a rozarlo para reaccionar es llegar tarde. El objetivo interno da margen, y por eso
avisa con severidad menor mientras que el acuerdo formal avisa como incumplimiento.

---

## D9 — El medidor de negocio es de instancia única y no puede lanzar

**Pregunta:** ¿cómo se expone el medidor de negocio a la capa de servicios?

**Decisión:** un contrato en `Interfaces/` con una implementación de instancia única, inyectada como
cualquier otra dependencia, que libera sus recursos al cerrar y cuyos métodos nunca propagan
excepciones.

**Razones:**

1. **Instancia única** porque un medidor es un recurso de proceso: crear uno por petición
   multiplicaría los ámbitos y la memoria sin ganar nada. Los instrumentos se crean una sola vez, en
   el constructor.
2. **Libera sus recursos** porque el análisis estático del proyecto exige disponer de lo desechable,
   y el build trata sus advertencias como errores.
3. **No puede lanzar** porque una métrica que rompe un inicio de sesión es peor que no tener
   métrica. Es un requisito funcional con su propia prueba, no una buena intención.
4. **El nombre del medidor es una constante compartida** entre la implementación y el registro de
   telemetría, siguiendo el criterio que ya usa el proyecto para el nombre de la variable del
   colector: dos literales iguales en archivos distintos acaban divergiendo.

**Alternativa descartada:** emitir las métricas desde los controladores. Viola el Principio II y,
además, mide lo que entró por HTTP en lugar de lo que el negocio decidió, que es justo la diferencia
que estas métricas existen para capturar.
