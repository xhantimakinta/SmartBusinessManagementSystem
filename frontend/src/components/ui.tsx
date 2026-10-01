import { AlertCircle, Check, ChevronLeft, ChevronRight, LoaderCircle, Search, X } from 'lucide-react'
import { useId, useState, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function Button({ children, variant = 'primary', ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'ghost' | 'danger' }) {
  return <button className={`button button-${variant}`} {...props}>{children}</button>
}
export function Input({ label, error, ...props }: InputHTMLAttributes<HTMLInputElement> & { label?: string; error?: string }) {
  const generatedId = useId()
  const inputId = props.id ?? generatedId
  return <label className="field" htmlFor={inputId}>{label && <span>{label}</span>}<input id={inputId} aria-label={label} className={error ? 'input input-error' : 'input'} {...props} />{error && <small>{error}</small>}</label>
}
export function Modal({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) {
  return <div className="modal-backdrop" role="presentation"><section className="modal" role="dialog" aria-modal="true"><header><div><span className="eyebrow">Workspace</span><h2>{title}</h2></div><button className="icon-button" onClick={onClose} aria-label="Close"><X size={18} /></button></header>{children}</section></div>
}
export function ConfirmationDialog({ title, message, onConfirm, onClose }: { title: string; message: string; onConfirm: () => void; onClose: () => void }) {
  return <Modal title={title} onClose={onClose}><p className="muted">{message}</p><div className="modal-actions"><Button variant="ghost" onClick={onClose}>Cancel</Button><Button variant="danger" onClick={onConfirm}>Confirm</Button></div></Modal>
}
export function Table({ headers, children, empty = 'No records found.' }: { headers: string[]; children: ReactNode; empty?: string }) {
  return <div className="table-wrap"><table><thead><tr>{headers.map((header) => <th key={header}>{header}</th>)}</tr></thead><tbody>{children}</tbody></table>{!children && <div className="empty">{empty}</div>}</div>
}
export function Pagination({ page, pageSize, total, onChange }: { page: number; pageSize: number; total: number; onChange: (page: number) => void }) {
  const pages = Math.max(1, Math.ceil(total / pageSize)); return <div className="pagination"><span>{total} results</span><div><button className="icon-button" disabled={page <= 1} onClick={() => onChange(page - 1)} aria-label="Previous page"><ChevronLeft size={16} /></button><strong>{page} <em>/ {pages}</em></strong><button className="icon-button" disabled={page >= pages} onClick={() => onChange(page + 1)} aria-label="Next page"><ChevronRight size={16} /></button></div></div>
}
export function LoadingSpinner() { return <div className="loading"><LoaderCircle className="spin" size={24} /><span>Loading workspace data...</span></div> }
export function ErrorMessage({ message = 'Something went wrong.' }: { message?: string }) { return <div className="error-message"><AlertCircle size={18} /><span>{message}</span></div> }
export function SearchInput({ value, onChange, placeholder = 'Search records' }: { value: string; onChange: (value: string) => void; placeholder?: string }) { return <label className="search"><Search size={17} /><input value={value} onChange={(event) => onChange(event.target.value)} placeholder={placeholder} /></label> }
export function ProtectedRoute({ children }: { children: ReactNode }) { const { user } = useAuth(); const location = useLocation(); return user ? <>{children}</> : <Navigate to="/login" state={{ from: location }} replace /> }
export function Toast({ message, onClose }: { message: string; onClose: () => void }) { const [visible] = useState(true); return visible ? <div className="toast"><Check size={16} />{message}<button onClick={onClose}><X size={15} /></button></div> : null }