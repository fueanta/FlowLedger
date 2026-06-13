import { zodResolver } from '@hookform/resolvers/zod'
import { LogIn } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { Alert } from '../components/ui/alert'
import { Button } from '../components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { loginSchema, type LoginFormValues } from './loginSchema'
import { useAuth } from './useAuth'

type LoginLocationState = {
  from?: string
  expired?: boolean
}

export function LoginPage() {
  const { isAuthenticated, login } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const state = location.state as LoginLocationState | null
  const [serverError, setServerError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })

  if (isAuthenticated) {
    return <Navigate to={state?.from ?? '/app/dashboard'} replace />
  }

  async function onSubmit(values: LoginFormValues) {
    setServerError(null)
    try {
      await login(values)
      navigate(state?.from ?? '/app/dashboard', { replace: true })
    } catch (error) {
      setServerError(error instanceof Error ? error.message : 'Login failed.')
    }
  }

  return (
    <main className="flex min-h-svh items-center justify-center bg-slate-50 px-4 py-10">
      <section className="w-full max-w-md" aria-labelledby="login-title">
        <div className="mb-6">
          <p className="text-sm font-semibold uppercase text-blue-900">ERP workflow module</p>
          <h1 id="login-title" className="mt-2 text-4xl font-bold tracking-normal text-slate-950">
            FlowLedger
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-600">
            Sign in with seeded internal users to review billing approvals, invoices, and audit history.
          </p>
        </div>
        <Card>
          <CardHeader>
            <CardTitle>Sign in</CardTitle>
            <CardDescription>Use credentials supplied through local environment variables.</CardDescription>
          </CardHeader>
          <CardContent>
            <form className="space-y-5" onSubmit={handleSubmit(onSubmit)} noValidate>
              {state?.expired ? <Alert variant="warning">Your session expired. Please log in again.</Alert> : null}
              {serverError ? <Alert variant="destructive">{serverError}</Alert> : null}

              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input id="email" autoComplete="email" {...register('email')} aria-invalid={Boolean(errors.email)} />
                {errors.email ? <p className="text-sm text-red-700">{errors.email.message}</p> : null}
              </div>

              <div className="space-y-2">
                <Label htmlFor="password">Password</Label>
                <Input
                  id="password"
                  type="password"
                  autoComplete="current-password"
                  {...register('password')}
                  aria-invalid={Boolean(errors.password)}
                />
                {errors.password ? <p className="text-sm text-red-700">{errors.password.message}</p> : null}
              </div>

              <Button className="w-full" type="submit" disabled={isSubmitting}>
                <LogIn className="h-4 w-4" aria-hidden="true" />
                {isSubmitting ? 'Signing in...' : 'Sign in'}
              </Button>
              <Button asChild className="w-full" variant="outline">
                <Link to="/register">Request access</Link>
              </Button>
            </form>
          </CardContent>
        </Card>
      </section>
    </main>
  )
}
