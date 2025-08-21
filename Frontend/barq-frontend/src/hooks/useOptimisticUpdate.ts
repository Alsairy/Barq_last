import { useCallback } from 'react';

export function useOptimisticUpdate() {
  const executeWithOptimism = useCallback(async <T, U>(
    asyncFn: () => Promise<T>,
    optimisticUpdate: (current: U) => U,
    currentValue: U,
    onSuccess?: (result: T, optimisticValue: U) => void,
    onError?: (error: any, originalValue: U) => void
  ): Promise<T> => {
    const optimisticValue = optimisticUpdate(currentValue);
    
    try {
      const result = await asyncFn();
      
      if (onSuccess) {
        onSuccess(result, optimisticValue);
      }
      
      return result;
    } catch (error) {
      if (onError) {
        onError(error, currentValue);
      }
      
      throw error;
    }
  }, []);

  return { executeWithOptimism };
}
