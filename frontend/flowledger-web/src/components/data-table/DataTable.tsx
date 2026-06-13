import { flexRender, getCoreRowModel, useReactTable, type ColumnDef } from '@tanstack/react-table'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../ui/table'
import { DataTablePagination } from './DataTablePagination'

export function DataTable<TData>({
  data,
  columns,
  page,
  pageSize,
  totalCount,
  loading = false,
  error = false,
  emptyMessage = 'No records found.',
  onPageChange,
}: {
  data: TData[]
  columns: ColumnDef<TData>[]
  page: number
  pageSize: number
  totalCount: number
  loading?: boolean
  error?: boolean
  emptyMessage?: string
  onPageChange: (page: number) => void
}) {
  // TanStack Table returns table helpers that React Compiler intentionally skips.
  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({ data, columns, getCoreRowModel: getCoreRowModel() })
  const columnCount = columns.length

  return (
    <div className="overflow-hidden rounded-md border border-slate-200 bg-white">
      <div className="overflow-x-auto">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id}>{header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}</TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={columnCount}>Loading records...</TableCell>
              </TableRow>
            ) : error ? (
              <TableRow>
                <TableCell colSpan={columnCount}>Records could not be loaded.</TableCell>
              </TableRow>
            ) : table.getRowModel().rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columnCount}>{emptyMessage}</TableCell>
              </TableRow>
            ) : (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>{flexRender(cell.column.columnDef.cell, cell.getContext())}</TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
      <DataTablePagination page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={onPageChange} />
    </div>
  )
}
