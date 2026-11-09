import { useState } from 'react'
import { Button, Col, Form, Row, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { dinero } from '../actas/formato'
import { abrirPdf } from '../api/archivos'
import { mensajeDeError } from '../api/cliente'
import { useAlertas } from '../api/dashboard'
import type { FiltroDashboard } from './filtroUrl'

const notaA2 =
  'El precio de un ítem del acta puede incluir mano de obra. A2 es una referencia para negociar, no un sobrecosto confirmado.'

type Propiedades = {
  filtro: FiltroDashboard
  alFiltrarConstruccion: (id: number) => void
}

export function TablaAlertas({ filtro, alFiltrarConstruccion }: Propiedades) {
  const [tipo, setTipo] = useState('')
  const [severidad, setSeveridad] = useState('')
  const [pagina, setPagina] = useState(1)
  const consulta = useAlertas(filtro, tipo, severidad, pagina)
  const datos = consulta.data

  async function abrir(numero: number) {
    const ventana = window.open('about:blank', '_blank')
    try {
      await abrirPdf(numero, ventana)
    } catch (error) {
      ventana?.close()
      await Swal.fire({ title: '¡Error!', text: await mensajeDeError(error), icon: 'error' })
    }
  }

  return (
    <section className="catalogo-panel" id="alertas">
      <h2>Requiere atención</h2>
      <Row className="g-2 mb-3">
        <Col sm={6} md={3}>
          <Form.Select
            aria-label="Tipo de alerta"
            value={tipo}
            onChange={(evento) => {
              setTipo(evento.target.value)
              setPagina(1)
            }}
          >
            <option value="">Todos los tipos</option>
            <option value="A1">A1 Precio sobre referencia</option>
            <option value="A2">A2 Precio sobre lista</option>
            <option value="A3">A3 Posible doble cobro</option>
            <option value="A4">A4 Obra sin actividad</option>
            <option value="A5">A5 Acta registrada tarde</option>
          </Form.Select>
        </Col>
        <Col sm={6} md={3}>
          <Form.Select
            aria-label="Severidad"
            value={severidad}
            onChange={(evento) => {
              setSeveridad(evento.target.value)
              setPagina(1)
            }}
          >
            <option value="">Todas las severidades</option>
            <option value="Alta">Alta</option>
            <option value="Media">Media</option>
            <option value="Baja">Baja</option>
          </Form.Select>
        </Col>
      </Row>
      <p className="nota-a2">{notaA2}</p>
      {consulta.isLoading ? <p>Cargando alertas…</p> : null}
      <div className="table-responsive">
        <Table hover className="catalogo-tabla">
          <thead>
            <tr>
              <th>Tipo</th>
              <th>Severidad</th>
              <th>Alerta</th>
              <th>Construcción</th>
              <th className="text-end">Impacto</th>
              <th>Acta</th>
            </tr>
          </thead>
          <tbody>
            {(datos?.alertas ?? []).map((alerta) => (
              <tr key={`${alerta.tipo}-${alerta.numeroActa ?? 'obra'}-${alerta.construccionId}-${alerta.titulo}`}>
                <td>{alerta.tipo}</td>
                <td>
                  <span className={`severidad severidad-${alerta.severidad.toLowerCase()}`}>{alerta.severidad}</span>
                </td>
                <td title={alerta.tipo === 'A2' ? notaA2 : alerta.detalle}>{alerta.titulo}</td>
                <td>
                  {alerta.construccionId ? (
                    <button type="button" className="btn btn-link p-0" onClick={() => alFiltrarConstruccion(alerta.construccionId!)}>
                      {alerta.construccion}
                    </button>
                  ) : (
                    (alerta.construccion ?? '—')
                  )}
                </td>
                <td className="text-end">{alerta.impacto === null ? '—' : dinero(alerta.impacto)}</td>
                <td>
                  {alerta.numeroActa ? (
                    <button type="button" className="btn btn-link p-0" onClick={() => void abrir(alerta.numeroActa!)}>
                      N° {alerta.numeroActa}
                    </button>
                  ) : (
                    '—'
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </Table>
      </div>
      {datos && datos.alertas.length === 0 ? <p>No hay alertas con ese filtro.</p> : null}
      {datos && datos.total > datos.tamano ? (
        <div className="catalogo-acciones">
          <Button type="button" variant="outline-secondary" size="sm" disabled={pagina <= 1} onClick={() => setPagina((actual) => actual - 1)}>
            Anterior
          </Button>
          <span>
            Página {datos.pagina} de {Math.ceil(datos.total / datos.tamano)}
          </span>
          <Button
            type="button"
            variant="outline-secondary"
            size="sm"
            disabled={pagina * datos.tamano >= datos.total}
            onClick={() => setPagina((actual) => actual + 1)}
          >
            Siguiente
          </Button>
        </div>
      ) : null}
    </section>
  )
}
