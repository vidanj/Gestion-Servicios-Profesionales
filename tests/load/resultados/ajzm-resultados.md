# Resultados — Prueba de carga del inicio de sesión

**Integrante:** Antonio Zamorano (@AntOnionPro) · **Script:** `tests/load/azprueba.js`
**Endpoint:** `POST /api/Auth/login` · **Fecha:** 2026-09-11
**Plan:** [`docs/plan-pruebas-carga-k6.md`](../../../docs/plan-pruebas-carga-k6.md)

---

## 1. Condiciones de la medición

| Aspecto | Valor |
|---|---|
| Herramienta | k6 v2.2.0 |
| Entorno | API local, no el despliegue en Render |
| Configuración de compilación | `Release` |
| Entorno de ejecución de ASP.NET | `Production` |
| Límite del limitador de tasa | `AUTH_RATE_LIMIT=10000` |
| Base de datos | PostgreSQL local |
| Depurador adjunto | No |

**Máquina**

| Componente | Especificación |
|---|---|
| Equipo | DESKTOP-IM3NQBO |
| Procesador | Intel Core i7-10750H @ 2.60 GHz |
| Memoria | 16 GB |
| Sistema operativo | Windows 11 Pro (10.0.26200) |

Cliente y servidor corren en la misma máquina, por lo que la latencia de red es
despreciable. La medición describe el costo de procesamiento de la aplicación y
de la base de datos, no el de la red.

## 2. Comandos ejecutados

**Terminal 1 — API**

```powershell
Get-Content .env | Where-Object { $_ -match '^\s*[^#].*=' } | ForEach-Object {
    $k, $v = $_ -split '=', 2
    [Environment]::SetEnvironmentVariable($k.Trim(), $v.Trim(), 'Process')
}
$env:ASPNETCORE_ENVIRONMENT="Production"
$env:AUTH_RATE_LIMIT=10000
dotnet run -c Release --project backend/SistemaServicios.API --no-launch-profile
```

**Terminal 2 — prueba**

```powershell
k6 run --summary-export=tests/load/resultados/az-resumen.json tests/load/azprueba.js
```

> Levantar la API en `Production` sin el perfil de arranque exige cargar `.env`
> manualmente: el proyecto no lo lee por sí solo. Omitir ese paso deja la API sin
> acceso a la base de datos y la sonda de disponibilidad responde con error.

## 3. Perfil de carga aplicado

| Etapa | Duración | Usuarios virtuales |
|---|---|---|
| Incremento gradual | 30 s | 0 → 5 |
| Sostenimiento | 1 min | 5 |
| Descenso | 30 s | 5 → 0 |

Umbral declarado: `http_req_duration: ['p(95)<5000']`. Pausa de 1 segundo entre
iteraciones como tiempo de reflexión.

## 4. Resultados

### 4.1 Nivel de servicio

| Indicador | Objetivo | Obtenido | Resultado |
|---|---|---|---|
| Percentil 95 del tiempo de respuesta | < 5 000 ms | **153.25 ms** | **Cumple** |

Margen: el valor obtenido es aproximadamente **32 veces menor** que el umbral
acordado.

### 4.2 Tiempo de respuesta

| Medida | Valor |
|---|---|
| Promedio | 131.52 ms |
| Mínimo | 118.86 ms |
| Mediana | 127.72 ms |
| Percentil 90 | 148.79 ms |
| **Percentil 95** | **153.25 ms** |
| Máximo | 197.27 ms |

### 4.3 Verificaciones

| Verificación | Resultado |
|---|---|
| Responde 200 | 100 % |
| Devuelve token | 100 % |
| No fue rechazado por limitación de tasa | 100 % |

**Total:** 1 242 de 1 242 verificaciones satisfechas, 0 fallidas.

### 4.4 Volumen y errores

| Métrica | Valor |
|---|---|
| Peticiones totales | 414 |
| Tasa de peticiones | 3.44 por segundo |
| Peticiones fallidas | 0 de 414 (0 %) |
| Iteraciones completadas | 414, 0 interrumpidas |
| Usuarios virtuales alcanzados | 5 de 5 |
| Duración de iteración (promedio) | 1.13 s |
| Datos recibidos | 420 kB |
| Datos enviados | 80 kB |
| Duración total de la ejecución | 2 min 0.2 s |

La duración de iteración de 1.13 segundos corresponde a la pausa de 1 segundo
más el tiempo de respuesta. Es coherente con las cifras anteriores y confirma que
no hubo esperas inesperadas.

## 5. Análisis

### 5.1 El sistema no se degrada bajo esta concurrencia

La dispersión es estrecha: 79 milisegundos entre el mínimo y el máximo a lo largo
de 414 peticiones. En un sistema que estuviera acercándose a su límite, se
esperaría una cola creciente y un máximo muy separado del promedio.

No la hay. Con cinco usuarios virtuales el endpoint no entra en contención: ni en
el conjunto de conexiones a la base de datos, ni en el cálculo de la verificación
de contraseña, que es la parte más costosa en CPU de esta ruta.

### 5.2 La subida gradual evitó contaminar la medición

El mínimo registrado es de 118.86 ms, muy cerca del promedio. Si la prueba
hubiera lanzado los cinco usuarios de golpe, las primeras iteraciones habrían
incluido el costo de inicializar el conjunto de conexiones.

Ese costo se observó al comprobar la disponibilidad antes de la prueba: la
primera consulta a PostgreSQL tardó 777 ms, frente a los valores de dos dígitos
posteriores. La etapa de incremento gradual de 30 segundos absorbió ese arranque.

### 5.3 El umbral acordado no discrimina en este sistema

Con un percentil 95 de 153 ms contra un umbral de 5 000 ms, la prueba pasa sin
tensión. **El umbral no distingue entre un sistema sano y uno degradado a este
nivel de carga.** Es un hallazgo, no un defecto del ejercicio: el nivel de
servicio se fijó como criterio de aceptación, y se cumple con holgura.

Encontrar el punto donde el sistema cede exigiría subir a decenas o centenas de
usuarios virtuales, lo que ya es una prueba de estrés y queda fuera del alcance
declarado en el plan.

### 5.4 La guardia contra el limitador demostró su utilidad

La verificación "no fue rechazado por limitación de tasa" está al 100 %, lo que
confirma que las 414 peticiones ejercitaron el inicio de sesión completo.

Sin esa verificación, un percentil de 153 ms sería indistinguible del resultado
que produciría el limitador rechazando todo: un rechazo se resuelve en el
middleware sin consultar la base de datos ni verificar la contraseña, de modo que
también daría cifras excelentes. La verificación es lo que permite afirmar que la
medición corresponde al endpoint y no al middleware.

## 6. Conclusión

El endpoint de inicio de sesión cumple el nivel de servicio acordado con un
margen amplio bajo una carga de cinco usuarios virtuales concurrentes. No se
detectaron cuellos de botella, errores ni degradación durante los dos minutos de
ejecución.

## 7. Archivos de evidencia

| Archivo | Contenido |
|---|---|
| `az-resumen.json` | Resumen exportado por k6 con todas las métricas |
| `az-captura.png` | Captura de la salida completa de la ejecución |
