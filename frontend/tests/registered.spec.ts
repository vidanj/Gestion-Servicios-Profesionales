import { test, expect } from '@playwright/test';

const mockUsers = [
  {
    id: "u-001",
    firstName: "Juan",
    lastName: "Pérez",
    email: "juan@test.com",
    role: 1,
    status: true,
  },
  {
    id: "u-002",
    firstName: "Ana",
    lastName: "López",
    email: "ana@test.com",
    role: 2,
    status: true,
  },
];

test.beforeEach(async ({ page }) => {
  await page.route('**/api/Users**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: mockUsers, total: 2 }),
    });
  });

  await page.addInitScript(() => {
    localStorage.setItem('token', 'fake-token');
  });

  await page.goto('http://localhost:3000/usuarios/registrados');
});

test('Registrados página carga correctamente', async ({ page }) => {
  await expect(page.getByRole('heading', { name: 'Usuarios registrados' })).toBeVisible();
});

test('Registrados muestra cards de usuarios de la API', async ({ page }) => {
  const cards = page.getByTestId('registrado-card');
  await expect(cards).toHaveCount(2);
});

test('Registrados muestra nombre y email del usuario', async ({ page }) => {
  await expect(page.getByText('Juan Pérez')).toBeVisible();
  await expect(page.getByText('juan@test.com')).toBeVisible();
});

test('Registrados muestra badge de rol correcto', async ({ page }) => {
  await expect(page.getByText('cliente').first()).toBeVisible();
  await expect(page.getByText('profesionista').first()).toBeVisible();
});

test('Registrados muestra estado vacío cuando no hay usuarios', async ({ page }) => {
  await page.route('**/api/Users**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: [], total: 0 }),
    });
  });

  await page.goto('http://localhost:3000/usuarios/registrados');
  await expect(page.getByTestId('registrados-empty')).toBeVisible();
});

test('Registrados muestra error cuando falla la API', async ({ page }) => {
  await page.route('**/api/Users**', async route => {
    await route.fulfill({ status: 500 });
  });

  await page.goto('http://localhost:3000/usuarios/registrados');
  await expect(page.getByTestId('registrados-error')).toBeVisible();
});

test('Botón eliminar abre modal de confirmación', async ({ page }) => {
  await page.getByLabel('Eliminar usuario').first().click();
  await expect(page.getByText('Confirmar eliminacion')).toBeVisible();
});

test('Cancelar modal de eliminación lo cierra', async ({ page }) => {
  await page.getByLabel('Eliminar usuario').first().click();
  await expect(page.getByText('Confirmar eliminacion')).toBeVisible();
  await page.getByRole('button', { name: 'Cancelar' }).click();
  await expect(page.getByText('Confirmar eliminacion')).not.toBeVisible();
});

test('Confirmar eliminación remueve la card del usuario', async ({ page }) => {
  await expect(page.getByTestId('registrado-card')).toHaveCount(2);
  await page.getByLabel('Eliminar usuario').first().click();
  await page.getByRole('button', { name: 'Eliminar' }).click();
  await expect(page.getByTestId('registrado-card')).toHaveCount(1);
});

test('Botón editar abre modal de confirmación', async ({ page }) => {
  await page.getByLabel('Editar usuario').first().click();
  await expect(page.getByText('Confirmar edicion')).toBeVisible();
});

test('Cancelar modal de edición lo cierra', async ({ page }) => {
  await page.getByLabel('Editar usuario').first().click();
  await expect(page.getByText('Confirmar edicion')).toBeVisible();
  await page.getByRole('button', { name: 'Cancelar' }).click();
  await expect(page.getByText('Confirmar edicion')).not.toBeVisible();
});

test('Link Usuarios navega a /usuarios', async ({ page }) => {
  await page.getByRole('link', { name: 'Usuarios', exact: true }).click();
  await expect(page).toHaveURL(/\/usuarios$/);
});

test('Link Logs de usuarios navega a /usuarios/logs', async ({ page }) => {
  await page.getByRole('link', { name: 'Logs de usuarios' }).click();
  await expect(page).toHaveURL(/\/usuarios\/logs/);
});