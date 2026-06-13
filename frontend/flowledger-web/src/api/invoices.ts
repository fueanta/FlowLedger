import { apiClient } from '../lib/apiClient'
import { downloadCsvBlob } from '../lib/downloadCsv'
import type { InvoiceDetail, InvoiceListItem, InvoiceStatus, PagedResult } from '../types'

export type InvoiceQuery = {
  status?: InvoiceStatus | ''
  customerId?: string
  search?: string
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
  page?: number
  pageSize?: number
}

export async function getInvoices(query: InvoiceQuery) {
  const response = await apiClient.get<PagedResult<InvoiceListItem>>('/invoices', {
    params: Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== '')),
  })

  return response.data
}

export async function exportInvoices(query: Omit<InvoiceQuery, 'page' | 'pageSize'>) {
  const response = await apiClient.get<Blob>('/invoices/export', {
    params: Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== '')),
    responseType: 'blob',
  })
  downloadCsvBlob(response.data, 'invoices.csv', response.headers['content-disposition'])
}

export async function getInvoice(id: string) {
  const response = await apiClient.get<InvoiceDetail>(`/invoices/${id}`)
  return response.data
}

export async function markInvoicePaid(id: string) {
  await apiClient.post(`/invoices/${id}/mark-paid`)
}
