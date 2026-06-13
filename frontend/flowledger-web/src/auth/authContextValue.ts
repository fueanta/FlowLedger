import { createContext } from 'react'
import type { User } from '../types'
import type { LoginFormValues } from './loginSchema'

export type AuthContextValue = {
  user: User | null
  token: string | null
  login: (values: LoginFormValues) => Promise<void>
  logout: () => void
  isAuthenticated: boolean
}

export const AuthContext = createContext<AuthContextValue | null>(null)
