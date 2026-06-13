import axios from 'axios'
import { clearStoredAuth, getStoredAuth } from './authStorage'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080/api'
let minimumGetDelayMs = import.meta.env.MODE === 'test' ? 0 : 1_000

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

  if (config.method?.toLowerCase() === 'get') {
    config.headers['x-flowledger-request-started-at'] = `${Date.now()}`
  }

  return config
})

apiClient.interceptors.response.use(
  async (response) => {
    await waitForMinimumGetDelay(response.config)
    return response
  },
  async (error) => {
    await waitForMinimumGetDelay(error.config)

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

export function setMinimumGetDelayForTesting(delayMs: number) {
  minimumGetDelayMs = delayMs
}

export function getApiErrorMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string; title?: string; detail?: string } | undefined
    return data?.message ?? data?.detail ?? data?.title ?? fallback
  }

  return fallback
}

async function waitForMinimumGetDelay(config: { method?: string; headers?: Record<string, string> } | undefined) {
  if (config?.method?.toLowerCase() !== 'get') {
    return
  }

  const startedAtHeader = config.headers?.['x-flowledger-request-started-at']
  const startedAt = startedAtHeader ? Number(startedAtHeader) : Date.now()
  const elapsed = Date.now() - startedAt
  const remaining = minimumGetDelayMs - elapsed

  if (remaining > 0) {
    await new Promise((resolve) => window.setTimeout(resolve, remaining))
  }
}
