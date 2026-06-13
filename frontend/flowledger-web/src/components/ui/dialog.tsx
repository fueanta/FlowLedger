import type { ReactNode } from 'react'
import { useEffect, useState } from 'react'
import { cn } from '../../lib/utils'
import { Button } from './button'

type DialogProps = {
  open: boolean
  title: string
  description?: string
  children: ReactNode
  onClose: () => void
}

export function Dialog({ open, title, description, children, onClose }: DialogProps) {
  const [isPresent, setIsPresent] = useState(open)
  const [isClosing, setIsClosing] = useState(false)

  useEffect(() => {
    if (open) {
      const timer = window.setTimeout(() => {
        setIsPresent(true)
        setIsClosing(false)
      }, 0)

      return () => window.clearTimeout(timer)
    }

    if (!isPresent) {
      return
    }

    const exitTimer = window.setTimeout(() => {
      setIsClosing(true)
    }, 0)
    const unmountTimer = window.setTimeout(() => {
      setIsPresent(false)
      setIsClosing(false)
    }, 160)

    return () => {
      window.clearTimeout(exitTimer)
      window.clearTimeout(unmountTimer)
    }
  }, [isPresent, open])

  if (!isPresent) {
    return null
  }

  return (
    <div
      className={cn(
        'dialog-overlay fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4 py-6',
        isClosing ? 'dialog-overlay-exit' : 'dialog-overlay-enter',
      )}
      role="dialog"
      aria-modal="true"
    >
      <div className={cn('dialog-panel w-full max-w-lg rounded-lg border border-slate-200 bg-white', isClosing ? 'dialog-panel-exit' : 'dialog-panel-enter')}>
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
