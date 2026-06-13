import { Button } from '../ui/button'

export function DataTablePagination({
  page,
  pageSize,
  totalCount,
  onPageChange,
}: {
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
}) {
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize))
  const pages = visiblePages(page, pageCount)

  return (
    <div className="flex flex-col gap-3 border-t border-slate-200 p-4 md:flex-row md:items-center md:justify-between">
      <p className="text-sm text-slate-600">
        Page {page} of {pageCount} · {totalCount} rows
      </p>
      <div className="flex flex-wrap gap-2">
        <Button type="button" variant="outline" size="sm" onClick={() => onPageChange(page - 1)} disabled={page <= 1}>
          Previous
        </Button>
        {pages.map((item) => (
          <Button key={item} type="button" variant={item === page ? 'default' : 'outline'} size="sm" onClick={() => onPageChange(item)}>
            {item}
          </Button>
        ))}
        <Button type="button" variant="outline" size="sm" onClick={() => onPageChange(page + 1)} disabled={page >= pageCount}>
          Next
        </Button>
      </div>
    </div>
  )
}

function visiblePages(page: number, pageCount: number) {
  const start = Math.max(1, page - 2)
  const end = Math.min(pageCount, start + 4)
  return Array.from({ length: end - start + 1 }, (_, index) => start + index)
}
