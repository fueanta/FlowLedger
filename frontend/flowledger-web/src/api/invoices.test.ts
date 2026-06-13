import MockAdapter from 'axios-mock-adapter'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiClient } from '../lib/apiClient'
import { exportInvoices, getInvoices } from './invoices'

const downloadCsvBlob = vi.fn()

vi.mock('../lib/downloadCsv', () => ({
  downloadCsvBlob: (...args: unknown[]) => downloadCsvBlob(...args),
  downloadFileBlob: vi.fn(),
}))

describe('invoices api', () => {
  afterEach(() => {
    downloadCsvBlob.mockReset()
  })

  it('sends date and amount filters to invoice list endpoint', async () => {
    const mock = new MockAdapter(apiClient)
    mock.onGet('/invoices').reply((config) => {
      expect(config.params).toMatchObject({
        search: 'INV-2026',
        fromDate: '2026-06-01',
        untilDate: '2026-06-30',
        minAmount: '1000',
        maxAmount: '5000',
      })
      return [200, { items: [], page: 1, pageSize: 10, totalCount: 0 }]
    })

    await getInvoices({
      search: 'INV-2026',
      fromDate: '2026-06-01',
      untilDate: '2026-06-30',
      minAmount: '1000',
      maxAmount: '5000',
      page: 1,
      pageSize: 10,
    })

    mock.restore()
  })

  it('sends date and amount filters to invoice csv export', async () => {
    const mock = new MockAdapter(apiClient)
    mock.onGet('/invoices/export').reply((config) => {
      expect(config.params).toMatchObject({
        search: 'INV-2026',
        fromDate: '2026-06-01',
        untilDate: '2026-06-30',
        minAmount: '1000',
        maxAmount: '5000',
      })

      return [
        200,
        new Blob(['invoiceNumber']),
        { 'content-disposition': 'attachment; filename="invoices.csv"' },
      ]
    })

    await exportInvoices({
      search: 'INV-2026',
      fromDate: '2026-06-01',
      untilDate: '2026-06-30',
      minAmount: '1000',
      maxAmount: '5000',
    })

    expect(downloadCsvBlob).toHaveBeenCalledOnce()
    mock.restore()
  })
})
