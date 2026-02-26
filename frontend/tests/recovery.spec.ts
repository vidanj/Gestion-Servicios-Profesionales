import { test, expect } from '@playwright/test';

test('Recovery page loads correctly', async ({ page }) => {
    await page.goto('http://localhost:3000/recovery');

    await expect(page.getByTestId('email-input')).toBeVisible();
    await expect(page.getByTestId('submit-button')).toBeVisible();
    await expect(page.getByTestId('back-to-login')).toBeVisible();
});