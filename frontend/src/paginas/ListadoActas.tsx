import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { descartarBorradorActa } from './TomaActas'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Row, Table } from 'react-bootstrap'
import { cliente, mensajeError } from '../api/cliente'
import { useSesion } from '../auth/contextoSesion'
import { esAdministrador } from '../auth/perfiles'
import { abrirPdf } from '../api/archivos'
import { dinero, fecha } from '../actas/formato'

type ActaResumen = {
  numeroActa: number
  fechaInicial: string
  fechaFinal: string
  arquitecto: string
  revisor: string
  contratista: string
  observacion: string | null
  total: number
  cerrada: boolean
}

type Consulta = {
  actas: ActaResumen[]
  totalGeneral: number
}

type CampoOrden = 'numero' | 'fechaInicial' | 'fechaFinal' | 'arquitecto' | 'revisor' | 'contratista'
type Direccion = 'asc' | 'desc'

const ordenInicial: CampoOrden = 'numero'

export function ListadoActas() {
  const { sesion } = useSesion()
  const [numero, setNumero] = useState('')
  const [arquitecto, setArquitecto] = useState('')
  const [revisor, setRevisor] = useState('')
  const [contratista, setContratista] = useState('')
  const [desde, setDesde] = useState('')
  const [hasta, setHasta] = useState('')
  const [estado, setEstado] = useState('')
  const [campo, setCampo] = useState<CampoOrden>(ordenInicial)
  const [direccion, setDireccion] = useState<Direccion>('desc')
  const listado = useQuery({
    queryKey: ['actas'],
    queryFn: async () => (await cliente.get<Consulta>('/api/actas')).data,
  })
  const fechasInvalidas = Boolean(desde && hasta && hasta < desde)
  const actas = useMemo(
    () => ordenarActas(filtrarActas(listado.data?.actas ?? [], { numero, arquitecto, revisor, contratista, desde, hasta, estado, fechasInvalidas }), campo, direccion),
    [listado.data, numero, arquitecto, revisor, contratista, desde, hasta, estado, fechasInvalidas, campo, direccion],
  )
  const hayFiltro = Boolean(numero.trim() || arquitecto.trim() || revisor.trim() || contratista.trim() || desde || hasta || estado)

  function ordenar(siguiente: CampoOrden) {
    if (campo === siguiente) {
      setDireccion((actual) => (actual === 'asc' ? 'desc' : 'asc'))
      return
    }

    setCampo(siguiente)
    setDireccion('asc')
  }

  function limpiar() {
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
          <Link
            to="/actas/nueva"
            state={{ nueva: Date.now() }}
            className="btn btn-primary"
            onClick={() => descartarBorradorActa()}
          >
            Nueva acta
          </Link>
        )}
      </div>

      <Form className="catalogo-panel" onSubmit={(evento) => evento.preventDefault()}>
        <Row className="g-3">
          <Col md={3}>
            <Form.Group controlId="actas-numero">
              <Form.Label>N° Acta</Form.Label>
              <Form.Control value={numero} inputMode="numeric" placeholder="Número" onChange={(evento) => setNumero(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-arquitecto">
              <Form.Label>Arquitecto</Form.Label>
              <Form.Control value={arquitecto} placeholder="Nombre" onChange={(evento) => setArquitecto(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-revisor">
              <Form.Label>Revisor</Form.Label>
              <Form.Control value={revisor} placeholder="Nombre" onChange={(evento) => setRevisor(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-contratista">
              <Form.Label>Contratista</Form.Label>
              <Form.Control value={contratista} placeholder="Nombre" onChange={(evento) => setContratista(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-desde">
              <Form.Label>Fecha desde</Form.Label>
              <Form.Control type="date" value={desde} onChange={(evento) => setDesde(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-hasta">
              <Form.Label>Fecha hasta</Form.Label>
              <Form.Control type="date" value={hasta} onChange={(evento) => setHasta(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="actas-estado">
              <Form.Label>Estado</Form.Label>
              <Form.Select value={estado} onChange={(evento) => setEstado(evento.target.value)}>
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
      {listado.data && (listado.data.actas.length === 0 || actas.length === 0) && (
        <p>{listado.data.actas.length === 0 ? 'No hay actas.' : 'No hay actas con ese filtro.'}</p>
      )}
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

function filtrarActas(
  actas: ActaResumen[],
  filtro: { numero: string; arquitecto: string; revisor: string; contratista: string; desde: string; hasta: string; estado: string; fechasInvalidas: boolean },
) {
  const numero = filtro.numero.trim()
  const arquitecto = normalizar(filtro.arquitecto)
  const revisor = normalizar(filtro.revisor)
  const contratista = normalizar(filtro.contratista)
  return actas.filter((acta) => {
    const inicio = acta.fechaInicial.slice(0, 10)
    const fin = acta.fechaFinal.slice(0, 10)
    if (numero && !String(acta.numeroActa).includes(numero)) {
      return false
    }
    if (arquitecto && !normalizar(acta.arquitecto ?? '').includes(arquitecto)) {
      return false
    }
    if (revisor && !normalizar(acta.revisor ?? '').includes(revisor)) {
      return false
    }
    if (contratista && !normalizar(acta.contratista).includes(contratista)) {
      return false
    }
    if (!filtro.fechasInvalidas && filtro.desde && fin < filtro.desde) {
      return false
    }
    if (!filtro.fechasInvalidas && filtro.hasta && inicio > filtro.hasta) {
      return false
    }
    if (filtro.estado === 'abierta' && acta.cerrada) {
      return false
    }
    if (filtro.estado === 'cerrada' && !acta.cerrada) {
      return false
    }
    return true
  })
}

function ordenarActas(actas: ActaResumen[], campo: CampoOrden, direccion: Direccion) {
  const factor = direccion === 'asc' ? 1 : -1
  return [...actas].sort((a, b) => factor * compararActa(a, b, campo))
}

function compararActa(a: ActaResumen, b: ActaResumen, campo: CampoOrden) {
  if (campo === 'numero') {
    return a.numeroActa - b.numeroActa
  }
  if (campo === 'fechaInicial' || campo === 'fechaFinal') {
    return a[campo].localeCompare(b[campo])
  }
  return normalizar(a[campo]).localeCompare(normalizar(b[campo]), 'es')
}

function normalizar(valor: string) {
  return valor
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLocaleLowerCase('es')
}
