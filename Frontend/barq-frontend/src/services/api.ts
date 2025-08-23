import axios from 'axios';

if (typeof window !== 'undefined') {
  const allowedHost = window.location.host;
  
  axios.interceptors.request.use((config) => {
    try {
      const url = new URL(config.url!, config.baseURL || window.location.origin);
      const isExternal = url.host !== allowedHost;
      if (isExternal) {
        console.warn('Blocked external request', url.href);
        return Promise.reject({ __cancelled: true, reason: 'blocked-external' });
      }
    } catch (e) {
      console.warn('Request URL parse issue, allowing request', e);
    }
    return config;
  }, (error) => Promise.reject(error));
}

export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

export interface Task {
  id: string;
  title: string;
  description: string;
  status: 'pending' | 'in-progress' | 'completed' | 'cancelled';
  priority: 'low' | 'medium' | 'high' | 'critical';
  assigneeId: string;
  projectId: string;
  dueDate: string;
  createdAt: string;
  updatedAt: string;
}

export interface TaskRequest {
  title: string;
  description: string;
  priority: 'low' | 'medium' | 'high' | 'critical';
  assigneeId?: string;
  projectId: string;
  dueDate: string;
  requiresApproval?: boolean;
}

export interface ApprovalRequest {
  id: string;
  taskId: string;
  requesterId: string;
  approverId: string;
  status: 'pending' | 'approved' | 'rejected';
  reason?: string;
  createdAt: string;
}

export interface AIRunRequest {
  taskId: string;
  prompt: string;
  provider?: string;
  model?: string;
}

export interface AIRunResult {
  id: string;
  taskId: string;
  status: 'running' | 'completed' | 'failed';
  result?: string;
  error?: string;
  cost?: number;
  duration?: number;
}

export interface FileAttachment {
  id: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  uploadedAt: string;
  scanStatus: 'pending' | 'clean' | 'infected' | 'quarantined';
  downloadUrl?: string;
}

export interface BillingStatus {
  currentUsage: number;
  planLimit: number;
  isOverQuota: boolean;
  upgradeRequired: boolean;
}

export const taskApi = {
  async createTask(request: TaskRequest): Promise<ApiResponse<Task>> {
    const response = await axios.post('/api/tasks', request);
    return response.data;
  },

  async getTasks(projectId?: string): Promise<ApiResponse<Task[]>> {
    const params = projectId ? { projectId } : {};
    const response = await axios.get('/api/tasks', { params });
    return response.data;
  },

  async updateTaskStatus(taskId: string, status: Task['status']): Promise<ApiResponse<Task>> {
    const response = await axios.patch(`/api/tasks/${taskId}/status`, { status });
    return response.data;
  },

  async deleteTask(taskId: string): Promise<ApiResponse> {
    const response = await axios.delete(`/api/tasks/${taskId}`);
    return response.data;
  }
};

export const approvalApi = {
  async requestApproval(taskId: string, approverId: string, reason?: string): Promise<ApiResponse<ApprovalRequest>> {
    const response = await axios.post('/api/approvals', { taskId, approverId, reason });
    return response.data;
  },

  async getPendingApprovals(): Promise<ApiResponse<ApprovalRequest[]>> {
    const response = await axios.get('/api/approvals/pending');
    return response.data;
  },

  async approveRequest(approvalId: string, reason?: string): Promise<ApiResponse<ApprovalRequest>> {
    const response = await axios.post(`/api/approvals/${approvalId}/approve`, { reason });
    return response.data;
  },

  async rejectRequest(approvalId: string, reason: string): Promise<ApiResponse<ApprovalRequest>> {
    const response = await axios.post(`/api/approvals/${approvalId}/reject`, { reason });
    return response.data;
  }
};

export const aiApi = {
  async runAI(request: AIRunRequest): Promise<ApiResponse<AIRunResult>> {
    const response = await axios.post('/api/ai/run', request);
    return response.data;
  },

  async getAIResult(runId: string): Promise<ApiResponse<AIRunResult>> {
    const response = await axios.get(`/api/ai/runs/${runId}`);
    return response.data;
  },

  async getAIRuns(taskId: string): Promise<ApiResponse<AIRunResult[]>> {
    const response = await axios.get(`/api/ai/runs`, { params: { taskId } });
    return response.data;
  }
};

export const fileApi = {
  async uploadFile(file: File, taskId?: string): Promise<ApiResponse<FileAttachment>> {
    const formData = new FormData();
    formData.append('file', file);
    if (taskId) formData.append('taskId', taskId);

    const response = await axios.post('/api/files/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    return response.data;
  },

  async getFileStatus(fileId: string): Promise<ApiResponse<FileAttachment>> {
    const response = await axios.get(`/api/files/${fileId}`);
    return response.data;
  },

  async downloadFile(fileId: string): Promise<string> {
    const response = await axios.get(`/api/files/${fileId}/download`);
    return response.data.downloadUrl;
  },

  async deleteFile(fileId: string): Promise<ApiResponse> {
    const response = await axios.delete(`/api/files/${fileId}`);
    return response.data;
  }
};

export const billingApi = {
  async getBillingStatus(): Promise<ApiResponse<BillingStatus>> {
    const response = await axios.get('/api/billing/status');
    return response.data;
  },

  async upgradePlan(planId: string): Promise<ApiResponse> {
    const response = await axios.post('/api/billing/upgrade', { planId });
    return response.data;
  },

  async getAvailablePlans(): Promise<ApiResponse<any[]>> {
    const response = await axios.get('/api/billing/plans');
    return response.data;
  }
};

export const slaApi = {
  async getSLAViolations(): Promise<ApiResponse<any[]>> {
    const response = await axios.get('/api/sla/violations');
    return response.data;
  },

  async acknowledgeViolation(violationId: string): Promise<ApiResponse> {
    const response = await axios.post(`/api/sla/violations/${violationId}/acknowledge`);
    return response.data;
  }
};

export const notificationApi = {
  async getNotifications(): Promise<ApiResponse<any[]>> {
    const response = await axios.get('/api/notifications');
    return response.data;
  },

  async markAsRead(notificationId: string): Promise<ApiResponse> {
    const response = await axios.post(`/api/notifications/${notificationId}/mark-read`);
    return response.data;
  },

  async sendNotification(notification: { title: string; type: string; message: string; priority?: 'low' | 'medium' | 'high'; metadata?: any }): Promise<ApiResponse> {
    const response = await axios.post('/api/notifications', notification);
    return response.data;
  }
};
