import { describe, expect, it } from 'vitest'
import { settingsFormSchema } from './settingsForm'

describe('settingsFormSchema', () => {
  it('rejects values outside configured business ranges', () => {
    const result = settingsFormSchema.safeParse({
      vatPercentage: 31,
      managerApprovalThreshold: 0,
      invoiceDueDays: 366,
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      const errors = result.error.flatten().fieldErrors
      expect(errors.vatPercentage).toContain('VAT cannot exceed 30%.')
      expect(errors.managerApprovalThreshold).toContain('Manager approval threshold must be greater than 0.')
      expect(errors.invoiceDueDays).toContain('Invoice due days cannot exceed 365.')
    }
  })

  it('accepts valid settings values', () => {
    const result = settingsFormSchema.safeParse({
      vatPercentage: 15,
      managerApprovalThreshold: 100000,
      invoiceDueDays: 30,
    })

    expect(result.success).toBe(true)
  })
})
