import { useState, useCallback } from 'react';
import { fileApi, notificationApi, FileAttachment } from '../services/api';
import { toast } from 'sonner';

export interface FileWorkflowState {
  uploading: boolean;
  scanning: boolean;
  files: FileAttachment[];
  error?: string;
}

export function useFileWorkflow() {
  const [state, setState] = useState<FileWorkflowState>({
    uploading: false,
    scanning: false,
    files: []
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

  const uploadAndScanFile = useCallback(async (file: File, taskId?: string) => {
    setState(prev => ({ ...prev, uploading: true, error: undefined }));

    try {
      const uploadResponse = await executeWithRetry(() => fileApi.uploadFile(file, taskId));
      
      if (!uploadResponse.success || !uploadResponse.data) {
        throw new Error(uploadResponse.message || 'Failed to upload file');
      }

      const uploadedFile = uploadResponse.data;
      setState(prev => ({ 
        ...prev, 
        files: [...prev.files, uploadedFile],
        uploading: false,
        scanning: true
      }));

      toast.success('File uploaded successfully, scanning for viruses...');

      let attempts = 0;
      const maxAttempts = 60; // 60 seconds max for scan

      const pollScanStatus = async (): Promise<void> => {
        if (attempts >= maxAttempts) {
          throw new Error('File scan timeout');
        }

        const statusResponse = await fileApi.getFileStatus(uploadedFile.id);
        
        if (statusResponse.success && statusResponse.data) {
          const fileStatus = statusResponse.data;
          
          setState(prev => ({
            ...prev,
            files: prev.files.map(f => 
              f.id === fileStatus.id ? fileStatus : f
            )
          }));

          if (fileStatus.scanStatus === 'clean') {
            setState(prev => ({ ...prev, scanning: false }));
            toast.success('File scan completed - file is clean');
            
            await notifyFileProcessed(fileStatus, 'clean');
            return;
          } else if (fileStatus.scanStatus === 'infected') {
            setState(prev => ({ ...prev, scanning: false }));
            toast.error('File scan detected malware - file quarantined');
            
            await notifyFileProcessed(fileStatus, 'infected');
            return;
          } else if (fileStatus.scanStatus === 'quarantined') {
            setState(prev => ({ ...prev, scanning: false }));
            toast.warning('File has been quarantined pending review');
            
            await notifyFileProcessed(fileStatus, 'quarantined');
            return;
          }
        }

        attempts++;
        setTimeout(pollScanStatus, 1000); // Poll every second
      };

      await pollScanStatus();
      return uploadedFile;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      setState(prev => ({ 
        ...prev, 
        uploading: false, 
        scanning: false,
        error: errorMessage 
      }));
      toast.error(`File processing failed: ${errorMessage}`);
      throw error;
    }
  }, [executeWithRetry]);

  const notifyFileProcessed = async (file: FileAttachment, status: 'clean' | 'infected' | 'quarantined') => {
    try {
      const message = {
        clean: `File "${file.fileName}" has been scanned and is safe to use`,
        infected: `File "${file.fileName}" contains malware and has been quarantined`,
        quarantined: `File "${file.fileName}" has been quarantined for manual review`
      }[status];

      console.log('File notification:', { file, status, message });
    } catch (error) {
      console.error('Failed to send file notification:', error);
    }
  };

  const downloadFile = useCallback(async (fileId: string) => {
    try {
      const downloadUrl = await executeWithRetry(() => fileApi.downloadFile(fileId));
      
      window.open(downloadUrl, '_blank');
      toast.success('Download started');
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Download failed: ${errorMessage}`);
    }
  }, [executeWithRetry]);

  const deleteFile = useCallback(async (fileId: string) => {
    try {
      const response = await executeWithRetry(() => fileApi.deleteFile(fileId));
      
      if (response.success) {
        setState(prev => ({
          ...prev,
          files: prev.files.filter(f => f.id !== fileId)
        }));
        toast.success('File deleted successfully');
        return true;
      }
      throw new Error(response.message || 'Failed to delete file');
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to delete file: ${errorMessage}`);
      return false;
    }
  }, [executeWithRetry]);

  return {
    state,
    uploadAndScanFile,
    downloadFile,
    deleteFile
  };
}
