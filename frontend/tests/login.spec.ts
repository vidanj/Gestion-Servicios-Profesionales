import { test, expect } from '@playwright/test';

test('Login page loads correctly', async ({ page }) => {
    await page.goto('http://localhost:3000/login');

    await expect(page.getByTestId('username-input')).toBeVisible();
    await expect(page.getByTestId('password-input')).toBeVisible();
    await expect(page.getByTestId('login-button')).toBeVisible();
});