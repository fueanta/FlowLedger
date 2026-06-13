import { Download } from 'lucide-react'
import { Button } from '../ui/button'

export function DataTableExportButton({ onExport, disabled = false, className }: { onExport: () => void; disabled?: boolean; className?: string }) {
  return (
    <Button type="button" variant="outline" className={className} onClick={onExport} disabled={disabled}>
      <Download className="h-4 w-4" aria-hidden="true" />
      Export CSV
    </Button>
  )
}
