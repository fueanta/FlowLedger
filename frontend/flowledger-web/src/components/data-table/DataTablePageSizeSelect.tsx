import { Label } from '../ui/label'
import { Select } from '../ui/select'
import { pageSizeOptions } from './dataTableTypes'

export function DataTablePageSizeSelect({ value, onChange }: { value: number; onChange: (value: number) => void }) {
  return (
    <div className="space-y-2">
      <Label htmlFor="data-table-page-size">Rows</Label>
      <Select id="data-table-page-size" value={String(value)} onChange={(event) => onChange(Number(event.target.value))}>
        {pageSizeOptions.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </Select>
    </div>
  )
}
