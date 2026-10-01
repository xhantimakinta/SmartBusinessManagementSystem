import { Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider, useAuth } from './context/AuthContext'
import { ProtectedRoute } from './components/ui'
import { AppLayout } from './layout/AppLayout'
import { CustomerDetailPage, LoginPage, OrderDetailPage, ProductDetailPage, ProfilePage, RegisterPage } from './pages/Pages'
import { ConnectedCustomersPage, ConnectedDashboardPage, ConnectedOrdersPage, ConnectedProductsPage, ConnectedReportsPage } from './pages/ConnectedPages'
import './App.css'

function RoleAwareDashboard() {
  const { user } = useAuth()
  return user?.role === 'Customer' ? <Navigate to="/orders" replace /> : <ConnectedDashboardPage />
}

function HomeRedirect() {
  const { user } = useAuth()
  return <Navigate to={user ? (user.role === 'Customer' ? '/orders' : '/dashboard') : '/login'} replace />
}

function App() {
  return <AuthProvider><Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route path="/register" element={<RegisterPage />} />
    <Route element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>
      <Route path="/dashboard" element={<RoleAwareDashboard />} />
      <Route path="/customers" element={<ConnectedCustomersPage />} />
      <Route path="/customers/:id" element={<CustomerDetailPage />} />
      <Route path="/products" element={<ConnectedProductsPage />} />
      <Route path="/products/:id" element={<ProductDetailPage />} />
      <Route path="/orders" element={<ConnectedOrdersPage />} />
      <Route path="/orders/:id" element={<OrderDetailPage />} />
      <Route path="/reports" element={<ConnectedReportsPage />} />
      <Route path="/profile" element={<ProfilePage />} />
    </Route>
    <Route path="*" element={<HomeRedirect />} />
  </Routes></AuthProvider>
}

export default App
