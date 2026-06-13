import { AxiosError, AxiosHeaders } from 'axios'
import { describe, expect, it } from 'vitest'
import { shouldRetryRequest } from './queryClient'

describe('query client retry policy', () => {
  it('does not retry 4xx API responses', () => {
    expect(shouldRetryRequest(0, axiosError(400))).toBe(false)
    expect(shouldRetryRequest(0, axiosError(403))).toBe(false)
    expect(shouldRetryRequest(0, axiosError(404))).toBe(false)
  })

  it('allows limited retries for server or network errors', () => {
    expect(shouldRetryRequest(0, axiosError(500))).toBe(true)
    expect(shouldRetryRequest(2, new Error('Network error'))).toBe(true)
    expect(shouldRetryRequest(3, new Error('Network error'))).toBe(false)
  })
})

function axiosError(status: number) {
  return new AxiosError('Request failed', undefined, undefined, undefined, {
    data: {},
    status,
    statusText: String(status),
    headers: {},
    config: { headers: new AxiosHeaders() },
  })
}
