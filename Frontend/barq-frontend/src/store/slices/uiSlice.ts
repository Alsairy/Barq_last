import { createSlice, PayloadAction } from '@reduxjs/toolkit'

interface UIState {
  leftPanelCollapsed: boolean
  rightPanelCollapsed: boolean
  theme: 'light' | 'dark' | 'system'
  sidebarWidth: number
  notifications: Array<{
    id: string
    type: 'success' | 'error' | 'warning' | 'info'
    title: string
    message: string
    timestamp: number
  }>
}

const initialState: UIState = {
  leftPanelCollapsed: false,
  rightPanelCollapsed: false,
  theme: 'system',
  sidebarWidth: 280,
  notifications: [],
}

export const uiSlice = createSlice({
  name: 'ui',
  initialState,
  reducers: {
    toggleLeftPanel: (state) => {
      state.leftPanelCollapsed = !state.leftPanelCollapsed
    },
    toggleRightPanel: (state) => {
      state.rightPanelCollapsed = !state.rightPanelCollapsed
    },
    setLeftPanelCollapsed: (state, action: PayloadAction<boolean>) => {
      state.leftPanelCollapsed = action.payload
    },
    setRightPanelCollapsed: (state, action: PayloadAction<boolean>) => {
      state.rightPanelCollapsed = action.payload
    },
    setTheme: (state, action: PayloadAction<'light' | 'dark' | 'system'>) => {
      state.theme = action.payload
    },
    setSidebarWidth: (state, action: PayloadAction<number>) => {
      state.sidebarWidth = action.payload
    },
    addNotification: (state, action: PayloadAction<Omit<UIState['notifications'][0], 'id' | 'timestamp'>>) => {
      const notification = {
        ...action.payload,
        id: Math.random().toString(36).substr(2, 9),
        timestamp: Date.now(),
      }
      state.notifications.push(notification)
    },
    removeNotification: (state, action: PayloadAction<string>) => {
      state.notifications = state.notifications.filter(n => n.id !== action.payload)
    },
    clearNotifications: (state) => {
      state.notifications = []
    },
  },
})

export const {
  toggleLeftPanel,
  toggleRightPanel,
  setLeftPanelCollapsed,
  setRightPanelCollapsed,
  setTheme,
  setSidebarWidth,
  addNotification,
  removeNotification,
  clearNotifications,
} = uiSlice.actions
