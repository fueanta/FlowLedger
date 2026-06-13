import { describe, expect, it } from 'vitest'
import { clientFormSchema } from './clientForm'

describe('clientFormSchema', () => {
  it('rejects missing required client fields', () => {
    const result = clientFormSchema.safeParse({
      companyName: '',
      contactPerson: '',
      email: 'not-an-email',
      phone: '',
      address: '',
      taxIdentifier: '',
      status: 'Active',
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.flatten().fieldErrors.companyName).toContain('Company name must be at least 2 characters.')
      expect(result.error.flatten().fieldErrors.contactPerson).toContain('Contact person must be at least 2 characters.')
      expect(result.error.flatten().fieldErrors.email).toContain('Enter a valid email address.')
      expect(result.error.flatten().fieldErrors.address).toContain('Address must be at least 3 characters.')
    }
  })

  it('accepts active and inactive client status values for editable clients', () => {
    const active = clientFormSchema.safeParse(validClient('Active'))
    const inactive = clientFormSchema.safeParse(validClient('Inactive'))
    const archived = clientFormSchema.safeParse(validClient('Archived'))

    expect(active.success).toBe(true)
    expect(inactive.success).toBe(true)
    expect(archived.success).toBe(false)
  })

  it('rejects alphabetic phone numbers', () => {
    const result = clientFormSchema.safeParse(validClient('Active', 'abcd'))
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.flatten().fieldErrors.phone).toContain('Enter a valid phone number.')
    }
  })

  it('accepts valid phone formats', () => {
    const e164 = clientFormSchema.safeParse(validClient('Active', '+8801700000000'))
    const local = clientFormSchema.safeParse(validClient('Active', '01700-000000'))
    const intl  = clientFormSchema.safeParse(validClient('Active', '+1 (555) 123-4567'))
    expect(e164.success).toBe(true)
    expect(local.success).toBe(true)
    expect(intl.success).toBe(true)
  })

  it('accepts blank phone (optional field)', () => {
    const result = clientFormSchema.safeParse(validClient('Active', ''))
    expect(result.success).toBe(true)
  })
})

function validClient(status: string, phone = '+8801700000000') {
  return {
    companyName: 'Valid Client Ltd.',
    contactPerson: 'Valid Owner',
    email: 'valid-client@flowledger.local',
    phone,
    address: 'Valid Road, Dhaka',
    taxIdentifier: 'TIN-VALID',
    status,
  }
}
