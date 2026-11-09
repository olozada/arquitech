import { useState } from 'react'
import type { FormEvent } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Row, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeDeError, mensajeError } from '../api/cliente'
import { abrirPdf, descargarExcel } from '../api/archivos'
import { dinero, fecha, hoyIso, inicioDeMesIso } from '../actas/formato'
import type { RegistroCatalogo } from '../catalogos/definiciones'

type ActaResumen = {
  numeroActa: number
  fechaInicial: string
  fechaFinal: string
  contratista: string
  observacion: string | null
  total: number
}

type Consulta = {
  actas: ActaResumen[]
  totalGeneral: number
}

type Filtro = {
  desde: string
  hasta: string
  arquitectoId: number
}

export function ReporteActas() {
  const [desde, setDesde] = useState(inicioDeMesIso)
  const [hasta, setHasta] = useState(hoyIso)
  const [arquitectoId, setArquitectoId] = useState(0)
  const [filtro, setFiltro] = useState<Filtro | null>(null)
  const [exportando, setExportando] = useState(false)

  const arquitectos = useQuery({
    queryKey: ['catalogo', 'arquitectos'],
    queryFn: async () => (await cliente.get<RegistroCatalogo[]>('/api/arquitectos')).data,
  })
  const reporte = useQuery({
    queryKey: ['reporte-actas', filtro],
    enabled: filtro != null,
    queryFn: async () => {
      const { data } = await cliente.get<Consulta>('/api/actas', {
        params: {
          desde: filtro?.desde,
          hasta: filtro?.hasta,
          arquitectoId: filtro && filtro.arquitectoId > 0 ? filtro.arquitectoId : undefined,
        },
      })
      return data
    },
  })

  async function consultar(evento: FormEvent) {
    evento.preventDefault()
    if (hasta < desde) {
      await avisar('La fecha final debe ser mayor o igual que la fecha inicial.')
      return
    }

    setFiltro({ desde, hasta, arquitectoId })
  }

  async function exportar() {
    if (hasta < desde) {
      await avisar('La fecha final debe ser mayor o igual que la fecha inicial.')
      return
    }

    setExportando(true)
    try {
      await descargarExcel(desde, hasta, arquitectoId)
    } catch (error) {
      await avisar(await mensajeDeError(error))
    } finally {
      setExportando(false)
    }
  }

  return (
    <section>
      <h1>Reporte de actas</h1>
      <Form className="catalogo-panel" onSubmit={(evento) => void consultar(evento)}>
        <Row className="g-3">
          <Col md={4}>
            <Form.Group controlId="reporte-desde">
              <Form.Label>Fecha inicial</Form.Label>
              <Form.Control type="date" value={desde} onChange={(evento) => setDesde(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={4}>
            <Form.Group controlId="reporte-hasta">
              <Form.Label>Fecha final</Form.Label>
              <Form.Control type="date" value={hasta} onChange={(evento) => setHasta(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={4}>
            <Form.Group controlId="reporte-arquitecto">
              <Form.Label>Arquitecto</Form.Label>
              <Form.Select value={arquitectoId || ''} onChange={(evento) => setArquitectoId(Number(evento.target.value))}>
                <option value="">Todos</option>
                {(arquitectos.data ?? []).map((arquitecto) => (
                  <option key={arquitecto.id} value={arquitecto.id}>
                    {String(arquitecto.nombre ?? '')}
                  </option>
                ))}
              </Form.Select>
            </Form.Group>
          </Col>
        </Row>
        <div className="catalogo-acciones mt-3">
          <Button type="submit">Consultar</Button>
          <Button type="button" variant="outline-primary" disabled={exportando} onClick={() => void exportar()}>
            Exportar Excel
          </Button>
        </div>
      </Form>

      {reporte.isError && <p>{mensajeError(reporte.error)}</p>}
      {reporte.data && reporte.data.actas.length === 0 && <p>No hay registros.</p>}
      {reporte.data && reporte.data.actas.length > 0 && (
        <div className="catalogo-panel table-responsive">
          <Table hover className="catalogo-tabla mb-0">
            <thead>
              <tr>
                <th>N° Acta</th>
                <th>Fecha inicial</th>
                <th>Fecha final</th>
                <th>Contratista</th>
                <th>Observación</th>
                <th>Total</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {reporte.data.actas.map((acta) => (
                <tr key={acta.numeroActa}>
                  <td>{acta.numeroActa}</td>
                  <td>{fecha(acta.fechaInicial)}</td>
                  <td>{fecha(acta.fechaFinal)}</td>
                  <td>{acta.contratista}</td>
                  <td>{acta.observacion ?? ''}</td>
                  <td>{dinero(acta.total)}</td>
                  <td>
                    <Button type="button" size="sm" variant="outline-primary" onClick={() => void abrirPdf(acta.numeroActa)}>
                      Imprimir
                    </Button>
                  </td>
                </tr>
              ))}
              <tr>
                <th colSpan={5}>TOTAL</th>
                <th>{dinero(reporte.data.totalGeneral)}</th>
                <th />
              </tr>
            </tbody>
          </Table>
        </div>
      )}
    </section>
  )
}

function avisar(texto: string) {
  return Swal.fire({ icon: 'error', title: '¡Error!', text: texto, confirmButtonColor: '#1abb9c' })
}
