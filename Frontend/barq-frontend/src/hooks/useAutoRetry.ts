import { useState, useEffect, useCallback } from 'react';
import { toast } from 'sonner';

interface AutoRetryOptions {
  maxRetries?: number;
  retryDelay?: number;
  exponentialBackoff?: boolean;
  onRetry?: (attempt: number) => void;
  onMaxRetriesReached?: () => void;
}

export function useAutoRetry<T>(
  queryFn: () => Promise<T>,
  options: AutoRetryOptions = {}
) {
  const {
    maxRetries = 3,
    retryDelay = 1000,
    exponentialBackoff = true,
    onRetry,
    onMaxRetriesReached
  } = options;

  const [data, setData] = useState<T | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  const execute = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    const attemptQuery = async (attempt: number): Promise<T> => {
      try {
        const result = await queryFn();
        setData(result);
        setRetryCount(0);
        return result;
      } catch (err) {
        const error = err instanceof Error ? err : new Error('Unknown error');
        
        if (attempt < maxRetries) {
          const delay = exponentialBackoff 
            ? retryDelay * Math.pow(2, attempt)
            : retryDelay;
          
          setRetryCount(attempt + 1);
          
          if (onRetry) {
            onRetry(attempt + 1);
          }
          
          toast.info(`Retrying... (${attempt + 1}/${maxRetries})`);
          
          await new Promise(resolve => setTimeout(resolve, delay));
          return attemptQuery(attempt + 1);
        } else {
          setError(error);
          setRetryCount(maxRetries);
          
          if (onMaxRetriesReached) {
            onMaxRetriesReached();
          }
          
          toast.error(`Failed after ${maxRetries} attempts: ${error.message}`);
          throw error;
        }
      }
    };

    try {
      return await attemptQuery(0);
    } finally {
      setIsLoading(false);
    }
  }, [queryFn, maxRetries, retryDelay, exponentialBackoff, onRetry, onMaxRetriesReached]);

  const retry = useCallback(() => {
    execute();
  }, [execute]);

  return { data, isLoading, error, retryCount, execute, retry };
}
