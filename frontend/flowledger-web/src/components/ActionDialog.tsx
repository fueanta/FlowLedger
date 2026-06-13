import { useState } from 'react'
import { Button } from './ui/button'
import { Dialog } from './ui/dialog'
import { Label } from './ui/label'
import { Textarea } from './ui/textarea'

type ActionDialogProps = {
  open: boolean
  title: string
  description: string
  label: string
  confirmLabel: string
  destructive?: boolean
  required?: boolean
  busy?: boolean
  onClose: () => void
  onConfirm: (value: string) => Promise<void> | void
}

export function ActionDialog({
  open,
  title,
  description,
  label,
  confirmLabel,
  destructive = false,
  required = false,
  busy = false,
  onClose,
  onConfirm,
}: ActionDialogProps) {
  const [value, setValue] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function handleConfirm() {
    if (required && value.trim().length === 0) {
      setError(`${label} is required.`)
      return
    }

    await onConfirm(value)
    setValue('')
    setError(null)
  }

  return (
    <Dialog open={open} title={title} description={description} onClose={onClose}>
      <div className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="action-dialog-value">{label}</Label>
          <Textarea
            id="action-dialog-value"
            value={value}
            onChange={(event) => {
              setValue(event.target.value)
              setError(null)
            }}
            maxLength={2000}
            aria-invalid={Boolean(error)}
          />
          {error ? <p className="text-sm text-red-700">{error}</p> : null}
        </div>
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose} disabled={busy}>
            Cancel
          </Button>
          <Button variant={destructive ? 'destructive' : 'default'} onClick={handleConfirm} disabled={busy}>
            {busy ? 'Working...' : confirmLabel}
          </Button>
        </div>
      </div>
    </Dialog>
  )
}
