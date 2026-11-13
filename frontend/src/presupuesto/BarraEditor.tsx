import { Link } from 'react-router-dom'
import { Button } from 'react-bootstrap'
import { descargarPresupuesto, type PresupuestoDetalle } from '../api/presupuesto'

export function BarraEditor({
  borrador, administrador, guardando, editable, aprobar, postAccion, guardarPlantilla, accion, guardar, previsualizarRecalculo,
}: {
  borrador: PresupuestoDetalle
  administrador: boolean
  guardando: boolean
  editable: boolean
  aprobar: () => Promise<void>
  postAccion: (ruta: string, cuerpo: unknown) => Promise<void>
  guardarPlantilla: () => Promise<void>
  accion: (ruta: string, titulo: string) => Promise<void>
  guardar: () => Promise<void>
  previsualizarRecalculo: () => Promise<void>
}) {
  return (
      <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
        <div>
          <h1 className="mb-0">{borrador.nombre}</h1>
          <p className="mb-0 text-secondary">
            {borrador.construccion} · versión {borrador.version} · {borrador.estado}
          </p>
        </div>
        <div className="d-flex gap-2 flex-wrap">
          <Link className="btn btn-outline-secondary" to={`/presupuestos/${borrador.id}/seguimiento`}>
            Seguimiento
          </Link>
          <Button type="button" variant="outline-primary" onClick={() => void descargarPresupuesto(borrador.id, 'excel')}>
            Excel
          </Button>
          <Button type="button" variant="outline-primary" onClick={() => void descargarPresupuesto(borrador.id, 'pdf')}>
            PDF
          </Button>
          {administrador && borrador.estado === 'Borrador' && !borrador.esPlantilla && (
            <Button type="button" variant="outline-success" disabled={guardando} onClick={() => void aprobar()}>
              Aprobar
            </Button>
          )}
          {editable && (
            <Button type="button" variant="outline-secondary" onClick={() => void postAccion('renumerar', { rowVersion: borrador.rowVersion })}>
              Renumerar
            </Button>
          )}
          {administrador && (
            <Button type="button" variant="outline-secondary" onClick={() => void guardarPlantilla()}>
              Plantilla
            </Button>
          )}
          {administrador && (
            <Button type="button" variant="outline-secondary" disabled={guardando} onClick={() => void accion('nueva-version', 'Nueva versión')}>
              Nueva versión
            </Button>
          )}
          {editable && (
            <Button type="button" disabled={guardando} onClick={() => void guardar()}>
              Guardar
            </Button>
          )}
          {(borrador.estado === 'Borrador' || borrador.estado === 'Aprobado' || borrador.estado === 'Cerrado') && (
            <Button type="button" variant="outline-primary" onClick={() => void previsualizarRecalculo()}>
              {borrador.estado === 'Borrador' ? 'Recalcular con precios actuales' : '¿Cuánto costaría hoy?'}
            </Button>
          )}
        </div>
      </div>
  )
}
