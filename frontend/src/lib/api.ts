import axios from 'axios'
import type { AuthResponse, Category, CategoryPayload, Customer, CustomerPayload, DashboardSummary, LoginPayload, Order, OrderPayload, PagedResponse, Product, ProductPayload, RegisterPayload, SalesBreakdown, SalesReport } from '../types/api'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('smartbusiness_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use((response) => response, (error) => {
  if (error.response?.status === 401) { localStorage.removeItem('smartbusiness_token'); localStorage.removeItem('smartbusiness_user'); if (import.meta.env.PROD ? !window.location.hash.startsWith('#/login') : !window.location.pathname.startsWith('/login')) window.location.assign(import.meta.env.PROD ? `${import.meta.env.BASE_URL}#/login` : '/login') }
  return Promise.reject(error)
})

const page = (params: { page?: number; pageSize?: number; search?: string }) => ({ page: 1, pageSize: 10, ...params })

export const authApi = {
  login: (payload: LoginPayload) => api.post<AuthResponse>('/auth/login', payload).then((r) => r.data),
  register: (payload: RegisterPayload) => api.post<AuthResponse>('/auth/register', payload).then((r) => r.data),
}
export const customersApi = {
  list: (params: { page?: number; pageSize?: number; search?: string }) => api.get<PagedResponse<Customer>>('/customers', { params: page(params) }).then((r) => r.data),
  get: (id: number) => api.get<Customer>(`/customers/${id}`).then((r) => r.data),
  own: () => api.get<Customer>('/customers/me').then((r) => r.data),
  create: (payload: CustomerPayload) => api.post<Customer>('/customers', payload).then((r) => r.data),
  update: (id: number, payload: CustomerPayload) => api.put<Customer>(`/customers/${id}`, payload).then((r) => r.data),
  updateOwn: (payload: CustomerPayload) => api.put<Customer>('/customers/me', payload).then((r) => r.data),
  remove: (id: number) => api.delete(`/customers/${id}`),
}
export const categoriesApi = {
  list: (params: { page?: number; pageSize?: number; search?: string }) => api.get<PagedResponse<Category>>('/categories', { params: page(params) }).then((r) => r.data),
  create: (payload: CategoryPayload) => api.post<Category>('/categories', payload).then((r) => r.data),
  update: (id: number, payload: CategoryPayload) => api.put<Category>(`/categories/${id}`, payload).then((r) => r.data),
  remove: (id: number) => api.delete(`/categories/${id}`),
}
export const productsApi = {
  list: (params: { page?: number; pageSize?: number; search?: string; categoryId?: number }) => api.get<PagedResponse<Product>>('/products', { params: page(params) }).then((r) => r.data),
  get: (id: number) => api.get<Product>(`/products/${id}`).then((r) => r.data),
  create: (payload: ProductPayload) => api.post<Product>('/products', payload).then((r) => r.data),
  update: (id: number, payload: ProductPayload) => api.put<Product>(`/products/${id}`, payload).then((r) => r.data),
  remove: (id: number) => api.delete(`/products/${id}`),
}
export const ordersApi = {
  list: (params: { page?: number; pageSize?: number; search?: string }) => api.get<PagedResponse<Order>>('/orders', { params: page(params) }).then((r) => r.data),
  own: (params: { page?: number; pageSize?: number }) => api.get<PagedResponse<Order>>('/orders/mine', { params: page(params) }).then((r) => r.data),
  get: (id: number) => api.get<Order>(`/orders/${id}`).then((r) => r.data),
  create: (payload: OrderPayload) => api.post<Order>('/orders', payload).then((r) => r.data),
  updateStatus: (id: number, status: string) => api.put<Order>(`/orders/${id}/status`, { status }).then((r) => r.data),
}
export const dashboardApi = {
  summary: () => api.get<DashboardSummary>('/dashboard/summary').then((r) => r.data),
  sales: (from?: string, to?: string) => api.get<SalesReport>('/reports/sales', { params: { from, to } }).then((r) => r.data),
  breakdown: (groupBy: 'product' | 'category', from?: string, to?: string) => api.get<SalesBreakdown>('/reports/sales/breakdown', { params: { groupBy, from, to } }).then((r) => r.data),
}