import { Download } from 'lucide-react'
import { Button } from '../ui/button'

export function DataTableExportButton({ onExport, disabled = false }: { onExport: () => void; disabled?: boolean }) {
  return (
    <Button type="button" variant="outline" onClick={onExport} disabled={disabled}>
      <Download className="h-4 w-4" aria-hidden="true" />
      Export CSV
    </Button>
  )
}
