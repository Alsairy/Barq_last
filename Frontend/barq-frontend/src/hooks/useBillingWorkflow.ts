import { useState, useCallback, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { billingApi, BillingStatus } from '../services/api';
import { toast } from 'sonner';

export interface BillingWorkflowState {
  status?: BillingStatus;
  plans: any[];
  upgrading: boolean;
  error?: string;
}

export function useBillingWorkflow() {
  const navigate = useNavigate();
  const [state, setState] = useState<BillingWorkflowState>({
    plans: [],
    upgrading: false
  });

  const executeWithRetry = useCallback(async (fn: () => Promise<any>) => {
    let lastError;
    for (let attempt = 0; attempt < 3; attempt++) {
      try {
        return await fn();
      } catch (error) {
        lastError = error;
        if (attempt < 2) {
          await new Promise(resolve => setTimeout(resolve, 1000 * Math.pow(2, attempt)));
        }
      }
    }
    throw lastError;
  }, []);

  useEffect(() => {
    loadBillingStatus();
    loadAvailablePlans();
  }, []);

  const loadBillingStatus = useCallback(async () => {
    try {
      const response = await executeWithRetry(() => billingApi.getBillingStatus());
      
      if (response.success && response.data) {
        setState(prev => ({ ...prev, status: response.data }));
        
        if (response.data.isOverQuota) {
          toast.error('Usage quota exceeded. Please upgrade your plan to continue.', {
            duration: 10000,
            action: {
              label: 'Upgrade Now',
              onClick: () => showUpgradeDialog()
            }
          });
        }
      }
    } catch (error) {
      console.error('Failed to load billing status:', error);
    }
  }, [executeWithRetry]);

  const loadAvailablePlans = useCallback(async () => {
    try {
      const response = await executeWithRetry(() => billingApi.getAvailablePlans());
      
      if (response.success && response.data) {
        setState(prev => ({ ...prev, plans: response.data || [] }));
      }
    } catch (error) {
      console.error('Failed to load billing plans:', error);
    }
  }, [executeWithRetry]);

  const showUpgradeDialog = useCallback(() => {
    if (state.plans.length > 0) {
      const planNames = state.plans.map(p => p.name).join(', ');
      toast.info(`Available plans: ${planNames}`, {
        duration: 5000,
        action: {
          label: 'View Plans',
          onClick: () => navigate('/billing/plans')
        }
      });
    }
  }, [state.plans]);

  const upgradePlan = useCallback(async (planId: string) => {
    setState(prev => ({ ...prev, upgrading: true, error: undefined }));

    try {
      const response = await executeWithRetry(() => billingApi.upgradePlan(planId));
      
      if (response.success) {
        toast.success('Plan upgraded successfully!');
        
        await loadBillingStatus();
        return true;
      }
      throw new Error(response.message || 'Failed to upgrade plan');
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      setState(prev => ({ ...prev, error: errorMessage }));
      toast.error(`Upgrade failed: ${errorMessage}`);
      return false;
    } finally {
      setState(prev => ({ ...prev, upgrading: false }));
    }
  }, [executeWithRetry, loadBillingStatus]);

  const handle402Response = useCallback(() => {
    toast.error('Payment required. Your usage has exceeded the current plan limits.', {
      duration: 10000,
      action: {
        label: 'Upgrade Plan',
        onClick: showUpgradeDialog
      }
    });
  }, [showUpgradeDialog]);

  return {
    state,
    loadBillingStatus,
    upgradePlan,
    handle402Response,
    showUpgradeDialog
  };
}
