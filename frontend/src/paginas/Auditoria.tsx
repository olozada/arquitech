import { Navigate } from 'react-router-dom'
import { HistorialAuditoria } from '../componentes/HistorialAuditoria'
import { esAdministrador } from '../auth/perfiles'
import { useSesion } from '../auth/contextoSesion'

export function Auditoria() {
  const { sesion } = useSesion()
  if (!esAdministrador(sesion?.tipoUsuario)) {
    return <Navigate to="/" replace />
  }

  return (
    <section>
      <h1>Historial</h1>
      <HistorialAuditoria conFiltros />
    </section>
  )
}
