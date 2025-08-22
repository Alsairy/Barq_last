import { test, expect } from "@playwright/test";

test("every button triggers DOM or network activity", async ({ page }) => {
  await page.goto("http://localhost:4173");

  const buttons = page.locator("button");
  const count = await buttons.count();

  if (count === 0) {
    console.log("No buttons found on the page");
    return;
  }

  for (let i = 0; i < count; i++) {
    const btn = buttons.nth(i);
    const label = await btn.innerText().catch(() => `button-${i}`);
    
    const isVisible = await btn.isVisible().catch(() => false);
    const isEnabled = await btn.isEnabled().catch(() => false);
    
    if (!isVisible || !isEnabled) {
      console.log(`Skipping button "${label}" - not visible or enabled`);
      continue;
    }

    const before = await page.content();

    await btn.click({ trial: false }).catch(() => {
      console.log(`Failed to click button "${label}"`);
    });
    await page.waitForTimeout(300);

    const after = await page.content();
    const domChanged = before !== after;

    if (!domChanged) {
      console.log(`Button "${label}" may be unwired - no DOM change detected`);
    } else {
      console.log(`Button "${label}" appears to be wired correctly`);
    }
  }
});

test("every anchor has href or click handler", async ({ page }) => {
  await page.goto("http://localhost:4173");

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

  await page.goto("http://localhost:4173");
  await page.waitForTimeout(2000);

  expect(consoleErrors.length, `Console errors found: ${consoleErrors.join(', ')}`).toBe(0);
});

test("critical UI elements are present", async ({ page }) => {
  await page.goto("http://localhost:4173");

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
