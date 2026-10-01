import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider, useAuth } from '../context/AuthContext'
import { authApi } from '../lib/api'
import type { AuthResponse } from '../types/api'

vi.mock('../lib/api', () => ({
  authApi: {
    login: vi.fn(),
    register: vi.fn(),
  },
}))

const response: AuthResponse = { id: 7, name: 'Avery Customer', email: 'avery@example.com', role: 'Customer', token: 'jwt-test-token' }

function Probe() {
  const { user, login, logout } = useAuth()
  return <div><span>{user?.email ?? 'signed-out'}</span><button onClick={() => void login({ email: response.email, password: 'password' })}>Login</button><button onClick={logout}>Logout</button></div>
}

describe('authentication context', () => {
  beforeEach(() => { localStorage.clear(); vi.clearAllMocks() })
  afterEach(() => cleanup())

  it('stores the token and user after a successful login', async () => {
    vi.mocked(authApi.login).mockResolvedValue(response)
    render(<AuthProvider><Probe /></AuthProvider>)

    fireEvent.click(screen.getByRole('button', { name: 'Login' }))
    await waitFor(() => expect(screen.getByText(response.email)).toBeInTheDocument())

    expect(localStorage.getItem('smartbusiness_token')).toBe(response.token)
    expect(JSON.parse(localStorage.getItem('smartbusiness_user') ?? '{}')).toMatchObject({ id: response.id, role: response.role })
  })

  it('clears authentication state on logout', async () => {
    vi.mocked(authApi.login).mockResolvedValue(response)
    render(<AuthProvider><Probe /></AuthProvider>)
    fireEvent.click(screen.getByRole('button', { name: 'Login' }))
    await waitFor(() => expect(screen.getByText(response.email)).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Logout' }))
    expect(screen.getByText('signed-out')).toBeInTheDocument()
    expect(localStorage.getItem('smartbusiness_token')).toBeNull()
  })

  it('propagates API authentication errors to the caller', async () => {
    vi.mocked(authApi.login).mockRejectedValue(new Error('Invalid email or password.'))
    render(<AuthProvider><Probe /></AuthProvider>)

    await expect(authApi.login({ email: response.email, password: 'bad' })).rejects.toThrow('Invalid email or password.')
  })
})
