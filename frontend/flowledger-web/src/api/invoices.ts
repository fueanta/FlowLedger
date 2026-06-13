import { apiClient } from '../lib/apiClient'
import type { InvoiceDetail, InvoiceListItem, InvoiceStatus, PagedResult } from '../types'

export type InvoiceQuery = {
  status?: InvoiceStatus | ''
  customerId?: string
  search?: string
  page?: number
  pageSize?: number
}

export async function getInvoices(query: InvoiceQuery) {
  const response = await apiClient.get<PagedResult<InvoiceListItem>>('/invoices', {
    params: Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== '')),
  })

  return response.data
}

export async function getInvoice(id: string) {
  const response = await apiClient.get<InvoiceDetail>(`/invoices/${id}`)
  return response.data
}

export async function markInvoicePaid(id: string) {
  await apiClient.post(`/invoices/${id}/mark-paid`)
}
