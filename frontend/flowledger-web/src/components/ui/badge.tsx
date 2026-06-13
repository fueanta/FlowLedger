import type { HTMLAttributes } from 'react'
import { cn } from '../../lib/utils'

type BadgeProps = HTMLAttributes<HTMLSpanElement> & {
  variant?: 'default' | 'secondary' | 'success' | 'warning' | 'destructive' | 'outline'
}

const variants = {
  default: 'bg-slate-950 text-white',
  secondary: 'bg-blue-100 text-blue-950',
  success: 'bg-emerald-100 text-emerald-900',
  warning: 'bg-yellow-100 text-yellow-900',
  destructive: 'bg-red-100 text-red-900',
  outline: 'border border-slate-300 bg-white text-slate-700',
}

export function Badge({ className, variant = 'default', ...props }: BadgeProps) {
  return (
    <span
      className={cn('inline-flex items-center rounded-md px-2 py-1 text-xs font-semibold leading-none', variants[variant], className)}
      {...props}
    />
  )
}
