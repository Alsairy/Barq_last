import React, { createContext, useContext, useEffect, ReactNode } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import { RootState } from '../store/store'
import { loginStart, loginSuccess, loginFailure, logout } from '../store/slices/authSlice'

interface AuthContextType {
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  isAuthenticated: boolean
  isLoading: boolean
  user: any
  error: string | null
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const dispatch = useDispatch()
  const { user, isAuthenticated, isLoading, error } = useSelector((state: RootState) => state.auth)

  const login = async (email: string, password: string) => {
    dispatch(loginStart())
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password }),
        credentials: 'include',
      })

      if (!response.ok) {
        throw new Error('Login failed')
      }

      const data = await response.json()
      dispatch(loginSuccess(data.user))
    } catch (error) {
      dispatch(loginFailure(error instanceof Error ? error.message : 'Login failed'))
    }
  }

  const handleLogout = () => {
    dispatch(logout())
    fetch('/api/auth/logout', {
      method: 'POST',
      credentials: 'include',
    }).catch(() => {
    })
  }

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const response = await fetch('/api/auth/me', {
          credentials: 'include',
        })
        if (response.ok) {
          const data = await response.json()
          dispatch(loginSuccess(data.user))
        }
      } catch (error) {
      }
    }

    checkAuth()
  }, [dispatch])

  const value: AuthContextType = {
    login,
    logout: handleLogout,
    isAuthenticated,
    isLoading,
    user,
    error,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
