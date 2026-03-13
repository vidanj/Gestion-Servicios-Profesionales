import { test, expect } from '@playwright/test';

// Respuesta mock que devuelve el backend para los cálculos.
const MOCK_RESPONSE = {
    basePrice: 250,
    complexityModifierPercent: 0.15,
    urgencyModifierPercent: 0.20,
    revisionsModifierPercent: 0.10,
    prioritySupportFee: 35,
    weekendDeliveryFee: 20,
    estimatedTotal: 448.75,
    notes: [
        'Complejidad elegida: media.',
        'Urgencia elegida: urgente.',
        'Revisiones extra solicitadas: 2.',
        'Este cálculo es estimado y puede cambiar según negociación final.',
    ],
};

async function mockEstimate(page: any, response = MOCK_RESPONSE, status = 200) {
    await page.route('**/api/pricing/estimate', async (route: any) => {
        await route.fulfill({
            status,
            contentType: 'application/json',
            body: JSON.stringify(response),
        });
    });
}

// ─── Carga de la página ───────────────────────────────────────────────────────

test('Cotizador carga correctamente y muestra el formulario', async ({ page }) => {
    await page.goto('http://localhost:3000/cotizador');

    await expect(page.getByTestId('cotizador-precio-base')).toBeVisible();
    await expect(page.getByTestId('cotizador-complejidad')).toBeVisible();
    await expect(page.getByTestId('cotizador-urgencia')).toBeVisible();
    await expect(page.getByTestId('cotizador-revisiones')).toBeVisible();
    await expect(page.getByTestId('cotizador-submit')).toBeVisible();
});

test('Cotizador muestra valor por defecto de precio base 250', async ({ page }) => {
    await page.goto('http://localhost:3000/cotizador');

    await expect(page.getByTestId('cotizador-precio-base')).toHaveValue('250');
});

// ─── Cálculo exitoso ──────────────────────────────────────────────────────────

test('Cotizador muestra el total estimado tras calcular', async ({ page }) => {
    await mockEstimate(page);

    await page.goto('http://localhost:3000/cotizador');
    await page.getByTestId('cotizador-submit').click();

    await expect(page.getByTestId('cotizador-total')).toBeVisible({ timeout: 5000 });
    await expect(page.getByTestId('cotizador-total')).toContainText('448.75');
});

test('Cotizador envía los campos del formulario correctamente', async ({ page }) => {
    let requestBody: any = null;

    await page.route('**/api/pricing/estimate', async (route) => {
        requestBody = JSON.parse(route.request().postData() ?? '{}');
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify(MOCK_RESPONSE),
        });
    });

    await page.goto('http://localhost:3000/cotizador');

    await page.getByTestId('cotizador-precio-base').fill('300');
    await page.getByTestId('cotizador-complejidad').selectOption('alta');
    await page.getByTestId('cotizador-urgencia').selectOption('express');
    await page.getByTestId('cotizador-revisiones').fill('3');
    await page.getByTestId('cotizador-submit').click();

    await expect(page.getByTestId('cotizador-total')).toBeVisible({ timeout: 5000 });

    expect(requestBody.basePrice).toBe(300);
    expect(requestBody.complexityLevel).toBe('alta');
    expect(requestBody.urgencyLevel).toBe('express');
    expect(requestBody.extraRevisions).toBe(3);
});

// ─── Extras (checkboxes) ──────────────────────────────────────────────────────

test('Cotizador incluye soporte prioritario al marcarlo', async ({ page }) => {
    let requestBody: any = null;

    await page.route('**/api/pricing/estimate', async (route) => {
        requestBody = JSON.parse(route.request().postData() ?? '{}');
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify(MOCK_RESPONSE),
        });
    });

    await page.goto('http://localhost:3000/cotizador');
    await page.getByTestId('cotizador-soporte').click();
    await page.getByTestId('cotizador-submit').click();

    await expect(page.getByTestId('cotizador-total')).toBeVisible({ timeout: 5000 });
    expect(requestBody.includePrioritySupport).toBe(true);
});

test('Cotizador incluye entrega fin de semana al marcarla', async ({ page }) => {
    let requestBody: any = null;

    await page.route('**/api/pricing/estimate', async (route) => {
        requestBody = JSON.parse(route.request().postData() ?? '{}');
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify(MOCK_RESPONSE),
        });
    });

    await page.goto('http://localhost:3000/cotizador');
    await page.getByTestId('cotizador-fds').click();
    await page.getByTestId('cotizador-submit').click();

    await expect(page.getByTestId('cotizador-total')).toBeVisible({ timeout: 5000 });
    expect(requestBody.includeWeekendDelivery).toBe(true);
});

// ─── Validación de precio base ────────────────────────────────────────────────

test('Cotizador muestra error si el precio base es 0', async ({ page }) => {
    await page.goto('http://localhost:3000/cotizador');

    await page.getByTestId('cotizador-precio-base').fill('0');
    await page.getByTestId('cotizador-submit').click();

    await expect(page.getByTestId('cotizador-error')).toBeVisible();
    await expect(page.getByTestId('cotizador-error')).toContainText('El precio base debe ser mayor que 0');
});

// ─── Error de servidor ────────────────────────────────────────────────────────

test('Cotizador muestra error si la API falla', async ({ page }) => {
    await mockEstimate(page, {} as any, 500);

    await page.goto('http://localhost:3000/cotizador');
    await page.getByTestId('cotizador-submit').click();

    await expect(page.getByTestId('cotizador-error')).toBeVisible({ timeout: 5000 });
    await expect(page.getByTestId('cotizador-error')).toContainText('No pude conectarme con el servidor');
});
