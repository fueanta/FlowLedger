import { apiClient } from '../lib/apiClient'
import type { AuditLogListItem, PagedResult } from '../types'

export type AuditLogListParams = {
  page?: number
  pageSize?: number
  search?: string
  entityType?: string
  actionType?: string
  actor?: string
  fromDate?: string
  untilDate?: string
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export async function getAuditLogs(params: AuditLogListParams = {}) {
  const response = await apiClient.get<PagedResult<AuditLogListItem>>('/audit-logs', {
    params: Object.fromEntries(Object.entries(params).filter(([, value]) => value !== undefined && value !== '')),
  })
  return response.data
}
