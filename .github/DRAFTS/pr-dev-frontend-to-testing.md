# 🚀 Solicitud de Pull Request

Gracias por tu contribución. Por favor, completa la siguiente información para ayudar al equipo a revisar y aprobar este PR de manera eficiente.

---

## 📌 Resumen del Cambio

Integración de `dev-frontend` → `testing` para validación de integración del módulo frontend completo.

Incluye cinco bloques de trabajo fusionados en esta rama:

1. **Funcionalidad de Login** (`feat/65-funcionalidad-a-login`): página de login conectada al backend JWT, redirección automática desde la raíz (`/`) hacia `/login`, y protección de rutas para evitar acceso a páginas sin autenticar.

2. **Catálogo de Servicios** (`feat/20-frontend-catálogo-de-servicios`): vistas y componentes para la navegación y visualización del catálogo de servicios disponibles.

3. **Recuperación de Contraseña** (`feat/18-feat-frontend-recuperación-de-contraseña-ui`): implementación de la UI para el flujo de recuperación de contraseña.

4. **Interfaz de Solicitudes** (`issue-22-frontend-solicitudes`): tabla y formulario para gestión de solicitudes con datos mock, usando arquitectura feature-based (domain / application / infrastructure / presentation).

5. **Navegación actualizada** (`chore/62-actualización-a-nav`): componente `nav.tsx` actualizado con los módulos correctos; se incluyen `user-actions` como componente reutilizable.

Adicionalmente se incluye una prueba E2E con Playwright sobre el flujo de recuperación de contraseña y correcciones de conflictos de configuración.

---

## 🔍 Tipo de Cambio realizado

Marca con una `x` lo que aplica:

- [x] 🐞 Corrección de error (`fix`)
- [x] ✨ Nueva funcionalidad (`feat`)
- [ ] ♻️ Refactorización del código sin cambios funcionales (`refactor`)
- [x] 🧪 Agregado o mejora de pruebas (`test`)
- [ ] 🧱 Cambio en configuración CI/CD (`ci`)
- [ ] 🚀 Mejora de rendimiento (`perf`)
- [ ] 📚 Cambios en la documentación (`docs`)
- [ ] Otro (especificar):

---

## 📂 Archivos Afectados

**Páginas y layouts — nuevos/modificados:**
- `frontend/app/page.tsx` *(redirección raíz → `/login`)*
- `frontend/app/layout.tsx`
- `frontend/app/login/page.tsx`
- `frontend/app/recovery/page.tsx`
- `frontend/app/dashboard/page.tsx`
- `frontend/app/dashboard/layout.tsx`
- `frontend/app/about/page.tsx`
- `frontend/app/about/layout.tsx`
- `frontend/app/solicitudes/page.tsx`
- `frontend/app/solicitudes/layout.tsx`
- `frontend/app/usuarios/layout.tsx`
- `frontend/app/usuarios/registrados/registrados_page.tsx`

**Componentes — nuevos/modificados:**
- `frontend/src/components/nav/nav.tsx`
- `frontend/src/components/user-actions/user-actions.tsx`
- `frontend/src/components/user-actions/index.ts`

**Feature: Solicitudes (arquitectura feature-based):**
- `frontend/src/features/solicitudes/domain/request.model.ts`
- `frontend/src/features/solicitudes/application/use-requests-controller.ts`
- `frontend/src/features/solicitudes/infrastructure/mock-requests.repository.ts`
- `frontend/src/features/solicitudes/presentation/requests-table.tsx`
- `frontend/src/features/solicitudes/presentation/requests-form.tsx`

**Datos y configuración:**
- `frontend/src/data/config.ts`
- `frontend/src/data/users-store.ts`
- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/.gitignore`
- `frontend/README.md`

**Pruebas E2E:**
- `frontend/tests/recovery.spec.ts`
- `frontend/test-results/.last-run.json`

---

## 🧪 ¿Cómo Probarlo?

### 1. Instalar dependencias y levantar el frontend

```bash
cd frontend
npm install
npm run dev
# Acceder a http://localhost:3000
```

### 2. Verificar redirección raíz

```
GET http://localhost:3000/
→ Debe redirigir automáticamente a /login
```

### 3. Verificar flujo de login

```
1. Ir a http://localhost:3000/login
2. Ingresar credenciales válidas (requiere backend corriendo en :5000)
3. Verificar redirección al dashboard
```

### 4. Verificar módulos de navegación

```
1. Desde el dashboard, navegar a Solicitudes y Catálogo
2. Verificar que el nav muestra los módulos correctos
3. Verificar que user-actions aparece con opciones de usuario
```

### 5. Verificar recuperación de contraseña

```
1. Ir a http://localhost:3000/recovery
2. Ingresar un email y verificar que el formulario responde
```

### 6. Pruebas E2E (Playwright)

```bash
cd frontend
npx playwright test tests/recovery.spec.ts
```

---

## ✅ Checklist

Asegúrate de completar lo siguiente antes de enviar:

- [x] He probado mis cambios localmente
- [x] Esta PR sigue el formato de convención de commits (si aplica)
- [x] Se han actualizado o agregado pruebas
- [ ] La documentación se actualizó si fue necesario
- [ ] No hay errores en CI/CD

---

## 📎 Notas Adicionales

- Este PR integra cinco features completadas y mergeadas en `dev-frontend`: login (#65), catálogo (#20), recuperación de contraseña (#18), solicitudes (#22) y nav (#62).
- El login funcional requiere que el backend (`dev-backend`) esté corriendo y apuntando a la misma BD — se recomienda mergearlo junto con el PR de `dev-backend → testing`.
- La rama local `dev-frontend` tiene **un commit divergente** respecto a `origin/dev-frontend` con un conflicto en `frontend/app/page.tsx` que debe resolverse antes de abrir el PR en GitHub.
- Los datos de solicitudes son **mock** por el momento; la conexión al backend real queda pendiente para siguientes iteraciones.

---

Gracias 🙌
