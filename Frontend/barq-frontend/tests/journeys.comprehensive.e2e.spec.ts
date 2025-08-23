import { test, expect } from '@playwright/test';

declare global {
  interface Window {
    mockSLABreach?: boolean;
    mockVirusDetected?: boolean;
    mockBillingCapReached?: boolean;
  }
}

test.describe('BARQ Comprehensive E2E Journeys', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.evaluate(() => {
      localStorage.setItem('auth_token', 'test-jwt-token');
      localStorage.setItem('tenant_id', 'test-tenant-123');
    });
  });

  test('Journey 1: Request → Approval → AI → QA → Complete', async ({ page }) => {
    await page.click('[data-testid="create-task-btn"]');
    
    await page.fill('[data-testid="task-title"]', 'E2E Test Task - Request to Complete');
    await page.fill('[data-testid="task-description"]', 'Comprehensive workflow test from request to completion');
    await page.selectOption('[data-testid="task-priority"]', 'High');
    
    await page.click('[data-testid="submit-task-btn"]');
    
    await expect(page.locator('[data-testid="task-status"]')).toContainText('Pending Approval');
    
    await page.click('[data-testid="approve-task-btn"]');
    await expect(page.locator('[data-testid="task-status"]')).toContainText('Approved');
    
    await page.click('[data-testid="start-ai-processing-btn"]');
    await expect(page.locator('[data-testid="task-status"]')).toContainText('AI Processing');
    
    await page.waitForTimeout(2000);
    await expect(page.locator('[data-testid="task-status"]')).toContainText('QA Review');
    
    await page.click('[data-testid="qa-approve-btn"]');
    await expect(page.locator('[data-testid="task-status"]')).toContainText('Completed');
    
    await expect(page.locator('[data-testid="completion-time"]')).toBeVisible();
    await expect(page.locator('[data-testid="ai-cost"]')).toBeVisible();
  });

  test('Journey 2: SLA Breach → Escalation → Notify → Reassign', async ({ page }) => {
    await page.click('[data-testid="create-task-btn"]');
    await page.fill('[data-testid="task-title"]', 'SLA Breach Test Task');
    await page.selectOption('[data-testid="sla-policy"]', 'Critical-1Hour');
    await page.click('[data-testid="submit-task-btn"]');
    
    await page.evaluate(() => {
      window.mockSLABreach = true;
    });
    
    await page.click('[data-testid="check-sla-btn"]');
    
    await expect(page.locator('[data-testid="sla-violation-alert"]')).toBeVisible();
    await expect(page.locator('[data-testid="escalation-level"]')).toContainText('Level 1');
    
    await expect(page.locator('[data-testid="escalation-notification"]')).toContainText('Manager notified');
    
    await page.click('[data-testid="escalate-further-btn"]');
    await expect(page.locator('[data-testid="escalation-level"]')).toContainText('Level 2');
    
    await expect(page.locator('[data-testid="task-assignee"]')).not.toContainText('Original Assignee');
    await expect(page.locator('[data-testid="webhook-sent"]')).toBeVisible();
  });

  test('Journey 3: File Attachment → AV Scan → Quarantine → Notify', async ({ page }) => {
    await page.click('[data-testid="create-task-btn"]');
    await page.fill('[data-testid="task-title"]', 'File Attachment Test');
    
    const fileInput = page.locator('[data-testid="file-upload"]');
    await fileInput.setInputFiles({
      name: 'test-document.pdf',
      mimeType: 'application/pdf',
      buffer: new Uint8Array([0x25, 0x50, 0x44, 0x46]) // Mock PDF header bytes
    });
    
    await page.click('[data-testid="submit-task-btn"]');
    
    await expect(page.locator('[data-testid="file-status"]')).toContainText('Scanning');
    
    await page.evaluate(() => {
      window.mockVirusDetected = true;
    });
    
    await page.click('[data-testid="complete-av-scan-btn"]');
    
    await expect(page.locator('[data-testid="file-status"]')).toContainText('Quarantined');
    await expect(page.locator('[data-testid="quarantine-reason"]')).toContainText('Virus detected');
    
    await expect(page.locator('[data-testid="security-notification"]')).toBeVisible();
    await expect(page.locator('[data-testid="admin-notified"]')).toContainText('Security team notified');
    
    await page.click('[data-testid="download-file-btn"]');
    await expect(page.locator('[data-testid="access-denied"]')).toBeVisible();
  });

  test('Journey 4: Billing Cap → 402 Response → Upgrade Flow', async ({ page }) => {
    await page.click('[data-testid="ai-intensive-task-btn"]');
    
    await page.evaluate(() => {
      window.mockBillingCapReached = true;
    });
    
    await page.click('[data-testid="start-expensive-ai-btn"]');
    
    await expect(page.locator('[data-testid="payment-required-modal"]')).toBeVisible();
    await expect(page.locator('[data-testid="error-code"]')).toContainText('402');
    await expect(page.locator('[data-testid="billing-cap-message"]')).toContainText('Monthly usage limit exceeded');
    
    await expect(page.locator('[data-testid="upgrade-options"]')).toBeVisible();
    await expect(page.locator('[data-testid="current-plan"]')).toBeVisible();
    await expect(page.locator('[data-testid="recommended-plan"]')).toBeVisible();
    
    await page.click('[data-testid="upgrade-to-pro-btn"]');
    
    await expect(page.url()).toContain('/billing/upgrade');
    await expect(page.locator('[data-testid="plan-comparison"]')).toBeVisible();
    
    await page.click('[data-testid="confirm-upgrade-btn"]');
    await expect(page.locator('[data-testid="upgrade-success"]')).toBeVisible();
    
    await page.goBack();
    await page.click('[data-testid="start-expensive-ai-btn"]');
    await expect(page.locator('[data-testid="ai-processing"]')).toBeVisible();
  });

  test('Journey 5: Multi-tenant Isolation Verification', async ({ page }) => {
    await page.evaluate(() => {
      localStorage.setItem('tenant_id', 'tenant-a-123');
      localStorage.setItem('user_id', 'user-a-456');
    });
    
    await page.click('[data-testid="create-task-btn"]');
    await page.fill('[data-testid="task-title"]', 'Tenant A Task');
    await page.click('[data-testid="submit-task-btn"]');
    
    const tenantATaskId = await page.locator('[data-testid="task-id"]').textContent();
    
    await page.evaluate(() => {
      localStorage.setItem('tenant_id', 'tenant-b-789');
      localStorage.setItem('user_id', 'user-b-012');
    });
    
    await page.reload();
    
    await page.goto('/tasks');
    await expect(page.locator(`[data-testid="task-${tenantATaskId}"]`)).not.toBeVisible();
    
    await page.click('[data-testid="create-task-btn"]');
    await page.fill('[data-testid="task-title"]', 'Tenant B Task');
    await page.click('[data-testid="submit-task-btn"]');
    
    await expect(page.locator('[data-testid="task-list"]')).toContainText('Tenant B Task');
    await expect(page.locator('[data-testid="task-list"]')).not.toContainText('Tenant A Task');
    
    const response = await page.request.get(`/api/tasks/${tenantATaskId}`, {
      headers: {
        'Authorization': 'Bearer test-jwt-token',
        'X-Tenant-Id': 'tenant-b-789'
      }
    });
    
    expect([403, 404]).toContain(response.status());
  });

  test('Journey 6: Real-time Collaboration & Notifications', async ({ page, context }) => {
    const page2 = await context.newPage();
    
    await page.evaluate(() => {
      localStorage.setItem('user_id', 'user-1');
      localStorage.setItem('user_name', 'Alice');
    });
    
    await page2.goto('/');
    await page2.evaluate(() => {
      localStorage.setItem('user_id', 'user-2');
      localStorage.setItem('user_name', 'Bob');
    });
    
    await page.click('[data-testid="create-task-btn"]');
    await page.fill('[data-testid="task-title"]', 'Collaborative Task');
    await page.click('[data-testid="add-collaborator-btn"]');
    await page.fill('[data-testid="collaborator-email"]', 'bob@example.com');
    await page.click('[data-testid="submit-task-btn"]');
    
    await page2.reload();
    await expect(page2.locator('[data-testid="notification-bell"]')).toHaveClass(/.*notification-active.*/);
    await page2.click('[data-testid="notification-bell"]');
    await expect(page2.locator('[data-testid="notification-list"]')).toContainText('Collaborative Task');
    
    await page2.click('[data-testid="notification-task-link"]');
    await page2.fill('[data-testid="comment-input"]', 'Great idea! I can help with this.');
    await page2.click('[data-testid="add-comment-btn"]');
    
    await expect(page.locator('[data-testid="comments-section"]')).toContainText('Great idea! I can help with this.');
    await expect(page.locator('[data-testid="comment-author"]')).toContainText('Bob');
    
    await expect(page.locator('[data-testid="activity-feed"]')).toContainText('Bob added a comment');
  });
});
