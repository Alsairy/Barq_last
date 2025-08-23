import { test, expect } from "@playwright/test";

const CLICK_TIMEOUT_MS = 2000;

test("every visible, enabled button triggers DOM or network activity", async ({ page, context }) => {
  await page.goto("/");

  const buttons = page.locator('button:not([disabled]):not([aria-disabled="true"])');
  const count = await buttons.count();

  for (let i = 0; i < count; i++) {
    const btn = buttons.nth(i);

    const label = (await btn.textContent())?.trim() || (await btn.getAttribute("data-testid")) || `button-${i}`;

    const isVisible = await btn.isVisible();
    if (!isVisible) continue;

    await btn.scrollIntoViewIfNeeded();

    const before = await page.content();

    const networkPromise = context.waitForEvent("request", {
      timeout: CLICK_TIMEOUT_MS
    }).catch(() => null);

    await btn.click({ trial: false }).catch(() => {});
    await page.waitForTimeout(350);

    const after = await page.content();
    const domChanged = before !== after;

    const req = await networkPromise;
    const networkHappened =
      !!req && !/(\.map|favicon\.ico|hot-update)/i.test(req.url());

    const actionable = domChanged || networkHappened;

    expect(actionable, `Button "${label}" may be unwired`).toBeTruthy();
  }
});
