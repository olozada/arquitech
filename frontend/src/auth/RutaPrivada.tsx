import { Navigate, Outlet } from 'react-router-dom'
import { useSesion } from './contextoSesion'

export function RutaPrivada() {
  const { sesion } = useSesion()
  if (!sesion) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}
