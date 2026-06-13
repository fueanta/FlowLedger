import { apiClient } from '../lib/apiClient'
import type { SystemSettings } from '../types'

export type SettingsPayload = {
  vatPercentage: number
  managerApprovalThreshold: number
  invoiceDueDays: number
}

export async function getSettings() {
  const response = await apiClient.get<SystemSettings>('/settings')
  return response.data
}

export async function updateSettings(payload: SettingsPayload) {
  await apiClient.put('/settings', payload)
}
