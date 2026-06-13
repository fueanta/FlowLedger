import { act, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { Dialog } from './dialog'

describe('Dialog', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('keeps dialog mounted briefly for exit animation before unmounting', async () => {
    const { rerender } = render(
      <Dialog open title="Reject request" onClose={() => undefined}>
        <button>Confirm</button>
      </Dialog>,
    )

    expect(screen.getByRole('dialog')).toHaveClass('dialog-overlay-enter')

    rerender(
      <Dialog open={false} title="Reject request" onClose={() => undefined}>
        <button>Confirm</button>
      </Dialog>,
    )

    act(() => {
      vi.advanceTimersByTime(0)
    })
    expect(screen.getByRole('dialog')).toHaveClass('dialog-overlay-exit')

    act(() => {
      vi.advanceTimersByTime(170)
    })

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
