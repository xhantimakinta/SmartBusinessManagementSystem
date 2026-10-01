import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ErrorMessage, Input, Pagination } from '../components/ui'

describe('shared UI components', () => {
  afterEach(() => cleanup())
  it('renders validation messages next to an input', () => {
    render(<Input label="Email" error="Enter a valid email" value="bad" onChange={() => undefined} />)

    expect(screen.getByLabelText('Email')).toHaveValue('bad')
    expect(screen.getByText('Enter a valid email')).toBeInTheDocument()
  })

  it('renders safe API error messages', () => {
    render(<ErrorMessage message="The service is temporarily unavailable." />)

    expect(screen.getByText('The service is temporarily unavailable.')).toBeInTheDocument()
    expect(screen.queryByText(/stack|exception|select/i)).not.toBeInTheDocument()
  })

  it('disables pagination controls at the boundaries', () => {
    const onChange = vi.fn()
    render(<Pagination page={1} pageSize={10} total={21} onChange={onChange} />)

    expect(screen.getByRole('button', { name: 'Previous page' })).toBeDisabled()
    fireEvent.click(screen.getByRole('button', { name: 'Next page' }))
    expect(onChange).toHaveBeenCalledWith(2)
  })
})
