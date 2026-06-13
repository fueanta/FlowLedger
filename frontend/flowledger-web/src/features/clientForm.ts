import { z } from 'zod'

export const clientStatusValues = ['Active', 'Inactive'] as const

export const clientFormSchema = z.object({
  companyName: z.string().trim().min(2, 'Company name must be at least 2 characters.').max(200, 'Company name is too long.'),
  contactPerson: z.string().trim().min(2, 'Contact person must be at least 2 characters.').max(160, 'Contact person is too long.'),
  email: z.string().trim().email('Enter a valid email address.').max(254, 'Email is too long.'),
  phone: z.string().trim().max(40, 'Phone is too long.').optional(),
  address: z.string().trim().min(3, 'Address must be at least 3 characters.').max(500, 'Address is too long.'),
  taxIdentifier: z.string().trim().max(80, 'Tax identifier is too long.').optional(),
  status: z.enum(clientStatusValues),
})

export type ClientFormValues = z.infer<typeof clientFormSchema>

export const defaultClientFormValues: ClientFormValues = {
  companyName: '',
  contactPerson: '',
  email: '',
  phone: '',
  address: '',
  taxIdentifier: '',
  status: 'Active',
}
