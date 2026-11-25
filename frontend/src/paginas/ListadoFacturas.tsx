import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Row, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import type { ConsultaFacturas } from '../api/facturas'
import { tamanoListado } from '../api/pagina'
import { dinero, fecha } from '../actas/formato'
import { useSesion } from '../auth/contextoSesion'
import { esAdministrador } from '../auth/perfiles'
import { Paginador } from '../componentes/Paginador'

export function ListadoFacturas() {
  const { sesion } = useSesion()
  const navegar = useNavigate()
  const [numero, setNumero] = useState('')
  const [proveedor, setProveedor] = useState('')
  const [desde, setDesde] = useState('')
  const [hasta, setHasta] = useState('')
  const [pagina, setPagina] = useState(1)
  const fechasInvalidas = Boolean(desde && hasta && hasta < desde)
  const listado = useQuery({
    queryKey: ['facturas', numero, proveedor, desde, hasta, pagina],
    queryFn: async () =>
      (await cliente.get<ConsultaFacturas>('/api/facturas', {
        params: {
          numero: numero.trim() || undefined,
          nombreProveedor: proveedor.trim() || undefined,
          desde: !fechasInvalidas && desde ? desde : undefined,
          hasta: !fechasInvalidas && hasta ? hasta : undefined,
          pagina,
          tamano: tamanoListado,
        },
      })).data,
  })
  const facturas = listado.data?.items ?? []
  const hayFiltro = Boolean(numero.trim() || proveedor.trim() || desde || hasta)

  async function eliminar(id: number) {
    const confirmacion = await Swal.fire({
      icon: 'warning',
      title: '¿Desea eliminar la factura?',
      showCancelButton: true,
      confirmButtonText: 'Eliminar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#d9534f',
    })
    if (!confirmacion.isConfirmed) {
      return
    }

    try {
      await cliente.delete(`/api/facturas/${id}`)
      await listado.refetch()
    } catch (error) {
      await Swal.fire({ icon: 'error', title: 'No se pudo eliminar', text: mensajeError(error) })
    }
  }

  return (
    <section>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <h1 className="mb-0">Facturas de materiales</h1>
        {esAdministrador(sesion?.tipoUsuario) && (
          <Button type="button" onClick={() => navegar('/facturas/nueva')}>
            Nueva factura
          </Button>
        )}
      </div>
      <Form className="catalogo-panel" onSubmit={(evento) => evento.preventDefault()}>
        <Row className="g-3">
          <Col md={3}>
            <Form.Group controlId="facturas-numero">
              <Form.Label>N° Factura</Form.Label>
              <Form.Control value={numero} onChange={(evento) => { setPagina(1); setNumero(evento.target.value) }} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="facturas-proveedor">
              <Form.Label>Proveedor</Form.Label>
              <Form.Control value={proveedor} onChange={(evento) => { setPagina(1); setProveedor(evento.target.value) }} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="facturas-desde">
              <Form.Label>Fecha desde</Form.Label>
              <Form.Control type="date" value={desde} onChange={(evento) => { setPagina(1); setDesde(evento.target.value) }} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="facturas-hasta">
              <Form.Label>Fecha hasta</Form.Label>
              <Form.Control type="date" value={hasta} onChange={(evento) => { setPagina(1); setHasta(evento.target.value) }} />
            </Form.Group>
          </Col>
        </Row>
        <div className="catalogo-acciones mt-3">
          <Button
            type="button"
            variant="outline-secondary"
            disabled={!hayFiltro}
            onClick={() => {
              setPagina(1)
              setNumero('')
              setProveedor('')
              setDesde('')
              setHasta('')
            }}
          >
            Limpiar
          </Button>
        </div>
        {fechasInvalidas && <p className="text-danger mt-3 mb-0">La fecha hasta debe ser mayor o igual que la fecha desde.</p>}
      </Form>
      {listado.isPending && <p>Cargando facturas…</p>}
      {listado.isError && <p>{mensajeError(listado.error)}</p>}
      {listado.data && facturas.length === 0 && <p>No hay facturas con ese filtro.</p>}
      {facturas.length > 0 && (
        <div className="catalogo-panel table-responsive">
          <Table hover size="sm" className="catalogo-tabla align-middle mb-0">
            <thead>
              <tr>
                <th>N° Factura</th>
                <th>Fecha</th>
                <th>Proveedor</th>
                <th>Orden de compra</th>
                <th>Comentarios</th>
                <th className="text-end">Total</th>
                <th className="text-end">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {facturas.map((factura) => (
                <tr key={factura.id}>
                  <td><Link to={`/facturas/${factura.id}`}>{factura.numero}</Link></td>
                  <td className="text-nowrap">{fecha(factura.fecha)}</td>
                  <td>{factura.proveedor}</td>
                  <td>{factura.ordenCompra ?? ''}</td>
                  <td>{factura.observacion ?? ''}</td>
                  <td className="text-end text-nowrap">{dinero(factura.total)}</td>
                  <td>
                    <div className="actas-acciones">
                      <Button type="button" size="sm" variant="outline-primary" onClick={() => void abrirPdf(factura.id)}>
                        Imprimir
                      </Button>
                      <Link to={`/facturas/${factura.id}`} className="btn btn-sm btn-primary">
                        {esAdministrador(sesion?.tipoUsuario) ? 'Modificar' : 'Ver'}
                      </Link>
                      {esAdministrador(sesion?.tipoUsuario) && (
                        <Button type="button" size="sm" variant="outline-danger" onClick={() => void eliminar(factura.id)}>
                          Eliminar
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
          <p className="mt-2 mb-0">Total del filtro: {dinero(listado.data?.totalGeneral ?? 0)}</p>
          <Paginador pagina={pagina} total={listado.data?.total ?? 0} tamano={listado.data?.tamano ?? tamanoListado} alCambiar={setPagina} />
        </div>
      )}
    </section>
  )
}

async function abrirPdf(id: number) {
  const { data } = await cliente.get<Blob>(`/api/facturas/${id}/pdf`, { responseType: 'blob' })
  const url = URL.createObjectURL(new Blob([data], { type: 'application/pdf' }))
  window.open(url, '_blank', 'noopener')
  window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
}
