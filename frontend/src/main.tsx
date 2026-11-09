import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { consultas } from './api/consultas'
import { SesionProvider } from './auth/SesionProvider'
import { App } from './App'
import 'bootstrap/dist/css/bootstrap.min.css'
import 'font-awesome/css/font-awesome.min.css'
import 'sweetalert2/dist/sweetalert2.min.css'
import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={consultas}>
      <SesionProvider>
        <BrowserRouter basename={import.meta.env.BASE_URL.replace(/\/$/, '') || undefined}>
          <App />
        </BrowserRouter>
      </SesionProvider>
    </QueryClientProvider>
  </StrictMode>,
)
