import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { StatusBadge } from './StatusBadge'

describe('StatusBadge', () => {
  it('renders readable billing request status text', () => {
    render(<StatusBadge status="AccountsReview" />)

    expect(screen.getByText('Accounts Review')).toBeInTheDocument()
  })

  it('renders readable invoice status text', () => {
    render(<StatusBadge status="Paid" />)

    expect(screen.getByText('Paid')).toBeInTheDocument()
  })
})
