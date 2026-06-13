import { z } from 'zod'

export const settingsFormSchema = z.object({
  vatPercentage: z.number().min(0, 'VAT cannot be negative.').max(30, 'VAT cannot exceed 30%.'),
  managerApprovalThreshold: z.number().min(1, 'Manager approval threshold must be greater than 0.'),
  invoiceDueDays: z.number().int().min(1, 'Invoice due days must be at least 1.').max(365, 'Invoice due days cannot exceed 365.'),
})

export type SettingsFormValues = z.infer<typeof settingsFormSchema>
