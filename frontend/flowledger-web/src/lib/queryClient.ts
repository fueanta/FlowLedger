import { QueryClient } from '@tanstack/react-query'
import { getApiStatusCode } from './apiClient'

export function createFlowLedgerQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: shouldRetryRequest,
      },
      mutations: {
        retry: shouldRetryRequest,
      },
    },
  })
}

export function shouldRetryRequest(failureCount: number, error: unknown) {
  const status = getApiStatusCode(error)

  if (status !== undefined && status >= 400 && status < 500) {
    return false
  }

  return failureCount < 3
}
