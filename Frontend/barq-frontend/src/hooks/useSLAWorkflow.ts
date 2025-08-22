import { useState, useCallback, useEffect } from 'react';
import { slaApi } from '../services/api';
import { toast } from 'sonner';

export interface SLAViolation {
  id: string;
  taskId: string;
  taskTitle: string;
  violationType: 'overdue' | 'approaching' | 'critical';
  slaHours: number;
  actualHours: number;
  escalationLevel: number;
  acknowledged: boolean;
  createdAt: string;
}

export interface SLAWorkflowState {
  violations: SLAViolation[];
  loading: boolean;
  error?: string;
}

export function useSLAWorkflow() {
  const [state, setState] = useState<SLAWorkflowState>({
    violations: [],
    loading: false
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
    loadViolations();
    
    const interval = setInterval(loadViolations, 30000);
    return () => clearInterval(interval);
  }, []);

  const loadViolations = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true }));

    try {
      const response = await executeWithRetry(() => slaApi.getSLAViolations());
      
      if (response.success && response.data) {
        const violations = response.data as SLAViolation[];
        setState(prev => ({ ...prev, violations, loading: false }));
        
        const newCriticalViolations = violations.filter(v => 
          v.violationType === 'critical' && !v.acknowledged
        );

        if (newCriticalViolations.length > 0) {
          newCriticalViolations.forEach(violation => {
            toast.error(`Critical SLA violation: ${violation.taskTitle}`, {
              duration: 15000,
              action: {
                label: 'Acknowledge',
                onClick: () => acknowledgeViolation(violation.id)
              }
            });
          });
        }

        const newOverdueViolations = violations.filter(v => 
          v.violationType === 'overdue' && !v.acknowledged
        );

        if (newOverdueViolations.length > 0) {
          newOverdueViolations.forEach(violation => {
            toast.warning(`Task overdue: ${violation.taskTitle}`, {
              duration: 10000,
              action: {
                label: 'View Task',
                onClick: () => console.log('Navigate to task:', violation.taskId)
              }
            });
          });
        }
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      setState(prev => ({ 
        ...prev, 
        loading: false, 
        error: errorMessage 
      }));
      console.error('Failed to load SLA violations:', error);
    }
  }, [executeWithRetry]);

  const acknowledgeViolation = useCallback(async (violationId: string) => {
    try {
      const response = await executeWithRetry(() => slaApi.acknowledgeViolation(violationId));
      
      if (response.success) {
        setState(prev => ({
          ...prev,
          violations: prev.violations.map(v => 
            v.id === violationId ? { ...v, acknowledged: true } : v
          )
        }));
        toast.success('SLA violation acknowledged');
        return true;
      }
      throw new Error(response.message || 'Failed to acknowledge violation');
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to acknowledge violation: ${errorMessage}`);
      return false;
    }
  }, [executeWithRetry]);

  const getViolationsByType = useCallback((type: SLAViolation['violationType']) => {
    return state.violations.filter(v => v.violationType === type);
  }, [state.violations]);

  const getUnacknowledgedCount = useCallback(() => {
    return state.violations.filter(v => !v.acknowledged).length;
  }, [state.violations]);

  return {
    state,
    loadViolations,
    acknowledgeViolation,
    getViolationsByType,
    getUnacknowledgedCount
  };
}
