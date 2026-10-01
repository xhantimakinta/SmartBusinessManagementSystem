export type UserRole = 'Admin' | 'Manager' | 'Employee' | 'Customer'
export type OrderStatus = 'Pending' | 'Confirmed' | 'Shipped' | 'Completed' | 'Cancelled'

export interface AuthResponse { id: number; name: string; email: string; role: UserRole; token: string }
export interface Customer { id: number; name: string; email: string; phone: string; address: string; createdAt: string }
export interface Category { id: number; name: string; description: string }
export interface Product { id: number; categoryId: number; categoryName: string; name: string; description: string; price: number; stockQuantity: number; createdAt: string; updatedAt: string }
export interface OrderItem { id: number; productId: number; productName: string; quantity: number; unitPrice: number; subtotal: number }
export interface Order { id: number; customerId: number; customerName: string; orderDate: string; status: OrderStatus; totalAmount: number; items: OrderItem[] }
export interface PagedResponse<T> { items: T[]; page: number; pageSize: number; totalCount: number }
export interface RecentOrder { id: number; customerName: string; orderDate: string; status: OrderStatus; totalAmount: number }
export interface DashboardSummary { customerCount: number; productCount: number; pendingOrderCount: number; orderCount: number; totalSales: number; lowStockProductCount: number; recentOrders: RecentOrder[] }
export interface SalesReport { from: string; to: string; totalSales: number; orderCount: number; rows: { date: string; orderCount: number; totalSales: number }[] }
export interface SalesBreakdown { from: string; to: string; groupBy: 'product' | 'category'; rows: { label: string; orderCount: number; totalSales: number }[] }
export interface RegisterPayload { name: string; email: string; password: string }
export interface LoginPayload { email: string; password: string }
export interface CustomerPayload { name: string; email: string; phone: string; address: string }
export interface CategoryPayload { name: string; description: string }
export interface ProductPayload { categoryId: number; name: string; description: string; price: number; stockQuantity: number }
export interface OrderPayload { customerId: number; items: { productId: number; quantity: number }[] }