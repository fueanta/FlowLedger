import type { HTMLAttributes } from 'react'
import { cn } from '../../lib/utils'

type AlertProps = HTMLAttributes<HTMLDivElement> & {
  variant?: 'default' | 'destructive' | 'warning'
}

const variants = {
  default: 'border-slate-300 bg-slate-50 text-slate-800',
  destructive: 'border-red-200 bg-red-50 text-red-800',
  warning: 'border-yellow-200 bg-yellow-50 text-yellow-900',
}

export function Alert({ className, variant = 'default', ...props }: AlertProps) {
  return <div role="alert" className={cn('rounded-md border px-4 py-3 text-sm', variants[variant], className)} {...props} />
}
