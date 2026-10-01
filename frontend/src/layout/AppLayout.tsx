import { BarChart3, Boxes, LayoutDashboard, LogOut, Menu, Settings2, ShoppingBag, Users, X } from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'
import { useState } from 'react'
import { useAuth } from '../context/AuthContext'

const links = [
  { to: '/dashboard', label: 'Overview', icon: LayoutDashboard },
  { to: '/customers', label: 'Customers', icon: Users, roles: ['Admin', 'Manager', 'Employee'] },
  { to: '/products', label: 'Products', icon: Boxes, roles: ['Admin', 'Manager'] },
  { to: '/orders', label: 'Orders', icon: ShoppingBag, roles: ['Admin', 'Manager', 'Employee', 'Customer'] },
  { to: '/reports', label: 'Reports', icon: BarChart3, roles: ['Admin', 'Manager'] },
]

export function AppLayout() {
  const { user, logout } = useAuth()
  const [open, setOpen] = useState(false)
  const available = links.filter((link) => !link.roles || link.roles.includes(user?.role ?? ''))
  return <div className="app-shell">
    <aside className={open ? 'sidebar sidebar-open' : 'sidebar'}>
      <div className="brand"><span className="brand-mark">S</span><div><strong>SmartBusiness</strong><small>Operations desk</small></div><button className="icon-button mobile-only" onClick={() => setOpen(false)} aria-label="Close navigation"><X size={18} /></button></div>
      <nav>{available.map(({ to, label, icon: Icon }) => <NavLink key={to} to={to} onClick={() => setOpen(false)} className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}><Icon size={18} /><span>{label}</span></NavLink>)}</nav>
      <div className="sidebar-bottom"><NavLink to="/profile" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}><Settings2 size={18} /><span>Profile</span></NavLink><button className="nav-link logout" onClick={logout}><LogOut size={18} /><span>Sign out</span></button></div>
    </aside>
    <main className="main-shell"><header className="topbar"><button className="icon-button mobile-only" onClick={() => setOpen(true)} aria-label="Open navigation"><Menu size={20} /></button><div className="topbar-context"><span className="status-dot" />Live workspace</div><div className="user-chip"><span>{user?.name.slice(0, 1).toUpperCase()}</span><div><strong>{user?.name}</strong><small>{user?.role}</small></div></div></header><section className="page-content"><Outlet /></section></main>
  </div>
}