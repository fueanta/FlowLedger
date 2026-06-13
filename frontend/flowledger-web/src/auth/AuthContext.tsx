import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { apiClient, getApiErrorMessage, setUnauthorizedHandler } from '../lib/apiClient'
import { clearStoredAuth, getStoredAuth, setStoredAuth } from '../lib/authStorage'
import type { LoginResponse } from '../types'
import { AuthContext, type AuthContextValue } from './authContextValue'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<LoginResponse | null>(() => getStoredAuth())
  const navigate = useNavigate()
  const location = useLocation()

  useEffect(() => {
    setUnauthorizedHandler(() => {
      setAuth(null)
      toast.error('Session expired. Please log in again.')
      navigate('/login', { replace: true, state: { expired: true, from: location.pathname } })
    })

    return () => setUnauthorizedHandler(null)
  }, [location.pathname, navigate])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: auth?.user ?? null,
      token: auth?.accessToken ?? null,
      isAuthenticated: Boolean(auth?.accessToken),
      login: async (values) => {
        try {
          const response = await apiClient.post<LoginResponse>('/auth/login', values)
          setStoredAuth(response.data)
          setAuth(response.data)
          toast.success(`Signed in as ${response.data.user.fullName}.`)
        } catch (error) {
          throw new Error(getApiErrorMessage(error, 'Login failed. Check the email and password.'), { cause: error })
        }
      },
      logout: () => {
        clearStoredAuth()
        setAuth(null)
        navigate('/login', { replace: true })
      },
    }),
    [auth, navigate],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
