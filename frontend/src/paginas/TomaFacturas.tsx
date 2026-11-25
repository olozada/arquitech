import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Row, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { Selector } from '../actas/SelectorCatalogo'
import { useCatalogo } from '../actas/useCatalogo'
import { dinero, numero as leerNumero } from '../actas/formato'
import { cliente, mensajeError } from '../api/cliente'
import type { Factura, FacturaSolicitud, LineaFacturaSolicitud } from '../api/facturas'
import { listarTodo } from '../api/pagina'
import { useSesion } from '../auth/contextoSesion'
import { esAdministrador } from '../auth/perfiles'

type LineaLocal = LineaFacturaSolicitud & { clave: string; construccion: string; insumo: string; unidad: string }

type PrecioProveedor = { id: number; descripcion: string; precio: number }

const hoy = () => new Date().toISOString().slice(0, 10)

export function TomaFacturas() {
  const { id } = useParams()
  const facturaId = Number(id)
  const editando = Number.isInteger(facturaId) && facturaId > 0
  const { sesion } = useSesion()
  const administrador = esAdministrador(sesion?.tipoUsuario)
  const navegar = useNavigate()
  const proveedores = useCatalogo('proveedores')
  const construcciones = useCatalogo('construcciones')
  const insumos = useQuery({
    queryKey: ['insumos-factura'],
    queryFn: async () => {
      const data = await listarTodo<{
        id: number
        codigo: string | null
        descripcion: string
        unidadCompra: string
        unidadUso: string
        activo: boolean
      }>('/api/insumos')
      return data.filter((insumo) => insumo.activo)
    },
  })
  const factura = useQuery({
    queryKey: ['factura', facturaId],
    enabled: editando,
    queryFn: async () => (await cliente.get<Factura>(`/api/facturas/${facturaId}`)).data,
    staleTime: Infinity,
  })
  const [numero, setNumero] = useState('')
  const [fechaValor, setFechaValor] = useState(hoy)
  const [proveedorId, setProveedorId] = useState(0)
  const [ordenCompra, setOrdenCompra] = useState('')
  const [observacion, setObservacion] = useState('')
  const [lineas, setLineas] = useState<LineaLocal[]>([])
  const [rowVersion, setRowVersion] = useState<string | null>(null)
  const [guardando, setGuardando] = useState(false)
  const [linea, setLinea] = useState(lineaVacia)
  const precios = useQuery({
    queryKey: ['proveedor-materiales', proveedorId, 'agregados'],
    enabled: proveedorId > 0,
    queryFn: async () => (await cliente.get<PrecioProveedor[]>(`/api/proveedores/${proveedorId}/materiales`)).data,
  })
  useEffect(() => {
    const cargada = factura.data
    if (!cargada) {
      return
    }

    setNumero(cargada.numero)
    setFechaValor(cargada.fecha.slice(0, 10))
    setProveedorId(cargada.proveedorId)
    setOrdenCompra(cargada.ordenCompra ?? '')
    setObservacion(cargada.observacion ?? '')
    setRowVersion(cargada.rowVersion)
    setLineas(cargada.detalles.map((detalle) => ({
      clave: String(detalle.id),
      construccionId: detalle.construccionId,
      construccion: detalle.construccion,
      insumoId: detalle.insumoId,
      insumo: detalle.codigoInsumo ? `${detalle.codigoInsumo} ${detalle.insumo}` : detalle.insumo,
      unidadMedidaId: detalle.unidadMedidaId,
      unidad: detalle.unidadMedida,
      cantidad: detalle.cantidad,
      precio: detalle.precio,
      descripcion: detalle.descripcion,
    })))
  }, [factura.data])

  const total = useMemo(
    () => lineas.reduce((suma, item) => suma + item.cantidad * item.precio, 0),
    [lineas],
  )
  const soloLectura = editando && !administrador
  const opcionesInsumo = (insumos.data ?? []).map((insumo) => ({
    id: insumo.id,
    descripcion: insumo.codigo ? `${insumo.codigo} ${insumo.descripcion}` : insumo.descripcion,
    unidad: (insumo.unidadCompra || insumo.unidadUso || '').trim(),
    activo: true,
  }))
  const insumoElegido = opcionesInsumo.find((item) => item.id === Number(linea.insumoId))

  function agregarLinea() {
    const cantidad = leerNumero(String(linea.cantidad))
    const precio = leerNumero(String(linea.precio))
    const construccion = (construcciones.data ?? []).find((item) => item.id === Number(linea.construccionId))
    const insumo = opcionesInsumo.find((item) => item.id === Number(linea.insumoId))
    if (!construccion || !insumo || cantidad == null || cantidad <= 0 || precio == null || precio < 0) {
      void Swal.fire({ icon: 'warning', title: 'Falta información', text: 'Indique construcción, insumo, cantidad y precio.' })
      return
    }

    setLineas((actuales) => [
      ...actuales,
      {
        clave: crypto.randomUUID(),
        construccionId: construccion.id,
        construccion: String(construccion.descripcion ?? ''),
        insumoId: insumo.id,
        insumo: insumo.descripcion,
        unidadMedidaId: null,
        unidad: insumo.unidad,
        cantidad,
        precio,
        descripcion: linea.descripcion.trim(),
      },
    ])
    setLinea(lineaVacia())
  }

  function elegirInsumo(insumoId: number) {
    const precioLista = (precios.data ?? []).find((item) => item.id === insumoId)?.precio
    setLinea((actual) => ({
      ...actual,
      insumoId: String(insumoId),
      precio: precioLista != null && Number(actual.precio) === 0 ? String(precioLista) : actual.precio,
    }))
  }

  async function guardar() {
    const solicitud: FacturaSolicitud = {
      numero: numero.trim(),
      fecha: fechaValor,
      proveedorId,
      ordenCompra: ordenCompra.trim() || null,
      observacion: observacion.trim() || null,
      rowVersion,
      detalles: lineas.map((item) => ({
        construccionId: item.construccionId,
        insumoId: item.insumoId,
        unidadMedidaId: item.unidadMedidaId,
        cantidad: item.cantidad,
        precio: item.precio,
        descripcion: item.descripcion,
      })),
    }
    setGuardando(true)
    try {
      if (editando) {
        await cliente.put(`/api/facturas/${facturaId}`, solicitud)
      } else {
        const { data } = await cliente.post<Factura>('/api/facturas', solicitud)
        await Swal.fire({ icon: 'success', title: 'Factura registrada', text: `Factura ${data.numero}` })
        navegar(`/facturas/${data.id}`)
        return
      }

      await Swal.fire({ icon: 'success', title: 'Factura guardada' })
      await factura.refetch()
    } catch (error) {
      await Swal.fire({ icon: 'error', title: 'No se pudo guardar', text: mensajeError(error) })
    } finally {
      setGuardando(false)
    }
  }

  if (editando && factura.isPending) {
    return <p>Cargando factura…</p>
  }

  if (editando && factura.isError) {
    return (
      <section>
        <p><Link to="/facturas">Volver a facturas</Link></p>
        <p>{mensajeError(factura.error)}</p>
      </section>
    )
  }

  return (
    <section>
      <p className="mb-2"><Link to="/facturas">Volver a facturas</Link></p>
      <h1>{editando ? `Factura ${numero || factura.data?.numero}` : 'Nueva factura de materiales'}</h1>
      <div className="catalogo-panel">
        <Row className="g-3">
          <Col md={3}>
            <Form.Group controlId="factura-numero">
              <Form.Label>N° Factura</Form.Label>
              <Form.Control value={numero} maxLength={30} disabled={soloLectura} onChange={(evento) => setNumero(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="factura-fecha">
              <Form.Label>Fecha</Form.Label>
              <Form.Control type="date" value={fechaValor} disabled={soloLectura} onChange={(evento) => setFechaValor(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Selector id="factura-proveedor" etiqueta="Proveedor" valor={proveedorId} opciones={proveedores.data} campo="nombre" deshabilitado={soloLectura} alCambiar={setProveedorId} />
          </Col>
          <Col md={3}>
            <Form.Group controlId="factura-orden">
              <Form.Label>Orden de compra</Form.Label>
              <Form.Control value={ordenCompra} maxLength={50} disabled={soloLectura} onChange={(evento) => setOrdenCompra(evento.target.value)} />
            </Form.Group>
          </Col>
        </Row>
      </div>
      {!soloLectura && (
        <div className="catalogo-panel mt-3">
          <Row className="g-3">
            <Col md={4}>
              <Selector id="linea-construccion" etiqueta="Construcción" valor={Number(linea.construccionId)} opciones={construcciones.data} campo="descripcion" alCambiar={(valor) => setLinea({ ...linea, construccionId: String(valor) })} />
            </Col>
            <Col md={4}>
              <Selector id="linea-insumo" etiqueta="Insumo" valor={Number(linea.insumoId)} opciones={opcionesInsumo} campo="descripcion" alCambiar={elegirInsumo} />
            </Col>
            <Col md={4}>
              <Form.Group controlId="linea-unidad">
                <Form.Label>Unidad</Form.Label>
                <Form.Control value={insumoElegido?.unidad ?? ''} disabled readOnly />
              </Form.Group>
            </Col>
            <Col md={3}>
              <Form.Group controlId="linea-cantidad">
                <Form.Label>Cantidad</Form.Label>
                <Form.Control value={linea.cantidad} inputMode="decimal" onChange={(evento) => setLinea({ ...linea, cantidad: evento.target.value })} />
              </Form.Group>
            </Col>
            <Col md={3}>
              <Form.Group controlId="linea-precio">
                <Form.Label>Precio</Form.Label>
                <Form.Control value={linea.precio} inputMode="decimal" onChange={(evento) => setLinea({ ...linea, precio: evento.target.value })} />
              </Form.Group>
            </Col>
            <Col md={6}>
              <Form.Group controlId="linea-descripcion">
                <Form.Label>Descripción</Form.Label>
                <Form.Control value={linea.descripcion} maxLength={500} onChange={(evento) => setLinea({ ...linea, descripcion: evento.target.value })} />
              </Form.Group>
            </Col>
          </Row>
          <Button type="button" className="mt-3" onClick={agregarLinea}>Agregar material</Button>
        </div>
      )}
      <div className="catalogo-panel mt-3">
        {lineas.length === 0 ? <p>No hay materiales.</p> : (
          <Table hover size="sm" className="mb-0">
            <thead>
              <tr>
                <th>Construcción</th>
                <th>Insumo</th>
                <th>UM</th>
                <th className="text-end">Cantidad</th>
                <th className="text-end">Precio</th>
                <th>Descripción</th>
                <th className="text-end">Subtotal</th>
                {!soloLectura && <th />}
              </tr>
            </thead>
            <tbody>
              {lineas.map((item) => (
                <tr key={item.clave}>
                  <td>{item.construccion}</td>
                  <td>{item.insumo}</td>
                  <td>{item.unidad}</td>
                  <td className="text-end">{item.cantidad}</td>
                  <td className="text-end">{dinero(item.precio, 2)}</td>
                  <td>{item.descripcion}</td>
                  <td className="text-end">{dinero(item.cantidad * item.precio, 2)}</td>
                  {!soloLectura && (
                    <td>
                      <Button type="button" size="sm" variant="outline-danger" onClick={() => setLineas(lineas.filter((actual) => actual.clave !== item.clave))}>
                        Quitar
                      </Button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </Table>
        )}
        <p className="total-acta">Total: {dinero(total, 2)}</p>
      </div>
      <div className="catalogo-panel mt-3">
        <Form.Group controlId="factura-comentario">
          <Form.Label>Comentarios</Form.Label>
          <Form.Control as="textarea" rows={3} maxLength={1000} value={observacion} disabled={soloLectura} onChange={(evento) => setObservacion(evento.target.value)} />
        </Form.Group>
        <div className="catalogo-acciones mt-3">
          {administrador && (
            <Button type="button" disabled={guardando} onClick={() => void guardar()}>
              {editando ? 'Guardar' : 'Registrar'}
            </Button>
          )}
          <Button type="button" variant="outline-secondary" onClick={() => navegar('/facturas')}>Cancelar</Button>
          {editando && (
            <Button type="button" variant="outline-primary" onClick={() => void abrirPdf(facturaId)}>Imprimir</Button>
          )}
        </div>
      </div>
    </section>
  )
}

function lineaVacia() {
  return { construccionId: '', insumoId: '', unidadMedidaId: '', cantidad: '', precio: '0', descripcion: '' }
}

async function abrirPdf(id: number) {
  const { data } = await cliente.get<Blob>(`/api/facturas/${id}/pdf`, { responseType: 'blob' })
  const url = URL.createObjectURL(new Blob([data], { type: 'application/pdf' }))
  window.open(url, '_blank', 'noopener')
  window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
}
