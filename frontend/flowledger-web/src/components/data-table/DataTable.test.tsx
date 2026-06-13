import type { ColumnDef } from '@tanstack/react-table'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { DataTable } from './DataTable'
import { DataTablePageSizeSelect } from './DataTablePageSizeSelect'
import { DataTableSearch } from './DataTableSearch'
import { DataTableSortableHeader } from './DataTableSortableHeader'
import { readDataTableState, writeDataTableState } from './dataTableState'

type Row = { id: string; name: string; amount: number }

const columns: ColumnDef<Row>[] = [
  { accessorKey: 'name', header: 'Name', cell: (info) => info.getValue() },
  { accessorKey: 'amount', header: 'Amount', cell: (info) => info.getValue() },
]

describe('DataTable', () => {
  it('renders rows and pagination controls', async () => {
    const onPageChange = vi.fn()
    render(<DataTable data={[{ id: '1', name: 'Fiber Retail', amount: 100 }]} columns={columns} page={2} pageSize={10} totalCount={35} onPageChange={onPageChange} />)

    expect(screen.getByText('Fiber Retail')).toBeInTheDocument()
    expect(screen.getByText('Page 2 of 4 · 35 rows')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Next' }))

    expect(onPageChange).toHaveBeenCalledWith(3)
  })

  it('renders loading, error, and empty states', () => {
    const { rerender } = render(<DataTable data={[]} columns={columns} page={1} pageSize={10} totalCount={0} loading onPageChange={() => undefined} />)
    expect(screen.getByText('Loading records...')).toBeInTheDocument()

    rerender(<DataTable data={[]} columns={columns} page={1} pageSize={10} totalCount={0} error onPageChange={() => undefined} />)
    expect(screen.getByText('Records could not be loaded.')).toBeInTheDocument()

    rerender(<DataTable data={[]} columns={columns} page={1} pageSize={10} totalCount={0} emptyMessage="No queue records." onPageChange={() => undefined} />)
    expect(screen.getByText('No queue records.')).toBeInTheDocument()
  })
})

describe('DataTable controls', () => {
  it('supports search and page-size selection', async () => {
    const onSearch = vi.fn()
    const onPageSize = vi.fn()

    function Harness() {
      const [search, setSearch] = useState('')
      return (
        <>
          <DataTableSearch
            value={search}
            onChange={(value) => {
              setSearch(value)
              onSearch(value)
            }}
          />
          <DataTablePageSizeSelect value={25} onChange={onPageSize} />
        </>
      )
    }

    render(<Harness />)

    await userEvent.type(screen.getByLabelText('Search'), 'fiber')
    await userEvent.selectOptions(screen.getByLabelText('Rows'), '50')

    expect(onSearch).toHaveBeenLastCalledWith('fiber')
    expect(onPageSize).toHaveBeenCalledWith(50)
  })

  it('announces and toggles sortable headers', async () => {
    const onSort = vi.fn()
    render(<DataTableSortableHeader label="Created" column="createdAtUtc" sortBy="createdAtUtc" sortDirection="desc" onSort={onSort} />)

    const button = screen.getByRole('button', { name: /Created/ })
    expect(button).toHaveAttribute('aria-sort', 'descending')

    await userEvent.click(button)

    expect(onSort).toHaveBeenCalledWith('createdAtUtc', 'asc')
  })
})

describe('dataTableState', () => {
  it('reads and writes URL query state', () => {
    const state = readDataTableState(new URLSearchParams('page=2&pageSize=50&search=fiber&sortBy=amount&sortDirection=asc'))

    expect(state).toEqual({ page: 2, pageSize: 50, search: 'fiber', sortBy: 'amount', sortDirection: 'asc' })

    const next = writeDataTableState(new URLSearchParams('page=2&search=fiber'), { page: 1, search: '' })

    expect(next.toString()).toBe('page=1')
  })
})
