import { useState, useCallback } from 'react';
import { toast } from 'sonner';

interface OptimisticUpdateOptions<T> {
  onSuccess?: (data: T) => void;
  onError?: (error: Error) => void;
  successMessage?: string;
  errorMessage?: string;
}

export function useOptimisticUpdate<T, P = any>(
  updateFn: (params: P) => Promise<T>,
  options: OptimisticUpdateOptions<T> = {}
) {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const execute = useCallback(
    async (params: P, optimisticUpdate?: () => void, rollback?: () => void) => {
      setIsLoading(true);
      setError(null);

      if (optimisticUpdate) {
        optimisticUpdate();
      }

      try {
        const result = await updateFn(params);
        
        if (options.successMessage) {
          toast.success(options.successMessage);
        }
        
        if (options.onSuccess) {
          options.onSuccess(result);
        }
        
        return result;
      } catch (err) {
        const error = err instanceof Error ? err : new Error('Unknown error');
        setError(error);
        
        if (rollback) {
          rollback();
        }
        
        const errorMessage = options.errorMessage || error.message;
        toast.error(errorMessage);
        
        if (options.onError) {
          options.onError(error);
        }
        
        throw error;
      } finally {
        setIsLoading(false);
      }
    },
    [updateFn, options]
  );

  return { execute, isLoading, error };
}
