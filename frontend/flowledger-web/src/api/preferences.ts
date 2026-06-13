import { apiClient } from '../lib/apiClient'
import type { UserPreference } from '../types'

export async function getMyPreferences() {
  const response = await apiClient.get<UserPreference>('/preferences/me')
  return response.data
}

export async function updateMyPreferences(payload: UserPreference) {
  const response = await apiClient.put<UserPreference>('/preferences/me', payload)
  return response.data
}
