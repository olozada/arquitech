import { Button, Table } from 'react-bootstrap'
import { dinero } from './formato'
import { perfilDeNuevoItem } from '../auth/perfiles'
import type { LineaBorrador } from './useBorradorActa'

export function TablaItemsActa({
  detalles,
  revisor,
  cerrada,
  administrador,
  tipoUsuario,
  total,
  todosRechazados,
  hayRechazo,
  alRevisar,
  alAbrir,
}: {
  detalles: LineaBorrador[]
  revisor: boolean
  cerrada: boolean
  administrador: boolean
  tipoUsuario: string | undefined
  total: number
  todosRechazados: boolean
  hayRechazo: boolean
  alRevisar: (clave: string, aprobado: boolean) => void
  alAbrir: (item: LineaBorrador) => void
}) {
  return (
    <div className="catalogo-panel">
      {detalles.length === 0 ? (
        <p>No hay ítems.</p>
      ) : (
        <div className="table-responsive">
          <Table hover className="catalogo-tabla mb-0">
            <thead>
              <tr>
                <th>{revisor ? 'Revisión' : 'Seleccionar'}</th>
                <th>Perfil</th>
                <th>Estado</th>
                <th>Construcción</th>
                <th>Concepto</th>
                <th>Insumo</th>
                <th>UM</th>
                <th>Cantidad</th>
                <th>Precio</th>
                <th>Descripción</th>
                <th>Subtotal</th>
              </tr>
            </thead>
            <tbody>
              {detalles.map((item) => (
                <tr key={item.clave}>
                  <td>
                    {cerrada ? null : revisor ? (
                      <div className="d-flex gap-1">
                        <Button type="button" size="sm" variant={item.aprobado ? 'success' : 'outline-success'} onClick={() => alRevisar(item.clave, true)}>
                          Aprobar
                        </Button>
                        <Button type="button" size="sm" variant={!item.aprobado ? 'danger' : 'outline-danger'} onClick={() => alRevisar(item.clave, false)}>
                          Rechazar
                        </Button>
                      </div>
                    ) : (
                      (administrador || item.tipoUsuarioId === perfilDeNuevoItem(tipoUsuario).id) && (
                        <Button type="button" size="sm" variant="outline-primary" onClick={() => alAbrir(item)}>
                          Seleccionar
                        </Button>
                      )
                    )}
                  </td>
                  <td>{item.tipoUsuario}</td>
                  <td>{item.aprobado ? 'Aprobado' : 'Rechazado'}</td>
                  <td>{item.construccion}</td>
                  <td>{item.concepto}</td>
                  <td>{item.insumo ?? ''}</td>
                  <td>{item.unidadMedida}</td>
                  <td>{item.cantidad}</td>
                  <td>{dinero(item.precio, 2)}</td>
                  <td>{item.descripcion}</td>
                  <td>{dinero(item.aprobado ? item.cantidad * item.precio : 0, 2)}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        </div>
      )}
      <p className="total-acta">Total: {dinero(total, 2)}</p>
      {todosRechazados && <p>Todos los ítems están rechazados. El valor del acta es 0.</p>}
      {hayRechazo && !todosRechazados && <p>Los ítems rechazados suman 0.</p>}
    </div>
  )
}
