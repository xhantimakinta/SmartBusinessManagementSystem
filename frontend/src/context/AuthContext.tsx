import { createContext, useContext, useState, type ReactNode } from 'react'
import { authApi } from '../lib/api'
import type { AuthResponse, LoginPayload, RegisterPayload } from '../types/api'

interface AuthContextValue { user: AuthResponse | null; login: (payload: LoginPayload) => Promise<AuthResponse>; register: (payload: RegisterPayload) => Promise<AuthResponse>; logout: () => void }
const AuthContext = createContext<AuthContextValue | null>(null)
const storedUser = () => { const value = localStorage.getItem('smartbusiness_user'); return value ? JSON.parse(value) as AuthResponse : null }

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthResponse | null>(storedUser)
  const save = (value: AuthResponse) => { localStorage.setItem('smartbusiness_token', value.token); localStorage.setItem('smartbusiness_user', JSON.stringify(value)); setUser(value) }
  const login = async (payload: LoginPayload) => { const value = await authApi.login(payload); save(value); return value }
  const register = async (payload: RegisterPayload) => { const value = await authApi.register(payload); save(value); return value }
  const logout = () => { localStorage.removeItem('smartbusiness_token'); localStorage.removeItem('smartbusiness_user'); setUser(null) }
  return <AuthContext.Provider value={{ user, login, register, logout }}>{children}</AuthContext.Provider>
}
// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() { const context = useContext(AuthContext); if (!context) throw new Error('useAuth must be used inside AuthProvider'); return context }