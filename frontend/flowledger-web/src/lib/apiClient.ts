import axios from 'axios'
import { clearStoredAuth, getStoredAuth } from './authStorage'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080/api'

let unauthorizedHandler: (() => void) | null = null

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
})

apiClient.interceptors.request.use((config) => {
  const auth = getStoredAuth()
  if (auth?.accessToken) {
    config.headers.Authorization = `Bearer ${auth.accessToken}`
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      clearStoredAuth()
      unauthorizedHandler?.()
    }

    return Promise.reject(error)
  },
)

export function setUnauthorizedHandler(handler: (() => void) | null) {
  unauthorizedHandler = handler
}

export function getApiErrorMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string; title?: string; detail?: string } | undefined
    return data?.message ?? data?.detail ?? data?.title ?? fallback
  }

  return fallback
}
