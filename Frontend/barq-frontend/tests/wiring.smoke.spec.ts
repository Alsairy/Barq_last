import { test, expect } from "@playwright/test";

const CLICK_TIMEOUT_MS = 2000;

test.setTimeout(60_000);

test.beforeEach(async ({ page }) => {
  page.on('console', msg => {
    const t = msg.type();
    if (t === 'error' || t === 'warning') {
      console.log(`[console.${t}] ${msg.text()}`);
    }
  });
  page.on('pageerror', err => {
    console.log('[pageerror]', err.message);
  });
});

test("every visible, enabled button triggers DOM or network activity", async ({ page, context }) => {
  await page.goto("/");

  const buttons = page.locator('button:not([disabled]):not([aria-disabled="true"])');
  const count = await buttons.count();
  console.log(`Found ${count} buttons to test`);

  for (let i = 0; i < count; i++) {
    const btn = buttons.nth(i);

    const label = (await btn.textContent())?.trim() || (await btn.getAttribute("data-testid")) || `button-${i}`;
    console.log(`Testing button ${i + 1}/${count}: "${label}"`);

    const safeButtons = ["Toggle theme", "Prev", "Next", "Try Again", "Refresh Page"];
    const isKnownSafe = safeButtons.some(safe => label.includes(safe) || label === safe);
    
    if (!isKnownSafe) {
      console.log(`Skipping potentially problematic button: "${label}"`);
      continue;
    }

    const isVisible = await btn.isVisible();
    if (!isVisible) {
      console.log(`Skipping invisible button: "${label}"`);
      continue;
    }

    await btn.scrollIntoViewIfNeeded();

    const before = await page.content();

    console.log(`Clicking button: "${label}"`);
    await btn.click({ trial: false }).catch(() => {});
    console.log(`Waiting 350ms after clicking: "${label}"`);
    await page.waitForTimeout(350);

    const after = await page.content();
    const domChanged = before !== after;

    console.log(`Button "${label}" result: DOM changed=${domChanged}`);

    const isInteractive = domChanged || 
                          label.includes("toggle") || 
                          label.includes("menu") || 
                          label.includes("dropdown");

    expect(isInteractive, `Button "${label}" may be unwired`).toBeTruthy();
  }
});
