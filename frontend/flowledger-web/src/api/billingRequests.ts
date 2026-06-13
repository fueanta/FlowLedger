import { apiClient } from '../lib/apiClient'
import { downloadCsvBlob } from '../lib/downloadCsv'
import type { BillingRequestDetail, BillingRequestListItem, BillingRequestStatus, PagedResult, WorkflowQueue } from '../types'

export type BillingRequestQuery = {
  status?: BillingRequestStatus | ''
  customerId?: string
  queue?: WorkflowQueue | ''
  search?: string
  assignedToMe?: boolean
  createdByMe?: boolean
  fromDate?: string
  untilDate?: string
  minAmount?: string
  maxAmount?: string
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
  page?: number
  pageSize?: number
}

export type BillingRequestPayload = {
  customerId: string
  title: string
  description: string
  lineItems: { description: string; quantity: number; unitPrice: number }[]
}

export async function getBillingRequests(query: BillingRequestQuery) {
  const response = await apiClient.get<PagedResult<BillingRequestListItem>>('/billing-requests', {
    params: normalizeQuery(query),
  })

  return response.data
}

export async function exportBillingRequests(query: Omit<BillingRequestQuery, 'page' | 'pageSize'>) {
  const response = await apiClient.get<Blob>('/billing-requests/export', {
    params: normalizeQuery(query),
    responseType: 'blob',
  })
  downloadCsvBlob(response.data, 'billing-requests.csv', response.headers['content-disposition'])
}

export async function getWorkQueue(query: Pick<BillingRequestQuery, 'queue' | 'search' | 'sortBy' | 'sortDirection' | 'page' | 'pageSize'>) {
  const response = await apiClient.get<PagedResult<BillingRequestListItem>>('/work-queue', {
    params: normalizeQuery(query),
  })

  return response.data
}

export async function getBillingRequest(id: string) {
  const response = await apiClient.get<BillingRequestDetail>(`/billing-requests/${id}`)
  return response.data
}

export async function createBillingRequest(payload: BillingRequestPayload) {
  const response = await apiClient.post<{ id: string }>('/billing-requests', payload)
  return response.data.id
}

export async function updateBillingRequest(id: string, payload: BillingRequestPayload) {
  await apiClient.put(`/billing-requests/${id}`, payload)
}

export async function submitBillingRequest(id: string) {
  await apiClient.post(`/billing-requests/${id}/submit`)
}

export async function approveBillingRequest(id: string, comment?: string) {
  await apiClient.post(`/billing-requests/${id}/approve`, { comment: comment?.trim() || null })
}

export async function rejectBillingRequest(id: string, reason: string) {
  await apiClient.post(`/billing-requests/${id}/reject`, { reason })
}

export async function addBillingRequestComment(id: string, body: string) {
  await apiClient.post(`/billing-requests/${id}/comments`, { body })
}

function normalizeQuery(query: BillingRequestQuery) {
  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''),
  )
}
