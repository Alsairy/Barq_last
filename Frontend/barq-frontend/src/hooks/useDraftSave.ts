import { useState, useEffect, useCallback } from 'react';
import { toast } from 'sonner';

interface DraftSaveOptions {
  key: string;
  saveInterval?: number;
  onSave?: (data: any) => void;
  onRestore?: (data: any) => void;
}

export function useDraftSave<T>(
  initialData: T,
  options: DraftSaveOptions
) {
  const { key, saveInterval = 30000, onSave, onRestore } = options;
  const [data, setData] = useState<T>(initialData);
  const [lastSaved, setLastSaved] = useState<Date | null>(null);
  const [isDirty, setIsDirty] = useState(false);

  useEffect(() => {
    const savedDraft = localStorage.getItem(`draft_${key}`);
    if (savedDraft) {
      try {
        const parsedData = JSON.parse(savedDraft);
        setData(parsedData);
        setIsDirty(true);
        
        if (onRestore) {
          onRestore(parsedData);
        }
        
        toast.info('Draft restored from previous session');
      } catch (error) {
        console.error('Failed to restore draft:', error);
        localStorage.removeItem(`draft_${key}`);
      }
    }
  }, [key, onRestore]);

  useEffect(() => {
    if (!isDirty) return;

    const interval = setInterval(() => {
      saveDraft();
    }, saveInterval);

    return () => clearInterval(interval);
  }, [data, isDirty, saveInterval]);

  const saveDraft = useCallback(() => {
    try {
      localStorage.setItem(`draft_${key}`, JSON.stringify(data));
      setLastSaved(new Date());
      
      if (onSave) {
        onSave(data);
      }
    } catch (error) {
      console.error('Failed to save draft:', error);
      toast.error('Failed to save draft');
    }
  }, [data, key, onSave]);

  const updateData = useCallback((newData: T | ((prev: T) => T)) => {
    setData(prev => {
      const updated = typeof newData === 'function' ? (newData as (prev: T) => T)(prev) : newData;
      setIsDirty(true);
      return updated;
    });
  }, []);

  const clearDraft = useCallback(() => {
    localStorage.removeItem(`draft_${key}`);
    setIsDirty(false);
    setLastSaved(null);
  }, [key]);

  const discardDraft = useCallback(() => {
    clearDraft();
    setData(initialData);
    toast.success('Draft discarded');
  }, [clearDraft, initialData]);

  return {
    data,
    updateData,
    saveDraft,
    clearDraft,
    discardDraft,
    lastSaved,
    isDirty
  };
}
