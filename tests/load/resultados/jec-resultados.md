# Resultados — Prueba de carga del listado de usuarios

**Integrante:** Jesús Efrén Campuzano (@vidanj) · **Script:** `tests/load/jecprueba.js`
**Endpoint:** `GET /api/Users` · **Fecha:** 2026-09-15
**Plan:** [`docs/plan-pruebas-carga-k6.md`](../../../docs/plan-pruebas-carga-k6.md)

---

## 1. Condiciones de la medición

| Aspecto | Valor |
|---|---|
| Herramienta | k6 v2.2.0 (commit 00a9a1b7f5, go1.26.5, windows/amd64) |
| Entorno | API local, no el despliegue en Render |
| Configuración de compilación | `Release` |
| Entorno de ejecución de ASP.NET | `Production` |
| Base de datos | PostgreSQL local, 203 usuarios activos |
| Página solicitada | `page=1`, `size=10` (los valores por defecto del endpoint) |
| Depurador adjunto | No |

**Máquina**

| Componente | Especificación |
|---|---|
| Equipo | LAPTOP-98GFGB54 |
| Procesador | AMD Ryzen 5 5600H (6 núcleos físicos, 12 lógicos) |
| Memoria | 15.3 GB |
| Sistema operativo | Windows 11 Home Single Language (10.0.26200) |

Cliente y servidor corren en la misma máquina, por lo que la latencia de red es
despreciable. La medición describe el costo de procesamiento de la aplicación y
de la base de datos, no el de la red.

**Sobre el limitador de tasa.** Esta prueba no necesitó elevar `AUTH_RATE_LIMIT`:
el limitador solo cubre los endpoints de `/api/Auth` y el script inicia sesión
una única vez por ejecución, en `setup()`. El endpoint medido no pasa por él.

## 2. Comandos ejecutados

**Terminal 1 — API**

```powershell
Get-Content .env | Where-Object { $_ -match '^\s*[^#].*=' } | ForEach-Object {
    $k, $v = $_ -split '=', 2
    [Environment]::SetEnvironmentVariable($k.Trim(), $v.Trim(), 'Process')
}
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run -c Release --project backend/SistemaServicios.API --no-launch-profile
```

**Terminal 2 — siembra de datos y prueba**

```powershell
./tests/load/seed-usuarios.ps1 -AdminUser <correo> -AdminPass <clave> -Cantidad 200

k6 run --summary-export=tests/load/resultados/jec-resumen.json `
  -e BASE_URL=http://localhost:5000 -e USER=<correo> -e PASS=<clave> `
  tests/load/jecprueba.js
```

> **La siembra no es un adorno.** La base de desarrollo tenía tres usuarios. El
> endpoint filtra por `Status`, ordena por fecha de creación, salta y toma la
> página y la proyecta a DTO: con tres filas ninguno de esos costos aparece y la
> cifra resultante no describiría el endpoint sino el viaje de ida y vuelta. Los
> 200 usuarios se crearon por la propia API (`POST /api/Users`), no con `INSERT`,
> de modo que pasaron por la validación y el hash de la aplicación.

## 3. Perfil de carga aplicado

| Etapa | Duración | Usuarios virtuales |
|---|---|---|
| Incremento gradual | 30 s | 0 → 5 |
| Sostenimiento | 1 min | 5 |
| Descenso | 30 s | 5 → 0 |

Umbral declarado: `http_req_duration: ['p(95)<5000']`. Pausa de 1 segundo entre
iteraciones como tiempo de reflexión. Duración total: 2 min 0.5 s.

## 4. Resultados

### 4.1 Nivel de servicio

| Indicador | Objetivo | Obtenido | Resultado |
|---|---|---|---|
| Percentil 95 del tiempo de respuesta | < 5 000 ms | **3.01 ms** | **Cumple** |

k6 terminó con código de salida 0. El valor obtenido es unas **1 660 veces
menor** que el umbral acordado.

### 4.2 Tiempo de respuesta

| Medida | Valor |
|---|---|
| Promedio | 2.51 ms |
| Mínimo | 0.52 ms |
| Mediana | 2.23 ms |
| Percentil 90 | 2.76 ms |
| **Percentil 95** | **3.01 ms** |
| Máximo | 127.96 ms |

### 4.3 Desglose del tiempo

| Fase | Promedio | Percentil 95 | Máximo |
|---|---|---|---|
| `http_req_waiting` (hasta el primer byte) | 2.12 ms | 2.77 ms | 125.92 ms |
| `http_req_receiving` | 0.39 ms | 1.05 ms | 2.16 ms |
| `http_req_connecting` | 0.007 ms | 0 ms | 1.60 ms |
| `http_req_blocked` | 0.035 ms | 0 ms | 14.89 ms |
| `http_req_sending` | 0.001 ms | 0 ms | 0.53 ms |
| `http_req_tls_handshaking` | 0 ms | 0 ms | 0 ms |

### 4.4 Verificaciones

| Verificación | Resultado |
|---|---|
| Responde 200 | 100 % |
| Devuelve la página | 100 % |
| No fue rechazado por autorización | 100 % |
| No fue rechazado por limitación de tasa | 100 % |

**Total:** 1 864 de 1 864 verificaciones satisfechas, 0 fallidas.

### 4.5 Volumen y errores

| Métrica | Valor |
|---|---|
| Peticiones totales | 467 |
| Tasa de peticiones | 3.88 por segundo |
| Peticiones fallidas | 0 de 467 (0 %) |
| Iteraciones completadas | 466, 0 interrumpidas |
| Usuarios virtuales alcanzados | 5 de 5 |
| Duración de iteración (promedio) | 1 002.68 ms |
| Datos recibidos | 1.34 MB (11.2 kB/s) |
| Datos enviados | 339 kB (2.8 kB/s) |
| CPU consumida por el backend durante la prueba | 1.66 s |
| Memoria de trabajo del backend al terminar | 171.8 MB |

## 5. Análisis

### 5.1 El máximo de 128 ms no es el listado: es el inicio de sesión

Hay 467 peticiones y 466 iteraciones. La sobrante es el `POST /api/Auth/login`
que `setup()` ejecuta una sola vez antes de repartir el token, y es la que
produce el máximo de 127.96 ms: cuarenta y dos veces el percentil 95 del resto.

No es una anomalía, es el costo de BCrypt. Verificar una contraseña está
diseñado para ser lento, y esa única petición arrastra el máximo de la
distribución. Lo confirma `http_req_waiting`, cuyo máximo es 125.92 ms: el
tiempo se fue esperando al servidor, no transfiriendo datos.

Es también la razón de que el patrón del plan —autenticarse una vez en `setup()`
y no una vez por iteración— sea correcto: pagar BCrypt 466 veces habría medido
el algoritmo de hash, no el listado.

### 5.2 El listado cuesta unas cincuenta veces menos que el inicio de sesión

| Endpoint | Percentil 95 |
|---|---|
| `POST /api/Auth/login` (ejecución de Antonio Zamorano) | 153.25 ms |
| `GET /api/Users` (esta ejecución) | 3.01 ms |

Comparar ambas cifras dice algo que ninguna dice por separado: el costo del
sistema bajo esta carga no está en consultar la base de datos, sino en la
criptografía del inicio de sesión. Una consulta paginada sobre 203 filas, con
filtro por `Status` y proyección a DTO, se resuelve en milisegundos de un dígito.

### 5.3 El servidor es el tiempo; la red no existe

`http_req_waiting` promedia 2.12 ms de los 2.51 ms totales: el 84 % del tiempo
es el servidor procesando. `http_req_connecting` promedia 7 microsegundos y
`http_req_tls_handshaking` es cero, porque cliente y servidor comparten máquina
y la prueba va sobre HTTP.

La consecuencia importante es de interpretación: estas cifras **no** son las que
vería un usuario real, que además paga latencia de red y TLS. Son el costo de
procesamiento de la aplicación, que es justo lo que el plan quería aislar.

### 5.4 La subida gradual absorbió el arranque en frío

El mínimo registrado es de 0.52 ms, muy por debajo del promedio, lo que indica
que ninguna petición cargó con la inicialización del conjunto de conexiones.

Ese costo existe y se midió aparte: la primera consulta a `/health/ready` tardó
1 379 ms, de los cuales 736 ms fueron la comprobación de PostgreSQL; la segunda
tardó 72 ms. La etapa de incremento gradual de 30 segundos absorbió esa
diferencia. Lanzar los cinco usuarios de golpe habría cargado ese segundo largo
a las primeras iteraciones.

### 5.5 El umbral acordado no discrimina en este endpoint

Con 3.01 ms contra 5 000 ms, el margen es de más de mil veces. El umbral se
cumple, pero no distingue entre un sistema sano y uno degradado: para que esta
prueba fallara, el endpoint tendría que volverse mil seiscientas veces más lento.

Es un hallazgo, no un defecto del ejercicio. El nivel de servicio se fijó como
criterio de aceptación y se cumple con holgura. Un umbral que discriminara en
este endpoint tendría que estar en el orden de las decenas de milisegundos, y
encontrar el punto de quiebre exigiría decenas o centenas de usuarios virtuales
—una prueba de estrés, fuera del alcance declarado en el plan.

### 5.6 Las guardias confirman que la medición es válida

Las cuatro verificaciones están al 100 %, incluidas las dos que existen para
detectar mediciones falsamente buenas: `401`/`403` y `429`. Sin ellas, un token
vencido o un rechazo del limitador habrían producido un percentil excelente
midiendo el middleware en vez del endpoint. Estar al 100 % es lo que permite
afirmar que las 466 peticiones del listado ejercitaron la consulta completa.

## 6. Conclusión

El listado paginado de usuarios cumple el nivel de servicio acordado con un
margen muy amplio bajo una carga de cinco usuarios virtuales concurrentes, sobre
una base de 203 usuarios. No se detectaron errores, rechazos ni degradación
durante los dos minutos de ejecución, y el consumo del backend fue de 1.66
segundos de CPU.

El hallazgo que deja la medición no es sobre el endpoint sino sobre el criterio:
el umbral de 5 000 ms es inservible para detectar una regresión en esta ruta.

## 7. Archivos de evidencia

| Archivo | Contenido |
|---|---|
| `jec-resumen.json` | Resumen exportado por k6 con todas las métricas |
| `jec-captura.png` | Captura de la salida completa de la ejecución |

### Advertencia sobre el resumen exportado

`--summary-export` escribe también un campo `setup_data` con **todo lo que
devuelve `setup()`**. Como este script devuelve el token de sesión, el archivo
contenía un JWT válido de la cuenta usada.

Se eliminó antes de versionarlo:

```powershell
python -c "import json; r='tests/load/resultados/jec-resumen.json'; d=json.load(open(r,encoding='utf-8')); d.pop('setup_data',None); json.dump(d,open(r,'w',encoding='utf-8'),indent=2,ensure_ascii=False)"
```

Afecta a cualquier script que se autentique en `setup()` —es decir, a todos los
endpoints autenticados del plan, no solo a este—, por lo que conviene incorporar
el paso al documento del plan. El script de Antonio no está afectado: mide un
endpoint público y no define `setup()`.

> Nota de lectura del JSON: en `thresholds`, `"p(95)<5000": false` significa que
> el umbral **no** se incumplió. Y en `http_req_failed`, `passes` cuenta las
> peticiones que sí fallaron: aquí vale 0, que es el resultado deseado.
