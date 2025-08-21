import { useCallback } from 'react';

export interface UseAutoRetryOptions {
  maxRetries?: number;
  delay?: number;
  backoff?: boolean;
}

export function useAutoRetry(options: UseAutoRetryOptions = {}) {
  const { maxRetries = 3, delay = 1000, backoff = true } = options;

  const executeWithRetry = useCallback(async <T>(
    fn: () => Promise<T>,
    retryOptions?: UseAutoRetryOptions
  ): Promise<T> => {
    const opts = { ...options, ...retryOptions };
    const maxAttempts = opts.maxRetries || maxRetries;
    let attempt = 0;

    while (attempt < maxAttempts) {
      try {
        return await fn();
      } catch (error) {
        attempt++;
        
        if (attempt >= maxAttempts) {
          throw error;
        }

        const waitTime = backoff 
          ? (opts.delay || delay) * Math.pow(2, attempt - 1)
          : (opts.delay || delay);

        await new Promise(resolve => setTimeout(resolve, waitTime));
      }
    }

    throw new Error('Max retries exceeded');
  }, [maxRetries, delay, backoff]);

  return { executeWithRetry };
}
