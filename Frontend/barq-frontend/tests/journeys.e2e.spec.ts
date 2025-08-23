import { test, expect } from "@playwright/test";

test.describe("Critical User Journeys", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/");
    await page.waitForLoadState('networkidle');
  });

  test("Journey 1: Request → Approval → AI → QA → Complete", async ({ page }) => {
    await page.click('[data-testid="create-task"]');
    await page.fill('[data-testid="task-title"]', 'Test AI Task');
    await page.fill('[data-testid="task-description"]', 'Test description for AI processing');
    await page.check('[data-testid="requires-approval"]');
    await page.selectOption('[data-testid="task-priority"]', 'high');
    await page.click('[data-testid="submit-task"]');
    
    await expect(page.locator('[data-testid="task-created-message"]')).toBeVisible();
    
    await page.click('[data-testid="pending-approvals"]');
    await page.click('[data-testid="approve-task"]');
    await page.fill('[data-testid="approval-reason"]', 'Approved for AI processing');
    await page.click('[data-testid="confirm-approval"]');
    
    await expect(page.locator('[data-testid="task-approved"]')).toBeVisible();
    
    await page.click('[data-testid="run-ai"]');
    await page.selectOption('[data-testid="ai-provider"]', 'openai');
    await page.fill('[data-testid="ai-prompt"]', 'Process this task with high quality output');
    await page.click('[data-testid="execute-ai"]');
    
    await page.waitForSelector('[data-testid="ai-completed"]', { timeout: 30000 });
    await expect(page.locator('[data-testid="ai-result"]')).toBeVisible();
    
    await page.click('[data-testid="qa-review"]');
    await page.fill('[data-testid="qa-notes"]', 'Quality review passed');
    await page.click('[data-testid="approve-qa"]');
    
    await expect(page.locator('[data-testid="task-status"]')).toContainText('Completed');
    await expect(page.locator('[data-testid="completion-timestamp"]')).toBeVisible();
  });

  test("Journey 2: SLA Breach → Escalation", async ({ page }) => {
    await page.click('[data-testid="create-task"]');
    await page.fill('[data-testid="task-title"]', 'SLA Test Task');
    await page.fill('[data-testid="task-description"]', 'Task to test SLA monitoring');
    await page.selectOption('[data-testid="task-priority"]', 'critical');
    await page.selectOption('[data-testid="sla-policy"]', 'critical-1hour');
    await page.click('[data-testid="submit-task"]');
    
    await page.click('[data-testid="admin-panel"]');
    await page.click('[data-testid="sla-monitor"]');
    
    await page.click('[data-testid="simulate-sla-breach"]');
    await page.fill('[data-testid="breach-task-id"]', 'test-task-id');
    await page.click('[data-testid="trigger-breach"]');
    
    await expect(page.locator('[data-testid="sla-violation-created"]')).toBeVisible();
    
    await page.click('[data-testid="escalation-actions"]');
    await expect(page.locator('[data-testid="escalation-level-1"]')).toBeVisible();
    
    await page.click('[data-testid="notifications"]');
    await expect(page.locator('[data-testid="sla-breach-notification"]')).toBeVisible();
    
    await page.click('[data-testid="task-reassignment"]');
    await expect(page.locator('[data-testid="escalated-assignee"]')).toBeVisible();
    
    await page.click('[data-testid="webhook-logs"]');
    await expect(page.locator('[data-testid="escalation-webhook-sent"]')).toBeVisible();
  });

  test("Journey 3: File Attach → AV Scan → Notify", async ({ page }) => {
    await page.click('[data-testid="create-task"]');
    await page.fill('[data-testid="task-title"]', 'File Upload Test');
    await page.fill('[data-testid="task-description"]', 'Task with file attachment');
    
    const fileInput = page.locator('[data-testid="file-upload"]');
    await fileInput.setInputFiles({
      name: 'test-document.pdf',
      mimeType: 'application/pdf',
      buffer: new Uint8Array([0x25, 0x50, 0x44, 0x46]) // PDF header bytes
    });
    
    await page.click('[data-testid="upload-file"]');
    await expect(page.locator('[data-testid="upload-progress"]')).toBeVisible();
    await expect(page.locator('[data-testid="file-uploaded"]')).toBeVisible();
    
    await expect(page.locator('[data-testid="av-scanning"]')).toBeVisible();
    await expect(page.locator('[data-testid="scan-status"]')).toContainText('Scanning');
    
    await page.waitForSelector('[data-testid="scan-completed"]', { timeout: 15000 });
    await expect(page.locator('[data-testid="scan-result"]')).toContainText('Clean');
    
    await page.click('[data-testid="notifications"]');
    await expect(page.locator('[data-testid="file-scan-notification"]')).toBeVisible();
    await expect(page.locator('[data-testid="notification-message"]')).toContainText('File scan completed - file is clean');
    
    await page.click('[data-testid="submit-task"]');
    await expect(page.locator('[data-testid="task-created-with-attachment"]')).toBeVisible();
  });

  test("Journey 4: Billing Cap → 402 → Upgrade Flow", async ({ page }) => {
    await page.click('[data-testid="admin-panel"]');
    await page.click('[data-testid="billing-settings"]');
    
    await page.click('[data-testid="simulate-quota-exceeded"]');
    await page.click('[data-testid="trigger-billing-cap"]');
    
    await page.click('[data-testid="create-task"]');
    await page.fill('[data-testid="task-title"]', 'Over Quota Task');
    await page.fill('[data-testid="task-description"]', 'This should trigger billing limit');
    await page.click('[data-testid="submit-task"]');
    
    await expect(page.locator('[data-testid="billing-error"]')).toBeVisible();
    await expect(page.locator('[data-testid="error-code"]')).toContainText('402');
    await expect(page.locator('[data-testid="quota-exceeded-message"]')).toBeVisible();
    
    await page.click('[data-testid="upgrade-plan-button"]');
    await expect(page.locator('[data-testid="billing-plans"]')).toBeVisible();
    
    await page.click('[data-testid="select-premium-plan"]');
    await page.fill('[data-testid="payment-method"]', '4111111111111111');
    await page.fill('[data-testid="expiry-date"]', '12/25');
    await page.fill('[data-testid="cvv"]', '123');
    await page.click('[data-testid="confirm-upgrade"]');
    
    await expect(page.locator('[data-testid="upgrade-successful"]')).toBeVisible();
    await expect(page.locator('[data-testid="new-quota-limit"]')).toBeVisible();
    
    await page.click('[data-testid="create-task"]');
    await page.fill('[data-testid="task-title"]', 'Post Upgrade Task');
    await page.fill('[data-testid="task-description"]', 'This should work after upgrade');
    await page.click('[data-testid="submit-task"]');
    
    await expect(page.locator('[data-testid="task-created-message"]')).toBeVisible();
  });

  test("Journey 5: Multi-tenant Data Isolation", async ({ page }) => {
    await page.click('[data-testid="tenant-switcher"]');
    await page.selectOption('[data-testid="tenant-select"]', 'tenant-a');
    await page.click('[data-testid="switch-tenant"]');
    
    await page.click('[data-testid="create-task"]');
    await page.fill('[data-testid="task-title"]', 'Tenant A Task');
    await page.fill('[data-testid="task-description"]', 'Task for tenant A only');
    await page.click('[data-testid="submit-task"]');
    
    await expect(page.locator('[data-testid="task-created-message"]')).toBeVisible();
    
    await page.click('[data-testid="tenant-switcher"]');
    await page.selectOption('[data-testid="tenant-select"]', 'tenant-b');
    await page.click('[data-testid="switch-tenant"]');
    
    await page.click('[data-testid="tasks-list"]');
    await expect(page.locator('[data-testid="tenant-a-task"]')).not.toBeVisible();
    
    await page.click('[data-testid="create-task"]');
    await page.fill('[data-testid="task-title"]', 'Tenant B Task');
    await page.fill('[data-testid="task-description"]', 'Task for tenant B only');
    await page.click('[data-testid="submit-task"]');
    
    await expect(page.locator('[data-testid="task-created-message"]')).toBeVisible();
    await expect(page.locator('[data-testid="tenant-b-task"]')).toBeVisible();
  });
});
