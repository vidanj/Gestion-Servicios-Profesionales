# Plan de Implementación de Pruebas de Carga con k6

Sistema de Gestión de Servicios Profesionales — Unidad 2

---

## 1. Objetivo y alcance

Establecer el procedimiento con el que se ejecutarán pruebas de carga sobre la
API del sistema, con el fin de identificar los límites operativos de la
aplicación: latencia bajo concurrencia, tasa de error y cuellos de botella en
los endpoints de uso más frecuente y en los procesos críticos.

Este documento **precede a la ejecución**. Define qué se va a medir, con qué
configuración y bajo qué criterio se considera aceptable el resultado, de modo
que las mediciones de todos los integrantes sean comparables entre sí. Los
resultados obtenidos se entregan por separado.

**Dentro del alcance:** endpoints de la API REST (backend ASP.NET Core).

**Fuera del alcance:** rendimiento del frontend Next.js, pruebas de estrés hasta
el punto de ruptura, y pruebas de resistencia (*soak testing*) de larga duración.

---

## 2. Nivel de servicio acordado

| Indicador | Objetivo |
|---|---|
| Percentil 95 del tiempo de respuesta (`p(95)`) | menor a 5 000 ms |


Se usa el percentil 95 y no el promedio porque el promedio oculta la cola de
peticiones lentas: un endpoint con media de 300 ms puede tener un 5 % de
peticiones por encima de 4 s, y son esas las que percibe el usuario como fallo.
El `p(95)` describe la experiencia del peor 5 % en condiciones normales.

Ambos valores se declaran como `thresholds` dentro de cada script, de forma que
k6 devuelva un código de salida distinto de cero cuando no se cumplan. Esto
permite que la prueba funcione como criterio automatizable si más adelante se
integra al flujo de CI/CD.

---

## 3. Herramienta seleccionada

**k6 v2.2.0**

Se elige k6 sobre JMeter y Apache Benchmark por tres razones:

- Los escenarios se escriben en JavaScript y viven en el repositorio como código
  versionable, revisable en un *pull request* y diferenciable entre commits. Los
  planes de JMeter son XML generado por una interfaz gráfica, incómodo de revisar.
- Expone umbrales (`thresholds`) como concepto de primera clase y devuelve un
  código de salida acorde, lo que lo hace directamente integrable a un pipeline.
- Es un único binario sin dependencias de tiempo de ejecución, a diferencia de
  JMeter, que requiere una JVM instalada en cada máquina del equipo.

Apache Benchmark se descarta por no permitir escenarios con varios pasos: no
puede autenticarse y después usar el token obtenido, que es justo lo que
requieren la mayoría de los endpoints de este sistema.

---

## 4. Instalación

k6 es un ejecutable del sistema operativo, **no una dependencia del repositorio**.
No se agrega a `package.json` ni se versiona el binario.

### Windows

```powershell
choco install k6
```

Alternativa sin Chocolatey:

```powershell
winget install k6.k6
```

### Linux / macOS

```bash
# macOS
brew install k6

# Debian / Ubuntu
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

### Verificación

```powershell
k6 version
# k6.exe v2.2.0 (commit/00a9a1b7f5, go1.26.5, windows/amd64)
```

> Si el comando no se reconoce inmediatamente después de instalar, la variable
> `PATH` de la sesión activa aún no incluye la ruta del binario. Cierre y abra la
> terminal, o ejecute `refreshenv` en PowerShell.

---

## 5. Entorno de ejecución

Las pruebas se ejecutan **contra la API levantada en la máquina local**, no
contra el despliegue en Render.

```powershell
# Terminal 1: levantar la API en configuración de pruebas de carga
$env:AUTH_RATE_LIMIT=10000
dotnet run -c Release --project backend/SistemaServicios.API

# Terminal 2: ejecutar la prueba
k6 run tests/load/<script>.js
```

URL base: `http://localhost:5000`

### Justificación

Medir contra el entorno desplegado produciría cifras que no describen el
rendimiento de la aplicación, sino el de la infraestructura intermedia:

- **Arranque en frío.** La instancia en Render se suspende por inactividad. La
  primera petición de una prueba incluiría decenas de segundos de arranque del
  contenedor, más el tiempo que `entrypoint.sh` dedica a aplicar migraciones
  antes de que Kestrel escuche. Ese valor contaminaría el `p(95)` sin relación
  alguna con el código.
- **Cloudflare.** Está configurado delante de la API y puede aplicar limitación
  de tasa o desafíos ante un patrón de peticiones concurrentes desde una misma
  dirección, que es exactamente lo que genera una prueba de carga. El resultado
  serían errores de red, no mediciones.
- **Recursos compartidos.** El plan de alojamiento no garantiza CPU dedicada,
  por lo que dos ejecuciones idénticas darían resultados distintos y las
  mediciones no serían reproducibles.

Ejecutar en local aísla la variable que interesa: el comportamiento de la API y
de PostgreSQL bajo concurrencia.

### Condiciones para una medición válida

Para que los resultados de los distintos integrantes sean comparables, cada
ejecución debe cumplir:

- Backend compilado en configuración `Release`, no `Debug`.
- Base de datos con migraciones aplicadas y con un volumen de datos semejante
  entre integrantes (relevante sobre todo para el listado paginado de usuarios).
- Sin depurador adjunto al proceso.
- Registrar en la evidencia el sistema operativo, CPU y memoria de la máquina
  donde se ejecutó.
- Limitador de tasa de autenticación elevado mediante `AUTH_RATE_LIMIT`, según
  lo descrito en la sección 6.

---

## 6. Autenticación durante las pruebas

**No se implementa una ruta de omisión (*bypass*) de autenticación.**

La práctica habitual de exponer un endpoint sin autenticar para facilitar las
pruebas se justifica cuando el inicio de sesión depende de un proveedor externo
(OAuth, SSO) o resulta prohibitivamente costoso de repetir. No es el caso aquí:

- `POST /api/Auth/login` es un endpoint **público** del propio sistema, y además
  es uno de los procesos críticos que se desea medir. Omitirlo eliminaría del
  alcance justo lo que interesa observar.
- Para los endpoints autenticados, k6 permite iniciar sesión **una sola vez** en
  la función `setup()` y distribuir el token resultante entre todos los usuarios
  virtuales. El costo de autenticación se paga una vez por ejecución, no una vez
  por iteración.

Además, agregar una ruta que responde sin verificar credenciales introduce en el
código de producción un camino cuya activación depende de configuración. Un
error en esa configuración expondría el sistema. El beneficio no compensa el
riesgo cuando existe una alternativa que no toca el backend.

### Patrón para endpoints autenticados

```javascript
export function setup() {
  const res = http.post(
    `${BASE_URL}/api/Auth/login`,
    JSON.stringify({ email: USER, password: PASS }),
    { headers: { 'Content-Type': 'application/json' } }
  );
  return { token: res.json('token') };
}

export default function (data) {
  const params = { headers: { Authorization: `Bearer ${data.token}` } };
  http.get(`${BASE_URL}/api/Users`, params);
}
```

> Los nombres de los campos del cuerpo de la petición y de la propiedad que
> contiene el token deben confirmarse en `/swagger` antes de escribir el script.

### Credenciales

Las credenciales del usuario de prueba **no se escriben en el script**. Se pasan
por variable de entorno en el momento de ejecutar:

```powershell
k6 run -e BASE_URL=http://localhost:5000 -e USER=prueba@ejemplo.com -e PASS=**** tests/load/<script>.js
```

Dentro del script se leen con `__ENV.USER`. De este modo el repositorio no
contiene credenciales, aunque sean de un entorno local.

### Limitador de tasa en el endpoint de autenticación

El sistema aplica limitación de tasa sobre `/api/Auth/login`: cinco peticiones
por ventana de 60 segundos y por dirección IP, según el caso de prueba CP-SEC-02
del plan de pruebas del proyecto. La sexta petición dentro de la ventana recibe
`429 Too Many Requests`.

**Esta configuración impide medir el endpoint.** Los usuarios virtuales se
ejecutan desde una sola máquina y comparten dirección IP, por lo que a partir de
la quinta petición todas las respuestas serían rechazos del limitador. El
problema no es que la prueba falle, sino que **aparentaría tener éxito**: un
`429` se resuelve en el middleware sin consultar la base de datos ni verificar la
contraseña, de modo que el tiempo de respuesta resultaría excelente y el `p(95)`
reportado describiría el costo del limitador, no el del inicio de sesión.

Para el entorno de pruebas de carga, el límite se eleva mediante la variable de
entorno `AUTH_RATE_LIMIT`:

```powershell
$env:AUTH_RATE_LIMIT=10000
```

El valor por defecto se mantiene en 5, de modo que ningún otro entorno cambia de
comportamiento. A diferencia de una ruta que omite la autenticación, esta
parametrización no introduce un camino sin verificación de credenciales: el
inicio de sesión sigue validando la contraseña y emitiendo el token igual que
siempre. Lo único que se ajusta es el umbral a partir del cual se rechazan
peticiones por frecuencia.

> La implementación de esta variable es un cambio en el backend y se atiende como
> un issue independiente, previo a la ejecución de las pruebas.

---

## 7. Endpoints bajo prueba

### Selección

| Endpoint | Método | Acceso | Qué ejercita |
|---|---|---|---|
| `/api/Auth/login` | `POST` | Público | Proceso crítico. Verificación de contraseña y emisión de JWT. Es el camino con mayor costo de CPU del sistema. |
| `/api/Users` | `GET` | Autenticado | Lectura más frecuente. Consulta paginada que aprovecha el índice parcial sobre `Users.Status`. |
| `/api/Users/{id}` | `GET` | Autenticado | Búsqueda puntual por UUID. Contrasta el costo de una consulta indexada simple frente al listado. |
| `/api/Auth/me` | `GET` | Autenticado | Validación de JWT con acceso mínimo a base de datos. Sirve de referencia para aislar el costo de la capa de autenticación. |
| `/health/ready` | `GET` | Público | Línea base. Comprueba la alcanzabilidad de PostgreSQL con una carga de trabajo casi nula. |

> **Pendiente de verificación.** El plan de pruebas del proyecto refiere las
> rutas en minúsculas (`/api/auth/login`) mientras que el README las documenta
> capitalizadas (`/api/Auth/login`), y menciona un endpoint
> `POST /api/profile/image` que no figura en la tabla de endpoints del README.
> La forma exacta de cada ruta debe confirmarse en `/swagger` antes de escribir
> los scripts.

### Endpoints excluidos deliberadamente

**`POST /api/Admin/backup`.** Ejecuta un volcado SQL completo de la base de
datos. Bajo concurrencia generaría múltiples procesos de volcado simultáneos por
cada iteración, saturando disco y base de datos. No es representativo de carga
de usuario y puede dejar el entorno inutilizable.

**`POST /api/Auth/register`.** Escribe usuarios reales y valida unicidad de
correo electrónico. A partir de la segunda iteración todas las peticiones
fallarían por duplicado, contaminando la tasa de error. Probarlo exigiría generar
direcciones únicas por iteración y limpiar los registros después, lo que desvía
el esfuerzo del objetivo de esta entrega.

**`PUT` y `DELETE` sobre `/api/Users`.** Modifican estado y alterarían las
condiciones de las pruebas de lectura ejecutadas por otros integrantes sobre la
misma base.

### Asignación por integrante

Cada integrante ejecuta un script propio sobre un endpoint distinto.

| Integrante | Endpoint asignado | Script |
|---|---|---|
| *Antonio de Jesus Zamorano Mendez* | `POST /api/Auth/login` | `ajzmprueba.js` |
| *Jesus Efren Campuzano* | `GET /api/Users` | `<iniciales>prueba.js` |
| *Demian Leonardo Soto Guerrero* | `GET /api/Users/{id}` | `<iniciales>prueba.js` |
| *Federico Gutierrez* | `GET /api/Auth/me` | `<iniciales>prueba.js` |

---

## 8. Configuración de los escenarios

Todos los scripts comparten el mismo perfil de carga para que los resultados
sean comparables entre endpoints.

```javascript
export const options = {
  stages: [
    { duration: '30s', target: 5 },   // incremento gradual
    { duration: '1m',  target: 5 },   // sostenimiento
    { duration: '30s', target: 0 },   // descenso
  ],
  thresholds: {
    http_req_duration: ['p(95)<5000'],
  },
};
```

| Parámetro | Valor | Razón |
|---|---|---|
| Usuarios virtuales | 5 | Mínimo establecido para la entrega. |
| Duración total | 2 min | Suficiente para acumular iteraciones y estabilizar el percentil, sin prolongar la ejecución. |
| Incremento gradual | 30 s | Evita que el arranque en frío de la aplicación distorsione las primeras mediciones. |
| Sostenimiento | 1 min | Ventana en la que se observa el comportamiento estable. Es el tramo que interesa reportar. |

El incremento gradual es deliberado: lanzar los 5 usuarios de golpe mide el
arranque del proceso, no su rendimiento en operación.

---

## 9. Métricas a recolectar
 
De la salida de k6 se registran, como mínimo:
 
| Métrica | Qué indica |
|---|---|
| `http_req_duration` | Tiempo total de respuesta. Se reportan `avg`, `min`, `med`, `p(90)`, `p(95)` y `max`. Es la métrica del acuerdo de nivel de servicio. |
| `http_req_failed` | Proporción de peticiones fallidas. Un `p(95)` bueno con tasa de error alta no es un buen resultado. |
| `http_reqs` | Total de peticiones y tasa por segundo. Es el rendimiento efectivo alcanzado. |
| `iterations` | Iteraciones completadas y su tasa. |
| `vus` / `vus_max` | Confirma que la concurrencia configurada se alcanzó realmente. |
| `http_req_waiting` | Tiempo hasta el primer byte. Aísla el procesamiento del servidor del tiempo de transferencia. |
| `http_req_connecting` | Tiempo de establecimiento de conexión. Si es alto, el cuello de botella es de red, no de aplicación. |
| `data_received` / `data_sent` | Volumen transferido. Relevante en el listado paginado. |
| `checks` | Proporción de verificaciones satisfechas, si el script las define. |
 
Adicionalmente se registra el uso de CPU y memoria del proceso del backend
durante la ejecución, obtenido del Administrador de tareas o con:

```powershell
Get-Process -Name SistemaServicios.API | Select-Object CPU, WorkingSet
```

### Exportación de resultados

Para conservar evidencia más allá de la captura de pantalla:

```powershell
k6 run --summary-export=resultados/<iniciales>-resumen.json tests/load/<script>.js
```
> En el JSON exportado, `http_req_failed` reporta `passes` como el número de
> peticiones que **sí fallaron**, y `value` como la proporción de fallos. Un
> `value` de 0 es el resultado deseado.
---

## 10. Convenciones del repositorio

### Ubicación

```
/repo-root
   /docs
      plan-pruebas-carga-k6.md        # este documento
   /tests
      /load
         <iniciales>prueba.js         # un script por integrante
         /resultados
            <iniciales>-resumen.json
            <iniciales>-captura.png
```

### Nomenclatura

Los scripts se nombran con las iniciales del integrante seguidas de `prueba.js`,
en minúsculas y sin separadores. Ejemplo: un integrante con iniciales `AZ`
entrega `azprueba.js`.

---

## 11. Comandos de ejecución

### Ejecución estándar

```powershell
k6 run -e BASE_URL=http://localhost:5000 tests/load/<iniciales>prueba.js
```

### Con credenciales para endpoints autenticados

```powershell
k6 run -e BASE_URL=http://localhost:5000 -e USER=prueba@ejemplo.com -e PASS=**** tests/load/<iniciales>prueba.js
```

### Sobreescribiendo el perfil desde la línea de comandos

Útil para una comprobación rápida sin modificar el script:

```powershell
k6 run --vus 5 --duration 30s tests/load/<iniciales>prueba.js
```

> Las opciones de la línea de comandos tienen precedencia sobre `options` en el
> script. La evidencia de la entrega debe generarse **sin** estas banderas, para
> que todos midan bajo el mismo perfil.

### Con exportación de resumen

```powershell
k6 run --summary-export=tests/load/resultados/<iniciales>-resumen.json -e BASE_URL=http://localhost:5000 tests/load/<iniciales>prueba.js
```

---

## 12. Criterios de aceptación

Una ejecución se considera válida para la entrega cuando:

- [ ] Se alcanzaron al menos 5 usuarios virtuales concurrentes.
- [ ] La ejecución completó las tres etapas del perfil de carga.
- [ ] `http_req_duration p(95)` es menor a 5 000 ms.
- [ ] Se conservan la salida completa de k6 y la captura de pantalla.
- [ ] Se documentaron las características de la máquina donde se ejecutó.
- [ ] No se registraron respuestas `429` atribuibles al limitador de tasa.

Un umbral incumplido **no invalida la entrega**: constituye un hallazgo. En ese
caso se documenta la cifra obtenida y se plantea una hipótesis sobre su causa,
que es precisamente el propósito de una prueba de carga.

---

## 13. Trabajo posterior a esta entrega

- **Ejecución y evidencia.** Scripts individuales, resultados y capturas.
- **Integración al flujo de CI/CD.** Ejecución de las pruebas como paso previo al
  commit (Husky) o dentro de GitHub Actions, habilitada mediante variable de
  entorno para no penalizar el ciclo de desarrollo cotidiano. Opcional.
- **Optimización.** Si alguna medición incumple el umbral acordado, el análisis y
  la corrección se atienden como un cambio de rendimiento independiente.
- **Parametrización del limitador de tasa.** Exponer `AUTH_RATE_LIMIT` como
  variable de entorno en el backend, conservando 5 como valor por defecto.
  Requisito previo a la ejecución de la prueba sobre el endpoint de inicio de
  sesión.