import { describe, expect, it } from 'vitest'
import { registerFormSchema } from './registerForm'

describe('registerFormSchema', () => {
  it('rejects invalid registration fields and mismatched passwords', () => {
    const result = registerFormSchema.safeParse({
      fullName: '',
      email: 'invalid',
      password: 'short',
      confirmPassword: 'different',
      requestedRole: 'Sales',
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      const errors = result.error.flatten().fieldErrors
      expect(errors.fullName).toContain('Full name must be at least 2 characters.')
      expect(errors.email).toContain('Enter a valid email address.')
      expect(errors.password).toContain('Password must be at least 8 characters.')
      expect(errors.confirmPassword).toContain('Passwords must match.')
    }
  })

  it('accepts valid registration input', () => {
    const result = registerFormSchema.safeParse({
      fullName: 'New User',
      email: 'new-user@flowledger.local',
      password: 'Valid-password-1!',
      confirmPassword: 'Valid-password-1!',
      requestedRole: 'Sales',
    })

    expect(result.success).toBe(true)
  })
})
