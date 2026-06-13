import { Search } from 'lucide-react'
import { Input } from '../ui/input'
import { Label } from '../ui/label'

export function DataTableSearch({ value, onChange, label = 'Search' }: { value: string; onChange: (value: string) => void; label?: string }) {
  return (
    <div className="space-y-2">
      <Label htmlFor="data-table-search">{label}</Label>
      <div className="relative">
        <Search className="absolute left-3 top-3 h-4 w-4 text-slate-500" aria-hidden="true" />
        <Input id="data-table-search" className="pl-9" value={value} onChange={(event) => onChange(event.target.value)} />
      </div>
    </div>
  )
}
