import { test, expect } from '@playwright/test';

test('Login page loads correctly', async ({ page }) => {
    await page.goto('http://localhost:3000/login');

    await expect(page.getByTestId('username-input')).toBeVisible();
    await expect(page.getByTestId('password-input')).toBeVisible();
    await expect(page.getByTestId('login-button')).toBeVisible();
});

test('Login exitoso redirige al dashboard', async ({ page }) => {
    await page.route('**/api/auth/login', async route => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
                token: 'fake-jwt-token',
                user: { id: 1, email: 'juan@ejemplo.com' }
            })
        });
    });

    await page.goto('http://localhost:3000/login');
    await page.getByTestId('username-input').fill('juan@ejemplo.com');
    await page.getByTestId('password-input').fill('password123');
    await page.getByTestId('login-button').click();

    await expect(page).toHaveURL(/dashboard/, { timeout: 10000 });
});

test('Login con credenciales inválidas muestra error', async ({ page }) => {
    // Intercepta la llamada antes de navegar
    await page.route('**/api/auth/login', async route => {
        await route.fulfill({
            status: 401,
            contentType: 'application/json',
            body: JSON.stringify({ message: 'Unauthorized' }),
        });
    });

    await page.goto('http://localhost:3000/login');

    await page.getByTestId('username-input').fill('mal@email.com');
    await page.getByTestId('password-input').fill('wrongpassword');
    await page.getByTestId('login-button').click();

    await expect(page.getByTestId('error-message')).toBeVisible();
    await expect(page.getByTestId('error-message')).toHaveText('Credenciales inválidas');
});

test('Login con email vacío muestra validación', async ({ page }) => {
    await page.goto('http://localhost:3000/login');

    await page.getByTestId('login-button').click();

    await expect(page.getByTestId('error-message')).toHaveText('El email es obligatorio');
});

test('Login con email inválido muestra validación', async ({ page }) => {
    await page.goto('http://localhost:3000/login');

    await page.getByTestId('username-input').fill('noesunemail');
    await page.getByTestId('login-button').click();

    await expect(page.getByTestId('error-message')).toHaveText('El email no es válido');
});

test('Login con contraseña corta muestra validación', async ({ page }) => {
    await page.goto('http://localhost:3000/login');

    await page.getByTestId('username-input').fill('juan@ejemplo.com');
    await page.getByTestId('password-input').fill('123');
    await page.getByTestId('login-button').click();

    await expect(page.getByTestId('error-message')).toHaveText('La contraseña debe tener al menos 6 caracteres');
});