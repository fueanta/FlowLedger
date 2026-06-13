import type { ReactNode } from 'react'
import { Button } from './button'

type DialogProps = {
  open: boolean
  title: string
  description?: string
  children: ReactNode
  onClose: () => void
}

export function Dialog({ open, title, description, children, onClose }: DialogProps) {
  if (!open) {
    return null
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4 py-6" role="dialog" aria-modal="true">
      <div className="w-full max-w-lg rounded-lg border border-slate-200 bg-white">
        <div className="flex items-start justify-between gap-4 border-b border-slate-200 p-5">
          <div>
            <h2 className="text-lg font-semibold text-slate-950">{title}</h2>
            {description ? <p className="mt-1 text-sm text-slate-600">{description}</p> : null}
          </div>
          <Button variant="ghost" size="sm" onClick={onClose} aria-label="Close dialog">
            Close
          </Button>
        </div>
        <div className="p-5">{children}</div>
      </div>
    </div>
  )
}
