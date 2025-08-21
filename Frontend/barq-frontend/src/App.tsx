import React, { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Provider } from 'react-redux';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from 'next-themes';
import { Toaster } from 'sonner';
import { store } from './store/store';
import { ThreePanelLayout } from './components/layout/ThreePanelLayout';
import { AuthProvider } from './contexts/AuthContext';
import { useBillingWorkflow } from './hooks/useBillingWorkflow';
import axios from 'axios';
import './index.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

function AppContent() {
  const { handle402Response } = useBillingWorkflow();

  useEffect(() => {
    const interceptor = axios.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 402) {
          handle402Response();
        }
        return Promise.reject(error);
      }
    );

    return () => {
      axios.interceptors.response.eject(interceptor);
    };
  }, [handle402Response]);

  return (
    <Router>
      <div className="min-h-screen bg-background">
        <Routes>
          <Route path="/*" element={<ThreePanelLayout />} />
        </Routes>
        <Toaster position="top-right" />
      </div>
    </Router>
  );
}

function App() {
  return (
    <Provider store={store}>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
          <AuthProvider>
            <AppContent />
          </AuthProvider>
        </ThemeProvider>
      </QueryClientProvider>
    </Provider>
  );
}

export default App;
