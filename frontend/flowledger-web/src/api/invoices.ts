import { apiClient } from '../lib/apiClient'
import { downloadCsvBlob, downloadFileBlob } from '../lib/downloadCsv'
import type { InvoiceDetail, InvoiceListItem, InvoiceStatus, PagedResult } from '../types'

export type InvoiceQuery = {
  status?: InvoiceStatus | ''
  customerId?: string
  search?: string
  fromDate?: string
  untilDate?: string
  minAmount?: string
  maxAmount?: string
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
  page?: number
  pageSize?: number
}

export async function getInvoices(query: InvoiceQuery) {
  const response = await apiClient.get<PagedResult<InvoiceListItem>>('/invoices', {
    params: normalizeQuery(query),
  })

  return response.data
}

export async function exportInvoices(query: Omit<InvoiceQuery, 'page' | 'pageSize'>) {
  const response = await apiClient.get<Blob>('/invoices/export', {
    params: normalizeQuery(query),
    responseType: 'blob',
  })
  downloadCsvBlob(response.data, 'invoices.csv', response.headers['content-disposition'])
}

export async function getInvoice(id: string) {
  const response = await apiClient.get<InvoiceDetail>(`/invoices/${id}`)
  return response.data
}

export async function downloadInvoicePdf(id: string, invoiceNumber: string) {
  const response = await apiClient.get<Blob>(`/invoices/${id}/pdf`, {
    responseType: 'blob',
  })
  downloadFileBlob(response.data, `${invoiceNumber}.pdf`, response.headers['content-disposition'])
}

export async function markInvoicePaid(id: string) {
  await apiClient.post(`/invoices/${id}/mark-paid`)
}

function normalizeQuery(query: InvoiceQuery) {
  return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''))
}
