import { createSlice, PayloadAction } from '@reduxjs/toolkit'

interface WorkflowInstance {
  id: string
  name: string
  status: 'running' | 'completed' | 'failed' | 'cancelled'
  progress: number
  startedAt: string
  completedAt?: string
  currentStep: string
}

interface WorkflowTask {
  id: string
  name: string
  status: 'pending' | 'in-progress' | 'completed' | 'failed'
  assignee?: string
  dueDate?: string
  priority: 'low' | 'medium' | 'high'
}

interface WorkflowState {
  instances: WorkflowInstance[]
  tasks: WorkflowTask[]
  selectedInstanceId: string | null
  isLoading: boolean
  error: string | null
}

const initialState: WorkflowState = {
  instances: [],
  tasks: [],
  selectedInstanceId: null,
  isLoading: false,
  error: null,
}

export const workflowSlice = createSlice({
  name: 'workflow',
  initialState,
  reducers: {
    setLoading: (state, action: PayloadAction<boolean>) => {
      state.isLoading = action.payload
    },
    setError: (state, action: PayloadAction<string | null>) => {
      state.error = action.payload
    },
    setInstances: (state, action: PayloadAction<WorkflowInstance[]>) => {
      state.instances = action.payload
    },
    addInstance: (state, action: PayloadAction<WorkflowInstance>) => {
      state.instances.push(action.payload)
    },
    updateInstance: (state, action: PayloadAction<Partial<WorkflowInstance> & { id: string }>) => {
      const index = state.instances.findIndex(i => i.id === action.payload.id)
      if (index !== -1) {
        state.instances[index] = { ...state.instances[index], ...action.payload }
      }
    },
    removeInstance: (state, action: PayloadAction<string>) => {
      state.instances = state.instances.filter(i => i.id !== action.payload)
    },
    setTasks: (state, action: PayloadAction<WorkflowTask[]>) => {
      state.tasks = action.payload
    },
    addTask: (state, action: PayloadAction<WorkflowTask>) => {
      state.tasks.push(action.payload)
    },
    updateTask: (state, action: PayloadAction<Partial<WorkflowTask> & { id: string }>) => {
      const index = state.tasks.findIndex(t => t.id === action.payload.id)
      if (index !== -1) {
        state.tasks[index] = { ...state.tasks[index], ...action.payload }
      }
    },
    removeTask: (state, action: PayloadAction<string>) => {
      state.tasks = state.tasks.filter(t => t.id !== action.payload)
    },
    setSelectedInstance: (state, action: PayloadAction<string | null>) => {
      state.selectedInstanceId = action.payload
    },
  },
})

export const {
  setLoading,
  setError,
  setInstances,
  addInstance,
  updateInstance,
  removeInstance,
  setTasks,
  addTask,
  updateTask,
  removeTask,
  setSelectedInstance,
} = workflowSlice.actions
