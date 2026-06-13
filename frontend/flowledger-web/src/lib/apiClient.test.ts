import MockAdapter from 'axios-mock-adapter'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiClient, setMinimumGetDelayForTesting, setUnauthorizedHandler } from './apiClient'
import { getStoredAuth, setStoredAuth } from './authStorage'

describe('api client auth handling', () => {
  afterEach(() => {
    window.localStorage.clear()
    setUnauthorizedHandler(null)
    setMinimumGetDelayForTesting(0)
    vi.useRealTimers()
  })

  it('clears stored auth and calls handler on 401 responses', async () => {
    const mock = new MockAdapter(apiClient)
    const handler = vi.fn()
    setUnauthorizedHandler(handler)
    setStoredAuth({
      accessToken: 'expired-token',
      user: {
        id: '11111111-1111-1111-1111-111111111111',
        fullName: 'Sarah Sales',
        email: 'sales@flowledger.local',
        role: 'Sales',
        status: 'Active',
        isActive: true,
        createdAtUtc: '2026-01-05T09:00:00Z',
        updatedAtUtc: '2026-01-05T09:00:00Z',
        lastLoginAtUtc: null,
      },
    })
    mock.onGet('/dashboard/summary').reply(401)

    await expect(apiClient.get('/dashboard/summary')).rejects.toBeTruthy()

    expect(getStoredAuth()).toBeNull()
    expect(handler).toHaveBeenCalledOnce()
  })

  it('holds get responses for at least one second so loading state stays visible briefly', async () => {
    vi.useFakeTimers()
    setMinimumGetDelayForTesting(1_000)

    const mock = new MockAdapter(apiClient)
    mock.onGet('/dashboard/summary').reply(200, { totalRequests: 4 })

    let resolved = false
    const request = apiClient.get('/dashboard/summary').then(() => {
      resolved = true
    })

    await vi.advanceTimersByTimeAsync(999)
    expect(resolved).toBe(false)

    await vi.advanceTimersByTimeAsync(1)
    await request
    expect(resolved).toBe(true)
  })
})
