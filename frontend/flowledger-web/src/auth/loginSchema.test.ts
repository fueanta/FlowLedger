import { describe, expect, it } from 'vitest'
import { loginSchema } from './loginSchema'

describe('login validation', () => {
  it('requires a valid email and password', () => {
    expect(loginSchema.safeParse({ email: 'bad-email', password: '' }).success).toBe(false)
    expect(loginSchema.safeParse({ email: 'sales@flowledger.local', password: 'local-secret' }).success).toBe(true)
  })
})
