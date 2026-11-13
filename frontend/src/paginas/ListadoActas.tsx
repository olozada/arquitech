import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { descartarBorradorActa } from '../actas/useBorradorActa'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Row, Table } from 'react-bootstrap'
import type { ConsultaActas } from '../api/actas'
import { cliente, mensajeError } from '../api/cliente'
import { tamanoListado } from '../api/pagina'
import { useSesion } from '../auth/contextoSesion'
import { esAdministrador } from '../auth/perfiles'
import { abrirPdf } from '../api/archivos'
import { dinero, fecha } from '../actas/formato'
import { Paginador } from '../componentes/Paginador'

type Consulta = ConsultaActas

type CampoOrden = 'numero' | 'fechaInicial' | 'fechaFinal' | 'arquitecto' | 'revisor' | 'contratista'
type Direccion = 'asc' | 'desc'

const ordenInicial: CampoOrden = 'numero'

export function ListadoActas() {
  const { sesion } = useSesion()
  const navegar = useNavigate()
  const [numero, setNumero] = useState('')
  const [arquitecto, setArquitecto] = useState('')
  const [revisor, setRevisor] = useState('')
  const [contratista, setContratista] = useState('')
  const [desde, setDesde] = useState('')
  const [hasta, setHasta] = useState('')
  const [estado, setEstado] = useState('')
  const [campo, setCampo] = useState<CampoOrden>(ordenInicial)
  const [direccion, setDireccion] = useState<Direccion>('desc')
  const [pagina, setPagina] = useState(1)
  const fechasInvalidas = Boolean(desde && hasta && hasta < desde)
  const listado = useQuery({
    queryKey: ['actas', numero, arquitecto, revisor, contratista, desde, hasta, estado, campo, direccion, pagina],
    queryFn: async () =>
      (await cliente.get<Consulta>('/api/actas', {
        params: {
          numero: numero.trim() || undefined,
          nombreArquitecto: arquitecto.trim() || undefined,
          nombreRevisor: revisor.trim() || undefined,
          nombreContratista: contratista.trim() || undefined,
          desde: !fechasInvalidas && desde ? desde : undefined,
          hasta: !fechasInvalidas && hasta ? hasta : undefined,
          estado: estado || undefined,
          orden: campo,
          descendente: direccion === 'desc',
          pagina,
          tamano: tamanoListado,
        },
      })).data,
  })
  const actas = listado.data?.items ?? []
  const hayFiltro = Boolean(numero.trim() || arquitecto.trim() || revisor.trim() || contratista.trim() || desde || hasta || estado)

  function ordenar(siguiente: CampoOrden) {
    setPagina(1)
    if (campo === siguiente) {
      setDireccion((actual) => (actual === 'asc' ? 'desc' : 'asc'))
      return
    }

    setCampo(siguiente)
    setDireccion('asc')
  }

  function limpiar() {
    setPagina(1)
    setNumero('')
    setArquitecto('')
    setRevisor('')
    setContratista('')
    setDesde('')
    setHasta('')
    setEstado('')
    setCampo(ordenInicial)
    setDireccion('desc')
  }

  return (
    <section>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <h1 className="mb-0">Actas</h1>
        {esAdministrador(sesion?.tipoUsuario) && (
          <Button
            type="button"
            onClick={() => {
              descartarBorradorActa()
              navegar('/actas/nueva', { state: { nueva: Date.now() } })
            }}
          >
            Nueva acta
          </Button>
        )}
      </div>

      <Form className="catalogo-panel" onSubmit={(evento) => evento.preventDefault()}>
        <Row className="g-3">
          <Col md={3}>
            <Form.Group controlId="actas-numero">
              <Form.Label>N° Acta</Form.Label>
              <Form.Control value={numero} inputMode="numeric" placeholder="Número" onChange={(evento) => { setPagina(1); setNumero(evento.target.value) }} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-arquitecto">
              <Form.Label>Arquitecto</Form.Label>
              <Form.Control value={arquitecto} placeholder="Nombre" onChange={(evento) => { setPagina(1); setArquitecto(evento.target.value) }} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-revisor">
              <Form.Label>Revisor</Form.Label>
              <Form.Control value={revisor} placeholder="Nombre" onChange={(evento) => { setPagina(1); setRevisor(evento.target.value) }} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-contratista">
              <Form.Label>Contratista</Form.Label>
              <Form.Control value={contratista} placeholder="Nombre" onChange={(evento) => { setPagina(1); setContratista(evento.target.value) }} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-desde">
              <Form.Label>Fecha desde</Form.Label>
              <Form.Control type="date" value={desde} onChange={(evento) => { setPagina(1); setDesde(evento.target.value) }} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-hasta">
              <Form.Label>Fecha hasta</Form.Label>
              <Form.Control type="date" value={hasta} onChange={(evento) => { setPagina(1); setHasta(evento.target.value) }} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-estado">
              <Form.Label>Estado</Form.Label>
              <Form.Select value={estado} onChange={(evento) => { setPagina(1); setEstado(evento.target.value) }}>
                <option value="">Todos</option>
                <option value="abierta">Abierta</option>
                <option value="cerrada">Cerrada</option>
              </Form.Select>
            </Form.Group>
          </Col>
        </Row>
        <div className="catalogo-acciones mt-3">
          <Button type="button" variant="outline-secondary" disabled={!hayFiltro && campo === ordenInicial && direccion === 'desc'} onClick={limpiar}>
            Limpiar
          </Button>
        </div>
        {fechasInvalidas && <p className="text-danger mt-3 mb-0">La fecha hasta debe ser mayor o igual que la fecha desde.</p>}
      </Form>

      {listado.isPending && <p>Cargando actas…</p>}
      {listado.isError && <p>{mensajeError(listado.error)}</p>}
      {listado.data && actas.length === 0 && <p>No hay actas con ese filtro.</p>}
      {actas.length > 0 && (
        <div className="catalogo-panel table-responsive">
          <Table hover size="sm" className="catalogo-tabla actas-tabla align-middle mb-0">
            <thead>
              <tr>
                <EncabezadoOrden campo="numero" activo={campo} direccion={direccion} alOrdenar={ordenar}>
                  N° Acta
                </EncabezadoOrden>
                <EncabezadoOrden campo="fechaInicial" activo={campo} direccion={direccion} alOrdenar={ordenar}>
                  Fecha inicial
                </EncabezadoOrden>
                <EncabezadoOrden campo="fechaFinal" activo={campo} direccion={direccion} alOrdenar={ordenar}>
                  Fecha final
                </EncabezadoOrden>
                <EncabezadoOrden campo="arquitecto" activo={campo} direccion={direccion} alOrdenar={ordenar}>
                  Arquitecto
                </EncabezadoOrden>
                <EncabezadoOrden campo="revisor" activo={campo} direccion={direccion} alOrdenar={ordenar}>
                  Revisor
                </EncabezadoOrden>
                <EncabezadoOrden campo="contratista" activo={campo} direccion={direccion} alOrdenar={ordenar}>
                  Contratista
                </EncabezadoOrden>
                <th>Observación</th>
                <th className="text-end">Total</th>
                <th>Estado</th>
                <th className="text-end">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {actas.map((acta) => (
                <tr key={acta.numeroActa}>
                  <td className="text-nowrap">
                    <Link to={`/actas/${acta.numeroActa}`}>{acta.numeroActa}</Link>
                  </td>
                  <td className="text-nowrap">{fecha(acta.fechaInicial)}</td>
                  <td className="text-nowrap">{fecha(acta.fechaFinal)}</td>
                  <td>{acta.arquitecto}</td>
                  <td>{acta.revisor}</td>
                  <td>{acta.contratista}</td>
                  <td>{acta.observacion ?? ''}</td>
                  <td className="text-end text-nowrap">{dinero(acta.total)}</td>
                  <td className="text-nowrap">
                    <span className={acta.cerrada ? 'acta-estado acta-estado-cerrada' : 'acta-estado acta-estado-abierta'}>
                      {acta.cerrada ? 'Cerrada' : 'Abierta'}
                    </span>
                  </td>
                  <td>
                    <div className="actas-acciones">
                      <Button type="button" size="sm" variant="outline-primary" onClick={() => void abrirPdf(acta.numeroActa)}>
                        Imprimir
                      </Button>
                      <Link to={`/actas/${acta.numeroActa}`} className="btn btn-sm btn-outline-secondary">
                        Modificar
                      </Link>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
          <Paginador pagina={pagina} total={listado.data?.total ?? 0} tamano={listado.data?.tamano ?? tamanoListado} alCambiar={setPagina} />
        </div>
      )}
    </section>
  )
}

function EncabezadoOrden({
  campo,
  activo,
  direccion,
  alOrdenar,
  children,
}: {
  campo: CampoOrden
  activo: CampoOrden
  direccion: Direccion
  alOrdenar: (campo: CampoOrden) => void
  children: string
}) {
  const seleccionado = activo === campo
  return (
    <th aria-sort={seleccionado ? (direccion === 'asc' ? 'ascending' : 'descending') : 'none'}>
      <button type="button" className="actas-orden" onClick={() => alOrdenar(campo)}>
        {children}
        <span aria-hidden="true">{seleccionado ? (direccion === 'asc' ? '▲' : '▼') : '↕'}</span>
      </button>
    </th>
  )
}
