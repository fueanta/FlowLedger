import { z } from 'zod'

export const billingRequestLineItemSchema = z.object({
  description: z.string().trim().min(1, 'Line item description is required.').max(200, 'Line item description is too long.'),
  quantity: z.number().int().min(1, 'Quantity must be at least 1.').max(10000, 'Quantity is too high.'),
  unitPrice: z.number().min(1, 'Unit price must be at least 1.').max(10000000, 'Unit price is too high.'),
})

export const billingRequestFormSchema = z.object({
  customerId: z.string().min(1, 'Client is required.'),
  title: z.string().trim().min(3, 'Title must be at least 3 characters.').max(200, 'Title is too long.'),
  description: z.string().max(2000, 'Description is too long.'),
  lineItems: z.array(billingRequestLineItemSchema).min(1, 'Add at least one line item.'),
})

export type BillingRequestFormValues = z.infer<typeof billingRequestFormSchema>

export function calculateRequestTotals(lineItems: BillingRequestFormValues['lineItems'], vatPercentage: number) {
  const subtotal = lineItems.reduce((sum, item) => sum + Number(item.quantity || 0) * Number(item.unitPrice || 0), 0)
  const vat = Math.round(subtotal * (vatPercentage / 100) * 100) / 100

  return {
    subtotal: Math.round(subtotal * 100) / 100,
    vat,
    total: Math.round((subtotal + vat) * 100) / 100,
  }
}
