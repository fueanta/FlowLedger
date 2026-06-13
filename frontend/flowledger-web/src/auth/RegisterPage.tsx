import { zodResolver } from '@hookform/resolvers/zod'
import { UserPlus } from 'lucide-react'
import type { ReactNode } from 'react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { registerEnrollment } from '../api/enrollment'
import { Alert } from '../components/ui/alert'
import { Button } from '../components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { Select } from '../components/ui/select'
import { getApiErrorMessage } from '../lib/apiClient'
import { registerFormSchema, type RegisterFormValues } from '../features/registerForm'

export function RegisterPage() {
  const [submitted, setSubmitted] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerFormSchema),
    defaultValues: { fullName: '', email: '', password: '', confirmPassword: '', requestedRole: 'Sales' },
  })

  async function onSubmit(values: RegisterFormValues) {
    setServerError(null)
    try {
      await registerEnrollment({
        fullName: values.fullName.trim(),
        email: values.email.trim(),
        password: values.password,
        requestedRole: values.requestedRole,
      })
      setSubmitted(true)
    } catch (error) {
      setServerError(getApiErrorMessage(error, 'Enrollment request could not be submitted.'))
    }
  }

  return (
    <main className="flex min-h-svh items-center justify-center bg-slate-50 px-4 py-10">
      <section className="w-full max-w-lg" aria-labelledby="register-title">
        <div className="mb-6">
          <p className="text-sm font-semibold uppercase text-blue-900">FlowLedger access</p>
          <h1 id="register-title" className="mt-2 text-4xl font-bold tracking-normal text-slate-950">
            Request enrollment
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-600">Admin approval is required before new internal users can sign in.</p>
        </div>
        <Card>
          <CardHeader>
            <CardTitle>Enrollment request</CardTitle>
            <CardDescription>Approved requests create an active FlowLedger user account.</CardDescription>
          </CardHeader>
          <CardContent>
            {submitted ? (
              <div className="space-y-4">
                <Alert>Enrollment request submitted for Admin review.</Alert>
                <Button asChild variant="outline">
                  <Link to="/login">Back to sign in</Link>
                </Button>
              </div>
            ) : (
              <form className="space-y-5" onSubmit={handleSubmit(onSubmit)} noValidate>
                {serverError ? <Alert variant="destructive">{serverError}</Alert> : null}
                <Field label="Full Name" id="fullName" error={errors.fullName?.message}>
                  <Input id="fullName" {...register('fullName')} aria-invalid={Boolean(errors.fullName)} />
                </Field>
                <Field label="Email" id="email" error={errors.email?.message}>
                  <Input id="email" type="email" autoComplete="email" {...register('email')} aria-invalid={Boolean(errors.email)} />
                </Field>
                <Field label="Requested Role" id="requestedRole" error={errors.requestedRole?.message}>
                  <Select id="requestedRole" {...register('requestedRole')} aria-invalid={Boolean(errors.requestedRole)}>
                    <option value="Sales">Sales</option>
                    <option value="Accounts">Accounts</option>
                    <option value="Manager">Manager</option>
                    <option value="Admin">Admin</option>
                  </Select>
                </Field>
                <Field label="Password" id="password" error={errors.password?.message}>
                  <Input id="password" type="password" autoComplete="new-password" {...register('password')} aria-invalid={Boolean(errors.password)} />
                </Field>
                <Field label="Confirm Password" id="confirmPassword" error={errors.confirmPassword?.message}>
                  <Input
                    id="confirmPassword"
                    type="password"
                    autoComplete="new-password"
                    {...register('confirmPassword')}
                    aria-invalid={Boolean(errors.confirmPassword)}
                  />
                </Field>
                <Button className="w-full" type="submit" disabled={isSubmitting}>
                  <UserPlus className="h-4 w-4" aria-hidden="true" />
                  {isSubmitting ? 'Submitting...' : 'Submit Request'}
                </Button>
                <Button asChild className="w-full" variant="outline">
                  <Link to="/login">Back to sign in</Link>
                </Button>
              </form>
            )}
          </CardContent>
        </Card>
      </section>
    </main>
  )
}

function Field({ label, id, error, children }: { label: string; id: string; error?: string; children: ReactNode }) {
  return (
    <div className="space-y-2">
      <Label htmlFor={id}>{label}</Label>
      {children}
      {error ? <p className="text-sm text-red-700">{error}</p> : null}
    </div>
  )
}
