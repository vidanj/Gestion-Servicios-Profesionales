# Feature Specification: Pruebas automáticas E2E asistidas por IA

**Feature Branch**: `001-e2e-playwright-mcp` (rama de git sugerida: `test/<issue>-e2e-playwright-mcp`)

**Created**: 2026-09-11

**Status**: Draft (clarificado)

**Input**: User description: "Pruebas automáticas: Katalon, Selenium o cualquier e2e test (Playwright + MCP-IA, Cypress, etc.)"

## Clarifications

### Session 2026-09-11

- Q: ¿Qué herramienta E2E se adopta, si la constitución (Principio V) prohíbe instalar una herramienta de automatización distinta de Playwright? → A: Playwright, la herramienta existente, más un agente de IA con acceso al navegador (Playwright MCP) para explorar, generar y reparar pruebas. Katalon, Selenium y Cypress se documentan como alternativas descartadas.
- Q: ¿Qué flujos debe cubrir primero la nueva suite? → A: Los flujos que hoy no tienen pruebas: P1 cliente (catálogo → detalle → solicitud), P2 profesional (gestión de sus servicios), P3 seguimiento del estado de las solicitudes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - El cliente contrata un servicio desde el catálogo (Priority: P1)

El equipo necesita la garantía automática de que un cliente puede recorrer el catálogo, abrir el
detalle de un servicio y enviar una solicitud, y de que los errores (servicio no disponible,
sesión vencida, fallo del servidor) se le muestran de forma comprensible.

**Why this priority**: Es el flujo que genera valor para el negocio (intermediación) y hoy no
tiene ninguna prueba automatizada: una regresión aquí llega a producción sin que nadie lo note.

**Independent Test**: Ejecutar solo las pruebas del flujo del cliente contra respuestas
simuladas; deben pasar sin backend ni base de datos.

**Acceptance Scenarios**:

1. **Given** un catálogo con al menos un servicio activo, **When** el cliente abre la pantalla de catálogo, **Then** ve cada servicio con su título, categoría y precio base.
2. **Given** el catálogo sin servicios, **When** el cliente lo abre, **Then** ve un mensaje de catálogo vacío y no una pantalla en blanco ni un error.
3. **Given** el detalle de un servicio activo, **When** el cliente envía una solicitud con descripción y fecha deseada, **Then** ve una confirmación y la solicitud aparece en "Solicitudes" con estado Pendiente.
4. **Given** un servicio inexistente o inactivo, **When** el cliente abre su detalle, **Then** ve un aviso de que no está disponible y no puede enviar la solicitud.
5. **Given** que el servidor responde con error al enviar la solicitud, **When** el cliente la envía, **Then** ve un mensaje de error y lo que escribió en el formulario se conserva.
6. **Given** una sesión vencida, **When** el cliente intenta enviar la solicitud, **Then** se le lleva a iniciar sesión.

---

### User Story 2 - El profesional gestiona sus servicios (Priority: P2)

El equipo necesita comprobar automáticamente que un profesional puede publicar, editar,
activar o desactivar y eliminar sus servicios, y que las validaciones del formulario le impiden
publicar datos inválidos.

**Why this priority**: Sin servicios publicados no hay catálogo; es el segundo flujo de mayor
impacto y tampoco tiene pruebas.

**Independent Test**: Ejecutar solo las pruebas del flujo del profesional contra respuestas simuladas.

**Acceptance Scenarios**:

1. **Given** un profesional autenticado, **When** abre "Mis servicios", **Then** ve solo sus servicios, con su estado activo o inactivo.
2. **Given** el formulario de alta con título, categoría, descripción y precio válidos, **When** lo envía, **Then** el servicio aparece en su lista.
3. **Given** el formulario con título vacío o precio negativo, **When** intenta enviarlo, **Then** ve el error de validación junto al campo y no se envía nada.
4. **Given** un servicio activo, **When** lo desactiva, **Then** su estado cambia a inactivo en la lista.
5. **Given** un servicio propio, **When** lo edita y guarda, **Then** ve los datos actualizados.
6. **Given** un servicio propio, **When** lo elimina y confirma, **Then** desaparece de su lista.

---

### User Story 3 - Seguimiento del estado de las solicitudes (Priority: P3)

El equipo necesita comprobar automáticamente que el profesional puede avanzar una solicitud por
su ciclo de vida (Pendiente → Aceptada → En progreso → Completada, o Cancelada) y que el cliente
ve el estado actualizado.

**Why this priority**: Es el seguimiento que el sistema promete; depende de que existan
solicitudes (P1), pero se prueba de forma independiente con datos simulados.

**Independent Test**: Ejecutar solo las pruebas de seguimiento con solicitudes simuladas en distintos estados.

**Acceptance Scenarios**:

1. **Given** una solicitud Pendiente asignada al profesional, **When** la acepta, **Then** su estado pasa a Aceptada.
2. **Given** una solicitud Aceptada, **When** el profesional la inicia y luego la completa, **Then** pasa a En progreso y después a Completada.
3. **Given** una solicitud en cualquier estado no final, **When** el profesional la cancela, **Then** pasa a Cancelada.
4. **Given** una solicitud Completada o Cancelada, **When** el profesional la ve, **Then** no se le ofrecen más cambios de estado.
5. **Given** que el servidor rechaza una transición, **When** el profesional la intenta, **Then** ve el motivo y el estado no cambia.
6. **Given** un cliente con solicitudes, **When** abre "Solicitudes", **Then** ve el estado vigente de cada una.

---

### User Story 4 - Generación y mantenimiento de pruebas asistidos por IA (Priority: P4)

Una persona del equipo describe un escenario en lenguaje natural y un agente de IA con acceso al
navegador explora la aplicación y propone un borrador de prueba; la persona lo revisa y lo
incorpora a la suite. El mismo agente ayuda a diagnosticar una prueba que falla.

**Why this priority**: Acelera la cobertura de los flujos siguientes, pero la suite de P1–P3 tiene
valor aunque esta historia no exista.

**Independent Test**: A partir de un escenario escrito, obtener con el agente un borrador que,
tras la revisión, pase tres veces consecutivas sin cambios.

**Acceptance Scenarios**:

1. **Given** un escenario escrito en *Given/When/Then*, **When** la persona pide al agente explorarlo, **Then** obtiene un borrador de prueba que usa los mismos criterios de selección de elementos que el resto de la suite.
2. **Given** una prueba que falla, **When** la persona pide al agente diagnosticarla, **Then** obtiene la causa probable (cambio de la interfaz, dato simulado o defecto real) sin que el agente modifique la prueba por su cuenta.

---

### Edge Cases

- Doble clic en "Enviar solicitud": no deben crearse dos solicitudes.
- Respuesta lenta del servidor: el usuario ve un indicador de carga y los controles no permiten reenviar.
- Usuario con rol cliente que abre directamente la pantalla de servicios del profesional.
- Textos con acentos, emojis o 1000 caracteres en la descripción (límite del modelo).
- Token presente pero rechazado por el servidor (401) en mitad de un flujo.
- Lista con muchas solicitudes: la pantalla sigue siendo usable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La suite MUST cubrir de forma automatizada todos los escenarios de aceptación de las historias 1 a 3.
- **FR-002**: Las pruebas MUST ejecutarse contra respuestas simuladas del servidor, nunca contra el backend ni la base de datos reales.
- **FR-003**: Cada prueba MUST ser determinista: el mismo resultado en tres ejecuciones consecutivas sin cambios de código.
- **FR-004**: Las pruebas MUST localizar los elementos por atributos de prueba estables o por su rol accesible, nunca por estilos ni posición.
- **FR-005**: La suite MUST ejecutarse automáticamente en cada cambio del frontend y bloquear la integración si falla.
- **FR-006**: Ante un fallo, la ejecución MUST conservar evidencia (traza y captura) consultable desde la ejecución automática.
- **FR-007**: Una prueba que falle porque el comportamiento real difiere del especificado MUST reportarse como defecto; no se modifica, salta ni elimina para que pase.
- **FR-008**: El equipo MUST disponer de un procedimiento documentado para generar un borrador de prueba con el agente de IA y para revisarlo antes de incorporarlo.
- **FR-009**: Las pruebas generadas con IA MUST cumplir las mismas reglas y pasar la misma revisión que las escritas a mano; el agente no incorpora pruebas a la suite sin revisión humana.
- **FR-010**: Los datos simulados de cada recurso (servicios, categorías, solicitudes, usuario autenticado) MUST definirse en un único lugar reutilizable y sin datos personales ni credenciales reales.
- **FR-011**: La suite MUST poder ejecutarse localmente con un solo comando, igual que la suite existente.
- **FR-012**: La ejecución automática MUST limitarse a un navegador (Chromium), como la decisión vigente del proyecto.

### Key Entities

- **Escenario de prueba**: caso *Given/When/Then* ligado a una historia y a un requisito.
- **Dato simulado (fixture)**: respuesta del servidor representativa de un recurso, reutilizable entre pruebas.
- **Evidencia de ejecución**: traza, captura y reporte asociados a una ejecución fallida.
- **Borrador generado por IA**: propuesta de prueba pendiente de revisión humana.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Los 18 escenarios de aceptación de las historias 1 a 3 tienen al menos una prueba automatizada.
- **SC-002**: Las pantallas con prueba de punta a punta pasan de 11 a al menos 15 de 17.
- **SC-003**: La suite completa (existente y nueva) termina en la ejecución automática en menos de 10 minutos.
- **SC-004**: Cero pruebas intermitentes: cinco ejecuciones consecutivas de la suite completa sin fallos no reproducibles.
- **SC-005**: Una persona del equipo obtiene un borrador de prueba revisable a partir de un escenario escrito en menos de 30 minutos.
- **SC-006**: El 100 % de los defectos detectados por las nuevas pruebas queda registrado como issue.

## Assumptions

- Las pantallas de referencia son las existentes: catálogo (`/dashboard`), detalle (`/catalogo/[id]`), servicios del profesional (`/profesionista`) y solicitudes (`/solicitudes`).
- Las respuestas simuladas reflejan los contratos actuales de la API, obtenidos por ingeniería inversa del código.
- El agente de IA se usa localmente por las personas del equipo; la ejecución automática en CI no depende del agente.
- Este piloto no cambia el comportamiento de la aplicación: si un escenario no se cumple, se reporta el defecto. Solo se agregan atributos de prueba cuando falten.
- Herramientas impuestas por la decisión del responsable (ver *Clarifications*): Playwright, ya instalado, y Playwright MCP como servidor del agente.
