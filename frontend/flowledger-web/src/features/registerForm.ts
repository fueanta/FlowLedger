import { z } from 'zod'

export const roleValues = ['Sales', 'Accounts', 'Manager', 'Admin'] as const

export const registerFormSchema = z
  .object({
    fullName: z.string().trim().min(2, 'Full name must be at least 2 characters.').max(160, 'Full name is too long.'),
    email: z.string().trim().email('Enter a valid email address.').max(254, 'Email is too long.'),
    password: z.string().min(8, 'Password must be at least 8 characters.').max(200, 'Password is too long.'),
    confirmPassword: z.string().min(1, 'Confirm password is required.'),
    requestedRole: z.enum(roleValues),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Passwords must match.',
  })

export type RegisterFormValues = z.infer<typeof registerFormSchema>
