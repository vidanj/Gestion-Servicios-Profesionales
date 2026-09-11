# Feature Specification: Guía interactiva de producto por rol

**Feature Branch**: `002-guia-interactiva-tours` (rama de git sugerida: `feat/<issue>-guia-interactiva-tours`)

**Created**: 2026-09-11

**Status**: Draft (clarificado)

**Input**: User description: "Guía interactiva: https://driverjs.com (tours) .. guía interactiva"

## Clarifications

### Session 2026-09-11

- Q: ¿Cómo se trata la librería de tours, si la constitución prohíbe agregar otra librería de UI? → A: Con una enmienda acotada (constitución v1.1.0) que permite `driver.js` exclusivamente para tours.
- Q: ¿Dónde se recuerda que un usuario ya vio o cerró su recorrido? → A: En el navegador, por usuario y por versión del recorrido; sin cambios de backend.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Recorrido de bienvenida del cliente (Priority: P1)

La primera vez que un cliente entra a la aplicación después de iniciar sesión, un recorrido
guiado le señala, paso a paso, dónde está el catálogo, cómo abrir un servicio, cómo solicitarlo,
dónde seguir sus solicitudes y dónde editar su perfil. Puede avanzar, retroceder u omitir el
recorrido en cualquier momento.

**Why this priority**: Los clientes son el grupo más numeroso y el que menos contexto tiene;
que lleguen a su primera solicitud sin ayuda es el objetivo del producto.

**Independent Test**: Iniciar sesión como cliente nuevo en un navegador limpio; el recorrido
aparece solo, se completa y no vuelve a aparecer al recargar.

**Acceptance Scenarios**:

1. **Given** un cliente que nunca vio el recorrido en este navegador, **When** entra después de iniciar sesión, **Then** el recorrido del cliente inicia automáticamente en el primer paso.
2. **Given** el recorrido en curso, **When** el cliente avanza o retrocede, **Then** cada paso resalta un elemento, muestra título y descripción y el progreso ("2 de 5").
3. **Given** el recorrido en curso, **When** el cliente lo cierra, **Then** el recorrido termina, queda registrado como omitido y la pantalla queda como estaba.
4. **Given** un cliente que completó u omitió el recorrido, **When** vuelve a entrar o recarga, **Then** el recorrido no inicia automáticamente.

---

### User Story 2 - Recorrido del profesional (Priority: P2)

El profesional recibe un recorrido que le muestra dónde publicar un servicio, cómo activarlo o
desactivarlo, dónde ver las solicitudes que recibe y cómo cambiar su estado.

**Why this priority**: Sin profesionales que publiquen no hay catálogo; su recorrido reduce el
tiempo hasta publicar el primer servicio.

**Independent Test**: Iniciar sesión como profesional nuevo; se muestra su recorrido, distinto al del cliente.

**Acceptance Scenarios**:

1. **Given** un profesional que nunca vio su recorrido, **When** entra, **Then** inicia el recorrido del profesional y no el del cliente.
2. **Given** un paso cuyo elemento no está en la pantalla, **When** el recorrido llega a él, **Then** el paso se omite y el recorrido continúa.

---

### User Story 3 - Recorrido del administrador (Priority: P3)

El administrador recibe un recorrido por la administración de usuarios, la bitácora, la gráfica
de registros y los respaldos.

**Why this priority**: Son pocos usuarios, pero con acciones de alto impacto (suspender
usuarios, respaldar la base).

**Independent Test**: Iniciar sesión como administrador nuevo; se muestra el recorrido de administración.

**Acceptance Scenarios**:

1. **Given** un administrador que nunca vio su recorrido, **When** entra, **Then** inicia el recorrido de administración.

---

### User Story 4 - Volver a ver la guía (Priority: P4)

Cualquier usuario puede volver a lanzar el recorrido de su rol desde un control visible en la
navegación, aunque ya lo haya completado u omitido.

**Why this priority**: Complementa las historias anteriores; sin ella, quien omitió el recorrido
por error no puede recuperarlo.

**Independent Test**: Con el recorrido ya completado, pulsar "Ver guía"; el recorrido inicia desde el primer paso.

**Acceptance Scenarios**:

1. **Given** un usuario con el recorrido completado, **When** pulsa "Ver guía", **Then** el recorrido de su rol inicia desde el primer paso.
2. **Given** una nueva versión del recorrido de su rol, **When** el usuario entra, **Then** el recorrido nuevo se muestra una vez, aunque hubiera completado la versión anterior.

---

### Edge Cases

- El usuario navega a otra pantalla en mitad del recorrido: el recorrido se cierra y cuenta como omitido.
- Pantallas angostas (< 768 px): los pasos se muestran como aviso centrado, sin resaltado, para no tapar el contenido.
- El almacenamiento del navegador no está disponible (modo privado estricto): el recorrido se ofrece, pero no se recuerda y no debe romper la aplicación.
- Dos usuarios distintos en el mismo navegador: cada uno tiene su propio estado.
- El usuario cambia de rol (lo cambia un administrador): recibe el recorrido del rol nuevo.
- Modo oscuro: los textos del recorrido mantienen contraste legible.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST ofrecer un recorrido guiado específico para cada rol: cliente, profesional y administrador.
- **FR-002**: El recorrido MUST iniciarse automáticamente la primera vez que un usuario con ese rol entra después de iniciar sesión, y MUST NOT volver a iniciarse automáticamente una vez completado u omitido.
- **FR-003**: El usuario MUST poder cerrar el recorrido en cualquier paso; cerrar cuenta como omitido.
- **FR-004**: Cada paso MUST resaltar un elemento de la interfaz y mostrar un título y una descripción en español de hasta 200 caracteres.
- **FR-005**: El recorrido MUST mostrar el progreso (paso actual y total) y permitir avanzar y retroceder.
- **FR-006**: Si el elemento de un paso no existe en la pantalla, el paso MUST omitirse sin interrumpir el recorrido.
- **FR-007**: El usuario MUST poder volver a iniciar el recorrido de su rol desde un control visible en la navegación.
- **FR-008**: El estado del recorrido (completado u omitido) MUST recordarse por usuario y por versión en el navegador; una versión nueva se muestra una vez más.
- **FR-009**: El recorrido MUST poder usarse solo con el teclado (avanzar, retroceder, cerrar con Esc) y ser anunciado por lectores de pantalla.
- **FR-010**: La apariencia del recorrido MUST ser coherente con el tema visual de la aplicación, incluido el modo oscuro.
- **FR-011**: El recorrido MUST NOT ejecutar acciones de la aplicación (enviar, borrar, cambiar estados); al terminar, la pantalla queda en el mismo estado en que estaba.
- **FR-012**: Los textos y el orden de los pasos MUST definirse en un único lugar, editable sin tocar la lógica del recorrido.
- **FR-013**: La funcionalidad MUST cubrirse con pruebas de punta a punta: inicio automático, omitir, no repetir, relanzar y paso omitido por elemento ausente.
- **FR-014**: La librería de recorridos MUST usarse solo para esta funcionalidad (excepción de la constitución v1.1.0).

### Key Entities

- **Recorrido**: identificador, rol destinatario, versión y lista ordenada de pasos.
- **Paso**: elemento de destino, título, descripción y orden.
- **Estado del recorrido**: usuario, recorrido, versión, resultado (completado u omitido) y fecha; vive en el navegador.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En una prueba de usabilidad con 5 personas por rol, al menos 4 completan su primera tarea clave sin ayuda externa después del recorrido (cliente: enviar una solicitud; profesional: publicar un servicio; administrador: localizar la bitácora).
- **SC-002**: Cada recorrido tiene como máximo 7 pasos y se completa en menos de 90 segundos.
- **SC-003**: Cero inicios automáticos repetidos por usuario y versión en el mismo navegador, verificado por pruebas automatizadas.
- **SC-004**: El 100 % de los pasos se puede recorrer solo con el teclado.

## Assumptions

- El rol del usuario se conoce a partir de su sesión actual.
- La navegación actual muestra los mismos enlaces a todos los roles; cada recorrido apunta solo a lo que su rol puede usar. Filtrar la navegación por rol queda fuera de este alcance.
- Si el usuario cambia de navegador o de equipo, verá el recorrido una vez más (consecuencia aceptada de guardar el estado en el navegador).
- No se registran métricas de uso en el servidor; SC-001 se mide con una prueba de usabilidad manual.
- Librería impuesta por la decisión del responsable (ver *Clarifications*): `driver.js`.
