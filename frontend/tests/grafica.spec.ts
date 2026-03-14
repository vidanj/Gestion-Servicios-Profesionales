import { test, expect } from '@playwright/test';

const mockStats = Array.from({ length: 30 }, (_, i) => ({
  date: new Date(Date.now() - (29 - i) * 86400000).toISOString().slice(0, 10),
  count: Math.floor(Math.random() * 5),
}));

test.beforeEach(async ({ page }) => {
  await page.route('**/api/Users/stats/registrations**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockStats),
    });
  });

  await page.addInitScript(() => {
    localStorage.setItem('token', 'fake-token');
  });

  await page.goto('http://localhost:3000/usuarios/grafica');
});

test('Gráfica página carga correctamente', async ({ page }) => {
  await expect(page.getByRole('heading', { name: 'Gráfica de registros' })).toBeVisible();
});

test('Gráfica muestra el SVG con barras', async ({ page }) => {
  await expect(page.getByTestId('grafica-svg')).toBeVisible();
  const bars = page.getByTestId('grafica-bar');
  await expect(bars).toHaveCount(30);
});

test('Gráfica muestra estado de error cuando falla la API', async ({ page }) => {
  await page.route('**/api/Users/stats/registrations**', async route => {
    await route.fulfill({ status: 500 });
  });
  await page.goto('http://localhost:3000/usuarios/grafica');
  await expect(page.getByTestId('grafica-error')).toBeVisible();
});

test('Filtro 7d envía parámetro correcto', async ({ page }) => {
  let urlCapturada = '';
  await page.route('**/api/Users/stats/registrations**', async route => {
    urlCapturada = route.request().url();
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockStats.slice(0, 7)),
    });
  });

  await page.getByTestId('filter-days-7').click();
  await expect(() => expect(urlCapturada).toContain('days=7')).toPass({ timeout: 3000 });
});

test('Filtro 14d envía parámetro correcto', async ({ page }) => {
  let urlCapturada = '';
  await page.route('**/api/Users/stats/registrations**', async route => {
    urlCapturada = route.request().url();
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockStats.slice(0, 14)),
    });
  });

  await page.getByTestId('filter-days-14').click();
  await expect(() => expect(urlCapturada).toContain('days=14')).toPass({ timeout: 3000 });
});

test('Filtro 90d envía parámetro correcto', async ({ page }) => {
  let urlCapturada = '';
  await page.route('**/api/Users/stats/registrations**', async route => {
    urlCapturada = route.request().url();
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockStats),
    });
  });

  await page.getByTestId('filter-days-90').click();
  await expect(() => expect(urlCapturada).toContain('days=90')).toPass({ timeout: 3000 });
});

test('Gráfica muestra total de usuarios', async ({ page }) => {
  const total = mockStats.reduce((sum, s) => sum + s.count, 0);
  await expect(page.getByText(`${total}`)).toBeVisible();
});

test('Link CRUD navega a /usuarios', async ({ page }) => {
  await page.getByRole('link', { name: 'CRUD', exact: true }).click();
  await expect(page).toHaveURL(/\/usuarios$/);
});

test('Link Logs navega a /usuarios/logs', async ({ page }) => {
  await page.getByRole('link', { name: 'Logs de usuarios' }).click();
  await expect(page).toHaveURL(/\/usuarios\/logs/);
});

test('Link Usuarios registrados navega a /usuarios/registrados', async ({ page }) => {
  await page.getByRole('link', { name: 'Usuarios registrados' }).click();
  await expect(page).toHaveURL(/\/usuarios\/registrados/);
});
