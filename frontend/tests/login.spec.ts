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

test('Login por PIN solicita, verifica y redirige al dashboard', async ({ page }) => {
    await page.route('**/api/auth/login', async route => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
                token: 'temp-password-token',
                firstName: 'Juan',
                lastName: 'Pérez',
                email: 'juan@ejemplo.com',
                role: 'Client'
            })
        });
    });

    await page.route('**/api/auth/login-pin/request', async route => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
                message: 'Si el correo está registrado y la cuenta está activa, recibirás un PIN en breve.'
            })
        });
    });

    await page.route('**/api/auth/login-pin/verify', async route => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
                token: 'fake-pin-token',
                firstName: 'Juan',
                lastName: 'Pérez',
                email: 'juan@ejemplo.com',
                role: 'Client'
            })
        });
    });

    await page.goto('http://localhost:3000/login');
    await page.getByRole('button', { name: 'PIN por correo' }).click();
    await page.getByTestId('username-input').fill('juan@ejemplo.com');
    await page.getByTestId('password-input').fill('password123');
    await page.getByTestId('login-button').click();

    await expect(page.getByTestId('success-message')).toHaveText('Se envió un PIN a tu correo. Revisa tu bandeja de entrada.');
    await expect(page.getByTestId('pin-input')).toBeVisible();

    await page.getByTestId('pin-input').fill('123456');
    await page.getByTestId('login-button').click();

    await expect(page).toHaveURL(/dashboard/, { timeout: 10000 });
});

test('Login por PIN con código incorrecto muestra error', async ({ page }) => {
    await page.route('**/api/auth/login', async route => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
                token: 'temp-password-token',
                firstName: 'Juan',
                lastName: 'Pérez',
                email: 'juan@ejemplo.com',
                role: 'Client'
            })
        });
    });

    await page.route('**/api/auth/login-pin/request', async route => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
                message: 'Si el correo está registrado y la cuenta está activa, recibirás un PIN en breve.'
            })
        });
    });

    await page.route('**/api/auth/login-pin/verify', async route => {
        await route.fulfill({
            status: 401,
            contentType: 'application/json',
            body: JSON.stringify({ message: 'PIN incorrecto.' })
        });
    });

    await page.goto('http://localhost:3000/login');
    await page.getByRole('button', { name: 'PIN por correo' }).click();
    await page.getByTestId('username-input').fill('juan@ejemplo.com');
    await page.getByTestId('password-input').fill('password123');
    await page.getByTestId('login-button').click();
    await page.getByTestId('pin-input').fill('654321');
    await page.getByTestId('login-button').click();

    await expect(page.getByTestId('error-message')).toHaveText('PIN incorrecto.');
});