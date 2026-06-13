import MockAdapter from 'axios-mock-adapter'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiClient } from '../lib/apiClient'
import { exportBillingRequests, getBillingRequests } from './billingRequests'

const downloadCsvBlob = vi.fn()

vi.mock('../lib/downloadCsv', () => ({
  downloadCsvBlob: (...args: unknown[]) => downloadCsvBlob(...args),
}))

describe('billingRequests api', () => {
  afterEach(() => {
    downloadCsvBlob.mockReset()
  })

  it('sends date and amount filters to billing request list endpoint', async () => {
    const mock = new MockAdapter(apiClient)
    mock.onGet('/billing-requests').reply((config) => {
      expect(config.params).toMatchObject({
        search: 'Fiber',
        fromDate: '2026-06-01',
        untilDate: '2026-06-30',
        minAmount: '1000',
        maxAmount: '5000',
      })
      return [200, { items: [], page: 1, pageSize: 10, totalCount: 0 }]
    })

    await getBillingRequests({
      search: 'Fiber',
      fromDate: '2026-06-01',
      untilDate: '2026-06-30',
      minAmount: '1000',
      maxAmount: '5000',
      page: 1,
      pageSize: 10,
    })

    mock.restore()
  })

  it('sends date and amount filters to billing request csv export', async () => {
    const mock = new MockAdapter(apiClient)
    mock.onGet('/billing-requests/export').reply((config) => {
      expect(config.params).toMatchObject({
        search: 'Fiber',
        fromDate: '2026-06-01',
        untilDate: '2026-06-30',
        minAmount: '1000',
        maxAmount: '5000',
      })

      return [
        200,
        new Blob(['requestNumber']),
        { 'content-disposition': 'attachment; filename="billing-requests.csv"' },
      ]
    })

    await exportBillingRequests({
      search: 'Fiber',
      fromDate: '2026-06-01',
      untilDate: '2026-06-30',
      minAmount: '1000',
      maxAmount: '5000',
    })

    expect(downloadCsvBlob).toHaveBeenCalledOnce()
    mock.restore()
  })
})
