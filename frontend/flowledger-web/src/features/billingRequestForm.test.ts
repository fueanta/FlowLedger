import { describe, expect, it } from 'vitest'
import { billingRequestFormSchema, calculateRequestTotals } from './billingRequestForm'

describe('billing request form validation', () => {
  it('requires a customer, valid title, and at least one valid line item', () => {
    const result = billingRequestFormSchema.safeParse({
      customerId: '',
      title: 'AB',
      description: '',
      lineItems: [{ description: '', quantity: 0, unitPrice: 0 }],
    })

    expect(result.success).toBe(false)
  })

  it('accepts a valid billing request payload', () => {
    const result = billingRequestFormSchema.safeParse({
      customerId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1',
      title: 'Retail billing package',
      description: 'Monthly service billing',
      lineItems: [{ description: 'Service fee', quantity: 2, unitPrice: 1000 }],
    })

    expect(result.success).toBe(true)
  })

  it('calculates subtotal, VAT, and total from line items', () => {
    expect(calculateRequestTotals([{ description: 'Service fee', quantity: 2, unitPrice: 1000 }], 15)).toEqual({
      subtotal: 2000,
      vat: 300,
      total: 2300,
    })
  })

  it('uses the supplied VAT percentage for totals', () => {
    expect(calculateRequestTotals([{ description: 'Service fee', quantity: 2, unitPrice: 1000 }], 20)).toEqual({
      subtotal: 2000,
      vat: 400,
      total: 2400,
    })
  })
})
