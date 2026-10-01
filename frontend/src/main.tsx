import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { BrowserRouter, HashRouter } from 'react-router-dom'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {import.meta.env.PROD ? <HashRouter><App /></HashRouter> : <BrowserRouter><App /></BrowserRouter>}
  </StrictMode>,
)
