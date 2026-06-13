import type { InputHTMLAttributes } from 'react'
import { cn } from '../../lib/utils'

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      className={cn(
        'flex h-10 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-950 transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-slate-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-900 disabled:cursor-not-allowed disabled:opacity-50 aria-[invalid=true]:border-red-600',
        className,
      )}
      {...props}
    />
  )
}
