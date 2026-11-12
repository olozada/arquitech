import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useSesion } from './contextoSesion'

export function RutaPrivada() {
  const { sesion } = useSesion()
  const ubicacion = useLocation()
  if (!sesion) {
    return <Navigate to="/login" replace />
  }

  if (sesion.debeCambiarContrasena && ubicacion.pathname !== '/cuenta/contrasena') {
    return <Navigate to="/cuenta/contrasena" replace />
  }

  return <Outlet />
}
