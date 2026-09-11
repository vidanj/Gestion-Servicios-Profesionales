# Feature Specification: Software como infraestructura (entornos en la nube, infraestructura declarativa y CI/CD)

**Feature Branch**: `003-infraestructura-como-codigo` (rama de git sugerida: `ci/<issue>-infraestructura-como-codigo`)

**Created**: 2026-09-11

**Status**: Draft (clarificado)

**Input**: User description: "Software como infraestructura: codespaces (deploy) ...... terraform (multivendor-cloud), --> CICD"

## Clarifications

### Session 2026-09-11

- Q: ¿Qué segundo proveedor demuestra la operación multi-nube, además de Render? → A: GitHub Codespaces.
- Q: ¿Qué administra la infraestructura declarativa en la primera iteración? → A: Un ambiente de staging nuevo; producción se incorpora después, sin riesgo para lo que hoy corre.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entorno de trabajo completo en la nube (Priority: P1)

Una persona del equipo abre el repositorio en un entorno de la nube y, sin instalar nada en su
equipo, obtiene la aplicación completa en marcha (API, base de datos con migraciones aplicadas y
frontend) con las herramientas del proyecto listas para formatear, analizar y probar.

**Why this priority**: Hoy preparar el entorno exige instalar .NET, Node y PostgreSQL y crear el
`.env` a mano; es la barrera de entrada más alta y el origen de diferencias entre equipos.

**Independent Test**: Crear un entorno nuevo desde la rama `dev`; comprobar que la sonda de
disponibilidad responde sana y que las suites de backend y E2E pasan.

**Acceptance Scenarios**:

1. **Given** una persona con acceso al repositorio, **When** crea un entorno en la nube desde `dev`, **Then** en menos de 15 minutos la API, la base de datos y el frontend están en marcha y accesibles desde su navegador.
2. **Given** el entorno en marcha, **When** ejecuta las pruebas del backend y las E2E, **Then** se ejecutan sin instalar nada adicional.
3. **Given** que la configuración sensible (clave JWT, SMTP) no está en el repositorio, **When** se crea el entorno, **Then** la toma de los secretos del proveedor; si falta un secreto obligatorio, el entorno indica cuál falta.

---

### User Story 2 - Staging declarativo y reproducible (Priority: P2)

El ambiente de staging (API, base de datos, frontend y su configuración) está descrito como código
en el repositorio; cualquier cambio se revisa en un PR mostrando qué va a cambiar antes de
aplicarse, y el ambiente puede destruirse y recrearse con el mismo resultado.

**Why this priority**: Es la base del despliegue continuo (P3) y elimina la configuración manual
en paneles, que hoy no es trazable.

**Independent Test**: Crear staging desde cero con la definición versionada; destruirlo y
recrearlo; el resultado es idéntico.

**Acceptance Scenarios**:

1. **Given** un PR que modifica la infraestructura, **When** se abre, **Then** el PR muestra el plan de cambios (qué se crea, modifica o destruye) antes de aplicar nada.
2. **Given** un plan revisado, **When** se aprueba explícitamente, **Then** se aplica y staging queda en el estado descrito.
3. **Given** que alguien cambia a mano la configuración en el panel del proveedor, **When** se genera el siguiente plan, **Then** aparece la diferencia.
4. **Given** una orden de destruir staging, **When** se ejecuta, **Then** la base de datos no se elimina sin una confirmación explícita adicional.

---

### User Story 3 - Despliegue continuo verificado y reversible (Priority: P3)

Al integrar un cambio en `dev` con todos los checks en verde, staging se actualiza
automáticamente con esa versión; el despliegue solo se da por bueno si la aplicación responde
sana, y existe una forma probada de volver a la versión anterior. La promoción a producción
requiere aprobación manual.

**Why this priority**: Retoma los issues #131 y #189: hoy el despliegue se lanza a mano y un
fallo de migración tumba el servicio sin dejar rastro legible.

**Independent Test**: Integrar un cambio trivial en `dev` y observar el despliegue verificado en
staging; forzar una versión rota y ejecutar la reversión.

**Acceptance Scenarios**:

1. **Given** un merge a `dev` con todos los checks en verde, **When** termina el CI, **Then** staging se despliega con la imagen de ese commit.
2. **Given** un despliegue, **When** la aplicación no responde sana dentro del tiempo límite (que contempla el arranque en frío), **Then** el despliegue se marca como fallido de forma visible.
3. **Given** una migración que falla, **When** arranca la nueva versión, **Then** la instancia sigue viva, informa que no está lista y el despliegue falla visiblemente, en lugar de caer por completo.
4. **Given** una versión defectuosa en staging, **When** se ejecuta la reversión, **Then** staging vuelve a la versión anterior.
5. **Given** dos despliegues al mismo ambiente, **When** coinciden en el tiempo, **Then** se ejecutan uno después del otro.
6. **Given** una versión sana en staging, **When** se solicita promoverla a producción, **Then** se requiere una aprobación manual.

---

### User Story 4 - Operación multi-proveedor (Priority: P4)

Las definiciones de infraestructura están organizadas por proveedor detrás de una interfaz común,
de modo que se administran de forma declarativa tanto Render (ejecución de staging) como GitHub
(ambientes, secretos de CI y secretos de los entornos en la nube), y se puede añadir otro
proveedor sin cambiar la aplicación.

**Why this priority**: Demuestra la portabilidad pedida ("multivendor") sobre la base de P1–P3.

**Independent Test**: Aplicar la definición de GitHub y comprobar que los ambientes y secretos
existen con los nombres esperados; revisar que añadir un proveedor solo exige un módulo nuevo.

**Acceptance Scenarios**:

1. **Given** la definición de GitHub, **When** se aplica, **Then** existen los ambientes `staging` y `production` (este con revisión obligatoria) y los nombres de secretos requeridos, sin que sus valores estén en el repositorio.
2. **Given** la interfaz común de módulos, **When** se documenta un proveedor adicional, **Then** los parámetros de entrada y salida son los mismos que los del proveedor actual.

---

### Edge Cases

- El plan gratuito del proveedor duerme la instancia: el primer acceso tarda decenas de segundos y no debe confundirse con un despliegue fallido.
- Se agota la cuota gratuita del entorno en la nube: el equipo sabe cómo detener entornos inactivos.
- Rotación de un secreto: se actualiza en el proveedor sin tocar el repositorio.
- Una persona sin permisos intenta aplicar cambios: la aplicación se rechaza.
- Una migración destructiva: la reversión de la imagen no devuelve los datos; queda documentado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El repositorio MUST definir un entorno de trabajo en la nube que levante API, base de datos y frontend con un solo paso.
- **FR-002**: El entorno de trabajo MUST obtener la configuración sensible de los secretos del proveedor, nunca del repositorio, e indicar qué secreto obligatorio falta.
- **FR-003**: El entorno de trabajo MUST dejar listas las herramientas del proyecto: formateo, análisis estático, pruebas unitarias, de integración y E2E.
- **FR-004**: El ambiente de staging MUST describirse de forma declarativa y versionada: servicio de API, base de datos PostgreSQL, frontend y grupos de variables.
- **FR-005**: Todo cambio de infraestructura MUST mostrar un plan revisable en el PR antes de aplicarse, y aplicarse solo con aprobación explícita.
- **FR-006**: El estado de la infraestructura MUST guardarse fuera del repositorio, con bloqueo contra aplicaciones simultáneas.
- **FR-007**: La base de datos de staging MUST estar protegida contra el borrado accidental.
- **FR-008**: Los ambientes y los secretos de GitHub (CI y entornos en la nube) MUST administrarse de forma declarativa, con los valores provistos fuera del repositorio.
- **FR-009**: Al integrar en `dev` con todos los checks en verde, staging MUST desplegarse automáticamente con la versión de ese commit.
- **FR-010**: Cada despliegue MUST verificarse con la sonda de disponibilidad existente; si no queda sano dentro del tiempo límite, MUST marcarse como fallido de forma visible.
- **FR-011**: Los despliegues a un mismo ambiente MUST serializarse.
- **FR-012**: MUST existir un procedimiento de reversión a la versión anterior, documentado y ejecutado al menos una vez en staging.
- **FR-013**: La aplicación de migraciones MUST desacoplarse del arranque, de modo que un fallo de migración deje la instancia viva reportando que no está lista (issue #189).
- **FR-014**: La promoción a producción MUST requerir aprobación manual en un ambiente protegido.
- **FR-015**: Las definiciones MUST organizarse por proveedor detrás de una interfaz común de entradas y salidas.
- **FR-016**: La versión desplegada MUST ser trazable al commit que la generó.
- **FR-017**: El README MUST documentar el flujo completo, desde el commit hasta la versión en marcha, y cómo crear y destruir staging.

### Key Entities

- **Ambiente**: entorno de trabajo en la nube, staging o producción; con su dueño y su política de aprobación.
- **Recurso de infraestructura**: servicio, base de datos o grupo de variables descrito como código.
- **Secreto**: nombre, alcance (CI, ambiente, entorno en la nube) y responsable; el valor nunca vive en el repositorio.
- **Despliegue**: commit, ambiente, resultado y verificación de salud.
- **Estado de infraestructura**: registro remoto, con bloqueo, de lo que existe.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Una persona nueva pasa de "abrir el repositorio" a "aplicación en marcha con pruebas en verde" en 15 minutos o menos, sin instalar nada en su equipo.
- **SC-002**: Staging se crea desde cero en 30 minutos o menos con una única aplicación aprobada, y recrearlo produce el mismo resultado.
- **SC-003**: El 100 % de los merges a `dev` con CI en verde termina en staging verificado o en un fallo visible en 20 minutos o menos.
- **SC-004**: La reversión de staging a la versión anterior toma 10 minutos o menos.
- **SC-005**: Cero secretos en el repositorio, verificado por un escaneo automático en cada PR.
- **SC-006**: El 100 % de los cambios manuales en el panel del proveedor aparece como diferencia en el siguiente plan.

## Assumptions

- **Interpretación de "GitHub Codespaces como segundo proveedor"**: Codespaces aloja el stack completo en contenedores efímeros para desarrollo, revisión de PR y demostraciones. No sustituye un hosting de producción, porque los entornos se detienen por inactividad. La operación multi-proveedor se demuestra administrando de forma declarativa Render (staging) y GitHub (ambientes y secretos de CI y de Codespaces).
- Render sigue siendo el proveedor de producción y no se toca en esta iteración; importar producción a la definición declarativa queda fuera de alcance.
- El destino actual del frontend no está documentado en el repositorio; staging lo incluye como servicio propio.
- La cuota gratuita de Codespaces de la cuenta dueña del repositorio es suficiente para el piloto; los entornos inactivos se detienen.
- Existe una persona con acceso de administración a Render y a GitHub que provee los secretos y aprueba las aplicaciones.
- La primera mitad del issue #131 (construir y arrancar la imagen en CI, `docker-image.yml`) ya está hecha; este spec absorbe el issue #189.
- Herramientas impuestas por la decisión del responsable: GitHub Codespaces, Terraform y GitHub Actions.
