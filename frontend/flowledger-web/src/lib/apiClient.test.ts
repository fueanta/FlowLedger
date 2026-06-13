import MockAdapter from 'axios-mock-adapter'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiClient, setUnauthorizedHandler } from './apiClient'
import { getStoredAuth, setStoredAuth } from './authStorage'

describe('api client auth handling', () => {
  afterEach(() => {
    window.localStorage.clear()
    setUnauthorizedHandler(null)
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
      },
    })
    mock.onGet('/dashboard/summary').reply(401)

    await expect(apiClient.get('/dashboard/summary')).rejects.toBeTruthy()

    expect(getStoredAuth()).toBeNull()
    expect(handler).toHaveBeenCalledOnce()
  })
})
