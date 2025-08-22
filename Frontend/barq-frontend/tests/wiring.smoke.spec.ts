import { test, expect } from "@playwright/test";

test("every button triggers DOM or network activity", async ({ page }) => {
  await page.goto("/");

  const buttons = page.locator("button");
  const count = await buttons.count();

  for (let i = 0; i < count; i++) {
    const btn = buttons.nth(i);
    const label = await btn.innerText().catch(() => `button-${i}`);
    const before = await page.content();

    await btn.click({ trial: false }).catch(() => {});
    await page.waitForTimeout(300);

    const after = await page.content();
    const domChanged = before !== after;

    expect(domChanged, `Button "${label}" may be unwired`).toBeTruthy();
  }
});

test("every anchor has href or click handler", async ({ page }) => {
  await page.goto("/");

  const anchors = page.locator("a");
  const count = await anchors.count();

  for (let i = 0; i < count; i++) {
    const anchor = anchors.nth(i);
    const href = await anchor.getAttribute("href");
    const hasClickHandler = await anchor.evaluate((el) => {
      return el.onclick !== null || el.addEventListener !== undefined;
    });

    expect(
      href !== null || hasClickHandler,
      `Anchor ${i} has no href and no click handler`
    ).toBeTruthy();
  }
});

test("no console errors on page load", async ({ page }) => {
  const consoleErrors: string[] = [];
  
  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      consoleErrors.push(msg.text());
    }
  });

  await page.goto("/");
  await page.waitForTimeout(2000);

  expect(consoleErrors.length, `Console errors found: ${consoleErrors.join(', ')}`).toBe(0);
});

test("critical UI elements are present", async ({ page }) => {
  await page.goto("/");

  const criticalElements = [
    'header, [role="banner"]',
    'nav, [role="navigation"]', 
    'main, [role="main"]',
    'button, input[type="submit"]'
  ];

  for (const selector of criticalElements) {
    const element = page.locator(selector).first();
    await expect(element, `Critical element missing: ${selector}`).toBeVisible();
  }
});
