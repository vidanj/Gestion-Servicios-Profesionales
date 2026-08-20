import { test, expect } from '@playwright/test';

const mockUsers = [
  { id: "u-001", firstName: "Juan", lastName: "Pérez", email: "juan@test.com", role: 1, status: true },
];

const mockLogs = [
  {
    id: "log-001",
    userId: "u-001",
    userName: "Juan Pérez",
    action: 6,
    detail: "Usuario juan@test.com creado.",
    status: 0,
    traceId: "06ded6db324384e443897587f45aec09",
    createdAt: "2026-03-11T10:00:00Z",
  },
  {
    id: "log-002",
    userId: "u-001",
    userName: "Juan Pérez",
    action: 3,
    detail: "Perfil actualizado.",
    status: 1,
    createdAt: "2026-03-11T11:00:00Z",
  },
];

test.beforeEach(async ({ page }) => {
  await page.route('**/api/Users**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: mockUsers, total: 1 }),
    });
  });

  await page.route('**/api/UserLogs**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: mockLogs, total: 2 }),
    });
  });

  await page.addInitScript(() => {
    localStorage.setItem('token', 'fake-token');
  });

  await page.goto('http://localhost:3000/usuarios/logs');
});

test('Logs página carga correctamente', async ({ page }) => {
  await expect(page.getByRole('heading', { name: 'Logs de usuarios' })).toBeVisible();
});

test('Logs muestra registros de la API', async ({ page }) => {
  const rows = page.getByTestId('log-row');
  await expect(rows).toHaveCount(2);
});

test('Logs muestra badge de estado correcto', async ({ page }) => {
  const row = page.getByTestId('log-row').first();
  await expect(row.getByText('exitoso')).toBeVisible();
  await expect(page.getByTestId('log-row').nth(1).getByText('alerta')).toBeVisible();
});

test('Logs muestra acción correcta', async ({ page }) => {
  await expect(page.getByRole('paragraph').filter({ hasText: 'Creación de usuario' })).toBeVisible();
  await expect(page.getByRole('paragraph').filter({ hasText: 'Actualización de perfil' })).toBeVisible();
});

test('Logs muestra detalle del log', async ({ page }) => {
  await expect(page.getByText('Usuario juan@test.com creado.')).toBeVisible();
});

test('Logs muestra nombre del usuario', async ({ page }) => {
  const rows = page.getByTestId('log-row');
  await expect(rows.first().getByText('Juan Pérez')).toBeVisible();
});

test('Logs muestra estado vacío cuando no hay registros', async ({ page }) => {
  await page.route('**/api/UserLogs**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: [], total: 0 }),
    });
  });
  await page.goto('http://localhost:3000/usuarios/logs');
  await expect(page.getByTestId('logs-empty')).toBeVisible();
});

test('Logs muestra error cuando falla la API', async ({ page }) => {
  await page.route('**/api/UserLogs**', async route => {
    await route.fulfill({ status: 500 });
  });
  await page.goto('http://localhost:3000/usuarios/logs');
  await expect(page.getByTestId('logs-error')).toBeVisible();
});

test('Filtro de estado envía parámetro correcto', async ({ page }) => {
  let urlCapturada = '';
  await page.route('**/api/UserLogs**', async route => {
    urlCapturada = route.request().url();
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: [], total: 0 }),
    });
  });

  await page.getByTestId('filter-status').selectOption('0');
  await expect(() => expect(urlCapturada).toContain('status=0')).toPass({ timeout: 3000 });
});

test('Filtro de acción envía parámetro correcto', async ({ page }) => {
  let urlCapturada = '';
  await page.route('**/api/UserLogs**', async route => {
    urlCapturada = route.request().url();
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: [], total: 0 }),
    });
  });

  await page.getByTestId('filter-action').selectOption('6');
  await expect(() => expect(urlCapturada).toContain('action=6')).toPass({ timeout: 3000 });
});

test('Botón limpiar resetea los filtros', async ({ page }) => {
  await page.getByTestId('filter-status').selectOption('0');
  await page.getByTestId('filter-clear').click();
  await expect(page.getByTestId('filter-status')).toHaveValue('');
  await expect(page.getByTestId('filter-action')).toHaveValue('');
});

test('Link CRUD navega a /usuarios', async ({ page }) => {
  await page.getByRole('link', { name: 'CRUD', exact: true }).click();
  await expect(page).toHaveURL(/\/usuarios$/);
});

test('Link Usuarios registrados navega a /usuarios/registrados', async ({ page }) => {
  await page.getByRole('link', { name: 'Usuarios registrados' }).click();
  await expect(page).toHaveURL(/\/usuarios\/registrados/);
});

test('Logs muestra la traza para enlazar con el log de operacion', async ({ page }) => {
  // El TraceId es el puente entre la bitacora de negocio y el log de operacion: sin
  // mostrarlo, el dato existe en la respuesta de la API pero no se puede usar desde
  // la pantalla, que era justo lo que faltaba.
  const traza = page.getByTestId('log-trace-id');
  await expect(traza).toHaveCount(1);
  await expect(traza).toContainText('06ded6db324384e443897587f45aec09');
});

test('Logs no muestra traza en registros que no la tienen', async ({ page }) => {
  // Las filas anteriores a la migracion no tienen traza, y una etiqueta vacia haria
  // creer que si existe correlacion.
  await expect(page.getByTestId('log-row')).toHaveCount(2);
  await expect(page.getByTestId('log-trace-id')).toHaveCount(1);
});
