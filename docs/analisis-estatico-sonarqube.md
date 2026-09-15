# Análisis estático de código con SonarQube

Sistema de Gestión de Servicios Profesionales — Unidad 2

Guía del stack local de SonarQube, de los parámetros de cada escáner y de cómo
se analiza el pull request de cada integrante.

---

## 1. Qué añade sobre lo que ya corre en CI

El repositorio ya tiene análisis estático: CSharpier y los analizadores StyleCop
y Roslynator en `backend-lint.yml`, ESLint en el frontend y CodeQL para
seguridad. SonarQube no los sustituye; responde otra pregunta.

| Herramienta | Qué contesta |
|---|---|
| CSharpier | ¿El formato coincide con el acordado? |
| StyleCop / Roslynator | ¿Esta línea viola una regla de estilo o una construcción desaconsejada? |
| CodeQL | ¿Hay un camino explotable de una entrada a una operación peligrosa? |
| **SonarQube** | **¿Cuánta deuda técnica acumula el proyecto, dónde está concentrada y cómo evoluciona entre versiones?** |

La diferencia práctica es que SonarQube mantiene el histórico y agrega métricas
por proyecto (deuda técnica en tiempo, duplicación, cobertura, densidad de
*code smells*), que es lo que pide la sección 4.3 del informe. Los analizadores
del compilador solo dicen sí o no sobre la línea que están mirando.

---

## 2. Instalación del stack local

El servidor y su base viven en contenedores; no se instala nada en la máquina
salvo el escáner de .NET.

```powershell
docker compose -f sonarqube/docker-compose.yml up -d
```

El primer arranque tarda un par de minutos: SonarQube levanta un Elasticsearch
embebido antes de responder. Cuando termine, `http://localhost:9000`
(credenciales iniciales `admin` / `admin`; el servidor obliga a cambiarlas).

| Parámetro del stack | Valor | Razón |
|---|---|---|
| Imagen | `sonarqube:community` | Es la edición gratuita. Sus límites se describen en la sección 5 |
| Base de datos | `postgres:18-alpine` | La misma versión mayor que usa el proyecto |
| Puerto | `9000` | Por defecto del servidor |
| Volúmenes | `sonarqube_data`, `_extensions`, `_logs`, `_db` | Sin ellos se pierden proyectos e histórico al detener el stack |
| `ulimits.nofile` | 65 536 | Elasticsearch no arranca con el valor por defecto de Docker Desktop |
| Montaje de la base | `sonarqube_db:/var/lib/postgresql` | **No** `/var/lib/postgresql/data`: ver la nota siguiente |

> **El punto de montaje de PostgreSQL 18.** Desde esa versión, la imagen oficial
> guarda los datos en un subdirectorio por versión mayor, de modo que el montaje
> correcto es `/var/lib/postgresql` y no el clásico `/var/lib/postgresql/data`.
> Montarlo en `.../data` no produce un aviso: la imagen lo detecta como un volumen
> sin usar y el contenedor termina con código 1 antes de arrancar. SonarQube
> entonces tampoco arranca, con el mensaje `dependency failed to start`, que no
> menciona el montaje por ningún lado.
>
> El workflow `docker-image.yml` del repositorio no tropieza con esto porque
> levanta PostgreSQL sin volumen: la base vive y muere con el job.

### Token de autenticación

Se genera en la interfaz: *My Account > Security > Generate Token*. Se exporta
como variable de entorno de la terminal, **no** se escribe en ningún archivo del
repositorio:

```powershell
$env:SONAR_TOKEN = "<token generado>"
```

No se agrega a `.env` ni a `.env.example`: ese archivo es la configuración que
la aplicación carga al arrancar, y el token no lo consume la aplicación sino el
escáner.

### Requisito de Java

El escáner de .NET corre sobre la máquina virtual de Java. Sin ella falla con un
mensaje que no menciona a Java, por lo que conviene verificarlo antes:

```powershell
winget install Microsoft.OpenJDK.17
java -version
```

---

## 3. Parámetros del escáner del backend (.NET)

Los proyectos de .NET no se analizan con el escáner genérico: hay que envolver
la compilación, porque el analizador necesita el grafo de símbolos que produce
el compilador de C#.

```powershell
dotnet tool run dotnet-sonarscanner begin `
  /k:"gsp-backend" `
  /n:"Gestion de Servicios Profesionales - Backend" `
  /d:sonar.host.url="http://localhost:9000" `
  /d:sonar.token="$env:SONAR_TOKEN" `
  /d:sonar.exclusions="frontend/**,tests/**,docs/**,.github/**,**/Migrations/**" `
  /d:sonar.cs.opencover.reportsPaths="coverage/**/coverage.opencover.xml"

dotnet build backend/SistemaServicios.sln

dotnet tool run dotnet-sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"
```

| Parámetro | Valor | Efecto |
|---|---|---|
| `/k` | `gsp-backend` | Clave del proyecto en el servidor |
| `/d:sonar.host.url` | `http://localhost:9000` | Servidor local |
| `/d:sonar.token` | Variable de entorno | Autenticación; nunca literal |
| `/d:sonar.exclusions` | `frontend/**`, `tests/**`, `docs/**`, `.github/**`, `**/Migrations/**` | Acota el proyecto al backend; ver la nota siguiente |
| `/d:sonar.cs.opencover.reportsPaths` | `coverage/**/coverage.opencover.xml` | Cobertura. Sin esto el proyecto aparece con 0 % |

> **Sin exclusiones, el proyecto del backend se traga el frontend.** El escáner de
> .NET analiza los proyectos de MSBuild, pero además recoge los archivos sueltos
> que encuentra bajo el directorio base, que aquí es la raíz del repositorio. El
> primer análisis de `gsp-backend` reportó `ts=8433` y `js=443` frente a
> `cs=5116`: más de la mitad del código medido era del frontend, que además
> vuelve a contarse en `gsp-frontend`.
>
> El efecto no es solo cosmético. La deuda técnica, la duplicación y el número de
> *code smells* del «backend» describían en realidad a las dos aplicaciones
> juntas, y cualquier conclusión sacada de esa cifra habría sido falsa.
>
> Se excluyen también las migraciones de EF: son código generado, y sus avisos no
> se corrigen a mano.

El `dotnet build` debe ser completo: con `--no-build` o `--no-incremental` mal
usados no hay compilación que interceptar y el análisis sale vacío.

### Cobertura en el formato que Sonar entiende

La suite del proyecto genera Cobertura, que es lo que consume ReportGenerator.
Para Sonar hace falta OpenCover:

```powershell
dotnet test backend/SistemaServicios.sln `
  --collect:"XPlat Code Coverage" `
  --settings backend/SistemaServicios.Tests/coverlet.runsettings `
  --results-directory coverage `
  -p:CoverletOutputFormat=opencover
```

---

## 4. Parámetros del escáner del frontend

El frontend sí usa el escáner genérico, por Docker, para no instalar el CLI:

```powershell
docker run --rm --network host `
  -v "${PWD}/frontend:/usr/src" `
  -e SONAR_TOKEN="$env:SONAR_TOKEN" `
  sonarsource/sonar-scanner-cli
```

Los parámetros viven en `frontend/sonar-project.properties`:

| Parámetro | Valor | Razón |
|---|---|---|
| `sonar.projectKey` | `gsp-frontend` | Proyecto separado del backend: son lenguajes y deudas distintas |
| `sonar.sources` | `app,src` | Rutas del App Router y del código reutilizable |
| `sonar.tests` | `tests` | Las specs de Playwright se declaran como pruebas, no como código de producción |
| `sonar.exclusions` | `node_modules`, `.next`, `public/mockServiceWorker.js` | Salida de compilación y archivo generado por MSW: analizarlos solo produce ruido |

---

## 5. Análisis del pull request de cada integrante

La actividad pide que cada integrante escanee su propio pull request de la
primera unidad.

**La edición Community no analiza ramas ni pull requests.** El parámetro
`sonar.branch.name` es de las ediciones de pago: en Community todo análisis se
atribuye a la rama principal del proyecto, de modo que escanear una rama sin más
sobrescribiría las métricas del proyecto con las de ese PR.

La forma correcta con esta edición es darle a cada PR su **propia clave de
proyecto**:

```powershell
git checkout <rama-del-pr>

dotnet tool run dotnet-sonarscanner begin `
  /k:"gsp-pr<numero>-<slug>" `
  /n:"PR #<numero> — <titulo corto>" `
  /d:sonar.host.url="http://localhost:9000" `
  /d:sonar.token="$env:SONAR_TOKEN"

dotnet build backend/SistemaServicios.sln

dotnet tool run dotnet-sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"
```

Así cada integrante obtiene su propio tablero, comparable con el del proyecto
completo, sin contaminar el histórico de `gsp-backend`.

### Asignación

| Integrante | Pull request de la unidad 1 | Clave de proyecto |
|---|---|---|
| Jesús Efrén Campuzano | [#186](https://github.com/vidanj/Gestion-Servicios-Profesionales/pull/186) — observabilidad con Serilog y OpenTelemetry | `gsp-pr186-observabilidad` |
| *(por definir)* | | |
| *(por definir)* | | |
| *(por definir)* | | |

> Conviene escoger un PR con código de aplicación. Un PR de documentación pasa el
> análisis sin hallazgos, no porque el código esté sano, sino porque no hay
> código que analizar.

---

## 6. Puerta de calidad

Se usa la puerta por defecto, *Sonar way*, sin modificar. Aplica sobre el
**código nuevo**: exige 0 problemas con calificación peor que A, cobertura
mínima del 80 % y duplicación por debajo del 3 % en las líneas que cambian.

Esa elección es deliberada: una puerta sobre el total del código convertiría
toda la deuda histórica en un bloqueo el primer día, y la respuesta natural del
equipo sería bajar el umbral hasta que pasara. Evaluando solo el código nuevo,
la deuda existente se mide y se reporta, pero lo que se exige es no añadir más.

---

## 7. Resultados

### 7.1 Cómo leer el número de *code smells*

El backend reporta 872 *code smells*, y esa cifra sola induce a error: **743 de
ellos son una sola regla**, `IDE0008` («usar el tipo explícito en lugar de
`var`»). Es una preferencia de estilo del analizador externo de Roslyn, no un
defecto, y además contradice el estilo que el equipo ya usa en todo el código.

| Regla | Ocurrencias | Qué es |
|---|---|---|
| `external_roslyn:IDE0008` | 743 | Usar tipo explícito en lugar de `var` (preferencia de estilo) |
| `external_roslyn:IDE0290` | 40 | Sugiere constructor primario |
| `external_roslyn:IDE0046` | 16 | Sugiere expresión condicional en el retorno |
| `csharpsquid:S6964` | 9 | Riesgo de *overposting* en modelos que entran por la API |
| `csharpsquid:S1135` | 4 | Comentarios `TODO` pendientes |

Descontado `IDE0008`, quedan unos 129 hallazgos, que es la cifra sobre la que
tiene sentido trabajar. De ellos, `S6964` es el único de fondo: advierte sobre
propiedades que podrían llegar desde el cuerpo de una petición sin que se desee.

Lo aconsejable es decidir en equipo si `IDE0008` refleja la convención del
proyecto; si no la refleja, se desactiva en el perfil de calidad de SonarQube.
Desactivarla ahí **no** es silenciar un aviso en el código: no se toca el
`.editorconfig` ni se agrega ningún `#pragma`, que es lo que prohíbe el
Principio VI de la constitución.

### 7.2 Métricas por proyecto

| Proyecto | Bugs | Vulnerabilidades | Code smells | Deuda | Duplicación | Cobertura | Líneas | Puerta |
|---|---|---|---|---|---|---|---|---|
| `gsp-backend` | 1 | 1 | 872 | 181 min | 0.8 % | 68.8 % | 4 390 | OK |
| `gsp-frontend` | 1 | 0 | 91 | 442 min | 6.3 % | 0.0 % | 8 485 | OK |

> La cobertura del frontend es 0 % porque las pruebas de Playwright corren contra
> respuestas simuladas y no instrumentan el código de la aplicación. No significa
> que no haya pruebas: significa que esa métrica no aplica a esa suite.

### 7.3 Los tres hallazgos que no son de estilo

| Proyecto | Tipo | Regla | Dónde | Qué dice |
|---|---|---|---|---|
| `gsp-backend` | Bug (mayor) | `csharpsquid:S8949` | `Extensions/HealthCheckResponseWriter.cs:36` | No se pasa `context.RequestAborted`: si el cliente corta la petición, la escritura de la respuesta sigue |
| `gsp-backend` | Vulnerabilidad (menor) | `docker:S6471` | `Dockerfile:27` | La imagen corre como `root`; conviene declarar un usuario sin privilegios |
| `gsp-frontend` | Bug (menor) | `typescript:S1082` | `src/components/profile/avatar-uploader.tsx:47` | Elemento con `onClick` sin equivalente de teclado: el selector de avatar no es accesible sin ratón |

Los tres son reales y ninguno lo detectaban las herramientas que ya corrían en
CI. El de accesibilidad es el más interesante: ESLint no lo marca con la
configuración actual, y describe un problema de uso concreto, no un tecnicismo.

Ninguno se corrige en esta entrega: el alcance es instalar y evidenciar el
análisis. Cada uno da para su propio issue.

### 7.4 Resultados por integrante

| Integrante | Pull request | Clave de proyecto | Bugs | Vulns. | Smells | Deuda | Duplicación | Puerta |
|---|---|---|---|---|---|---|---|---|
| Jesús Efrén Campuzano | [#186](https://github.com/vidanj/Gestion-Servicios-Profesionales/pull/186) — observabilidad | `gsp-pr186-observabilidad` | 1 | 1 | 860 | 181 min | 0.8 % | OK |
| *(por definir)* | | | | | | | | |
| *(por definir)* | | | | | | | | |
| *(por definir)* | | | | | | | | |

El escaneo se hizo sobre un *worktree* de la rama en una ruta corta
(`C:\gsp-pr186`). En Windows, crear el *worktree* dentro de una carpeta ya
profunda falla con `Filename too long`: los `.Designer.cs` de las migraciones
superan el límite de 260 caracteres.

**Lo que dice la comparación.** Las cifras del PR #186 y las de `gsp-backend`
son casi idénticas: mismo bug (`S8949`), misma vulnerabilidad (`docker:S6471`),
misma deuda de 181 minutos y la misma duplicación.

Eso no significa que el PR no aportara nada; significa lo contrario de lo que
parece a primera vista: **los dos hallazgos no los introdujo ese PR y siguen
vivos en `dev` hoy**. Un análisis por PR sirve para separar lo que llegó con un
cambio de lo que ya estaba, y en este caso demuestra que ambos son deuda
heredada, no una regresión de esa rama.

La cobertura aparece en 0 % porque en esa rama no se ejecutaron las pruebas
antes de analizar; el escaneo midió calidad de código, no cobertura. Para
compararla habría que repetir el `dotnet test` con OpenCover dentro del
*worktree*.
