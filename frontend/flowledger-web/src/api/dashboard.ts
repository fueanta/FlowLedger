import { apiClient } from '../lib/apiClient'
import type { DashboardSummary } from '../types'

export async function getDashboardSummary(periodMonths: number) {
  const response = await apiClient.get<DashboardSummary>('/dashboard/summary', {
    params: { periodMonths },
  })

  return response.data
}
