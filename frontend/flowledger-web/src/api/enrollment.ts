import { apiClient } from '../lib/apiClient'
import type { EnrollmentRequest, EnrollmentRequestStatus, PagedResult, Role } from '../types'

export type RegisterEnrollmentPayload = {
  fullName: string
  email: string
  password: string
  requestedRole: Role
}

export type EnrollmentListParams = {
  page?: number
  pageSize?: number
  search?: string
  status?: EnrollmentRequestStatus | ''
  requestedRole?: Role | ''
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export async function registerEnrollment(payload: RegisterEnrollmentPayload) {
  const response = await apiClient.post<{ id: string }>('/enrollment-requests', payload)
  return response.data.id
}

export async function getEnrollmentRequests(params: EnrollmentListParams = {}) {
  const response = await apiClient.get<PagedResult<EnrollmentRequest>>('/enrollment-requests', {
    params: {
      ...params,
      status: params.status || undefined,
      requestedRole: params.requestedRole || undefined,
    },
  })
  return response.data
}

export async function approveEnrollmentRequest(id: string, assignedRole: Role) {
  await apiClient.post(`/enrollment-requests/${id}/approve`, { assignedRole })
}

export async function rejectEnrollmentRequest(id: string, reason: string) {
  await apiClient.post(`/enrollment-requests/${id}/reject`, { reason })
}
