import { configureStore } from '@reduxjs/toolkit'
import { authSlice } from './slices/authSlice'
import { uiSlice } from './slices/uiSlice'
import { workflowSlice } from './slices/workflowSlice'

export const store = configureStore({
  reducer: {
    auth: authSlice.reducer,
    ui: uiSlice.reducer,
    workflow: workflowSlice.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        ignoredActions: ['persist/PERSIST', 'persist/REHYDRATE'],
      },
    }),
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
