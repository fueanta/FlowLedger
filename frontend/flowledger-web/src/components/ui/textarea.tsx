import type { TextareaHTMLAttributes } from 'react'
import { cn } from '../../lib/utils'

export function Textarea({ className, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return (
    <textarea
      className={cn(
        'flex min-h-24 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-950 transition-colors placeholder:text-slate-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-900 disabled:cursor-not-allowed disabled:opacity-50 aria-[invalid=true]:border-red-600',
        className,
      )}
      {...props}
    />
  )
}
