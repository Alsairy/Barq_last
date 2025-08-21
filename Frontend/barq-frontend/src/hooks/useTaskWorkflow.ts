import { useState, useCallback } from 'react';
import { taskApi, approvalApi, aiApi, TaskRequest, Task, AIRunRequest } from '../services/api';
import { useOptimisticUpdate } from './useOptimisticUpdate';
import { useAutoRetry } from './useAutoRetry';
import { toast } from 'sonner';

export interface TaskWorkflowState {
  task?: Task;
  approvalStatus: 'none' | 'pending' | 'approved' | 'rejected';
  aiRunning: boolean;
  aiResult?: string;
  isLoading: boolean;
  error?: string;
}

export function useTaskWorkflow(taskId?: string) {
  const [state, setState] = useState<TaskWorkflowState>({
    approvalStatus: 'none',
    aiRunning: false,
    isLoading: false
  });

  const { executeWithOptimism } = useOptimisticUpdate();
  const { executeWithRetry } = useAutoRetry();

  const createTaskWithApproval = useCallback(async (request: TaskRequest & { approverId?: string }) => {
    setState(prev => ({ ...prev, isLoading: true, error: undefined }));

    try {
      const taskResponse = await executeWithRetry(() => taskApi.createTask(request));
      
      if (!taskResponse.success || !taskResponse.data) {
        throw new Error(taskResponse.message || 'Failed to create task');
      }

      const newTask = taskResponse.data;
      setState(prev => ({ ...prev, task: newTask }));

      if (request.requiresApproval && request.approverId) {
        setState(prev => ({ ...prev, approvalStatus: 'pending' }));
        
        const approvalResponse = await executeWithRetry(() => 
          approvalApi.requestApproval(newTask.id, request.approverId!, 'Task requires approval before execution')
        );

        if (!approvalResponse.success) {
          throw new Error('Failed to request approval');
        }

        toast.success('Task created and sent for approval');
      } else {
        setState(prev => ({ ...prev, approvalStatus: 'approved' }));
        toast.success('Task created successfully');
      }

      return newTask;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      setState(prev => ({ ...prev, error: errorMessage }));
      toast.error(`Failed to create task: ${errorMessage}`);
      throw error;
    } finally {
      setState(prev => ({ ...prev, isLoading: false }));
    }
  }, [executeWithRetry]);

  const approveTask = useCallback(async (approvalId: string) => {
    try {
      const response = await executeWithRetry(() => approvalApi.approveRequest(approvalId));
      
      if (response.success) {
        setState(prev => ({ ...prev, approvalStatus: 'approved' }));
        toast.success('Task approved successfully');
        return true;
      }
      throw new Error(response.message || 'Failed to approve task');
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to approve task: ${errorMessage}`);
      return false;
    }
  }, [executeWithRetry]);

  const runAI = useCallback(async (prompt: string, provider?: string) => {
    if (!state.task) {
      toast.error('No task available for AI processing');
      return;
    }

    setState(prev => ({ ...prev, aiRunning: true, error: undefined }));

    try {
      const aiRequest: AIRunRequest = {
        taskId: state.task.id,
        prompt,
        provider
      };

      const response = await executeWithRetry(() => aiApi.runAI(aiRequest));
      
      if (!response.success || !response.data) {
        throw new Error(response.message || 'Failed to start AI processing');
      }

      const runId = response.data.id;
      let attempts = 0;
      const maxAttempts = 30; // 30 seconds max

      const pollResult = async (): Promise<void> => {
        if (attempts >= maxAttempts) {
          throw new Error('AI processing timeout');
        }

        const resultResponse = await aiApi.getAIResult(runId);
        
        if (resultResponse.success && resultResponse.data) {
          const result = resultResponse.data;
          
          if (result.status === 'completed') {
            setState(prev => ({ 
              ...prev, 
              aiRunning: false, 
              aiResult: result.result 
            }));
            toast.success('AI processing completed');
            return;
          } else if (result.status === 'failed') {
            throw new Error(result.error || 'AI processing failed');
          }
        }

        attempts++;
        setTimeout(pollResult, 1000); // Poll every second
      };

      await pollResult();
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      setState(prev => ({ 
        ...prev, 
        aiRunning: false, 
        error: errorMessage 
      }));
      toast.error(`AI processing failed: ${errorMessage}`);
    }
  }, [state.task, executeWithRetry]);

  const completeTask = useCallback(async () => {
    if (!state.task) {
      toast.error('No task to complete');
      return;
    }

    try {
      const response = await executeWithOptimism(
        () => taskApi.updateTaskStatus(state.task!.id, 'completed'),
        (currentTask: any) => ({ ...currentTask, status: 'completed' as const }),
        state.task
      );

      if (response.success) {
        setState(prev => ({ 
          ...prev, 
          task: { ...prev.task!, status: 'completed' }
        }));
        toast.success('Task completed successfully');
        return true;
      }
      throw new Error(response.message || 'Failed to complete task');
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to complete task: ${errorMessage}`);
      return false;
    }
  }, [state.task, executeWithOptimism]);

  return {
    state,
    createTaskWithApproval,
    approveTask,
    runAI,
    completeTask
  };
}
