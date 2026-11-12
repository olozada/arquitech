import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Form, Modal, Nav, Table } from 'react-bootstrap'
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { tamanoListado } from '../api/pagina'
import type { Pagina } from '../api/pagina'
import { dinero } from '../actas/formato'
import { precioUnitarioInsumo } from '../presupuesto/calculadoraApu'
import { Paginador } from '../componentes/Paginador'

const tipos = ['Material', 'Equipo', 'Herramienta', 'Laboratorio', 'Transporte', 'ManoObraDestajo', 'Otro']

type Insumo = {
  id: number
  codigo: string | null
  descripcion: string
  tipo: string
  unidadCompra: string
  precioBase: number
  porcentajeIva: number
  cantidadPorUnidad: number
  unidadUso: string
  precioUnitario: number
  activo: boolean
}

type Historial = { id: number; precioBase: number; porcentajeIva: number; fecha: string; usuario: string | null }

const vacio = (): Insumo => ({
  id: 0,
  codigo: '',
  descripcion: '',
  tipo: 'Material',
  unidadCompra: '',
  precioBase: 0,
  porcentajeIva: 0.19,
  cantidadPorUnidad: 1,
  unidadUso: '',
  precioUnitario: 0,
  activo: true,
})

export function InsumosPresupuesto() {
  const [tipo, setTipo] = useState('Material')
  const [texto, setTexto] = useState('')
  const [pagina, setPagina] = useState(1)
  const [seleccionado, setSeleccionado] = useState<Insumo | null>(null)
  const [historial, setHistorial] = useState<Historial[] | null>(null)
  const consultas = useQueryClient()
  const lista = useQuery({
    queryKey: ['insumos', tipo, texto, pagina],
    queryFn: async () =>
      (await cliente.get<Pagina<Insumo>>('/api/insumos', {
        params: { tipo, texto: texto.trim() || undefined, pagina, tamano: tamanoListado },
      })).data,
  })
  const visibles = lista.data?.items ?? []

  async function guardar(insumo: Insumo) {
    try {
      if (insumo.id > 0) {
        await cliente.put(`/api/insumos/${insumo.id}`, insumo)
      } else {
        await cliente.post('/api/insumos', insumo)
      }
      await consultas.invalidateQueries({ queryKey: ['insumos'] })
      setSeleccionado(null)
    } catch (error) {
      await Swal.fire({ icon: 'error', title: 'No se pudo guardar', text: mensajeError(error) })
    }
  }

  async function desactivar(id: number) {
    try {
      await cliente.delete(`/api/insumos/${id}`)
      await consultas.invalidateQueries({ queryKey: ['insumos'] })
    } catch (error) {
      const textoError = mensajeError(error)
      if (!textoError.toLowerCase().includes('se usa')) {
        await Swal.fire({ icon: 'error', text: textoError })
        return
      }
      const confirmacion = await Swal.fire({ icon: 'warning', title: textoError, showCancelButton: true, confirmButtonColor: '#1abb9c' })
      if (!confirmacion.isConfirmed) {
        return
      }
      await cliente.delete(`/api/insumos/${id}`, { params: { confirmar: true } })
      await consultas.invalidateQueries({ queryKey: ['insumos'] })
    }
  }

  async function verHistorial(id: number) {
    const { data } = await cliente.get<Historial[]>(`/api/insumos/${id}/historial`)
    setHistorial(data)
  }

  async function exportar() {
    const { data } = await cliente.get<Blob>('/api/insumos/excel', { params: { tipo }, responseType: 'blob' })
    const url = URL.createObjectURL(data)
    const enlace = document.createElement('a')
    enlace.href = url
    enlace.download = `insumos-${tipo}.xlsx`
    enlace.click()
    URL.revokeObjectURL(url)
  }

  const precio = seleccionado
    ? precioUnitarioInsumo(seleccionado.precioBase, seleccionado.porcentajeIva, seleccionado.cantidadPorUnidad || 1)
    : 0

  return (
    <section>
      <div className="d-flex justify-content-between mb-3">
        <h1 className="mb-0">Insumos</h1>
        <div className="d-flex gap-2">
          <Button type="button" variant="outline-primary" onClick={() => void exportar()}>Exportar Excel</Button>
          <Button type="button" onClick={() => setSeleccionado({ ...vacio(), tipo })}>+ Nuevo insumo</Button>
        </div>
      </div>
      <Nav variant="tabs" activeKey={tipo} onSelect={(clave) => { if (clave) { setPagina(1); setTipo(clave) } }} className="mb-3">
        {tipos.map((item) => (
          <Nav.Item key={item}>
            <Nav.Link eventKey={item}>{item}{item === tipo ? ` (${lista.data?.total ?? 0})` : ''}</Nav.Link>
          </Nav.Item>
        ))}
      </Nav>
      <Form.Control className="mb-3" placeholder="Buscar por código o descripción" value={texto} onChange={(evento) => { setPagina(1); setTexto(evento.target.value) }} />
      <div className="catalogo-panel">
        <Table responsive hover size="sm" className="mb-0">
          <thead>
            <tr>
              <th>Código</th>
              <th>Descripción</th>
              <th>Unidad</th>
              <th className="text-end">Precio unitario</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {visibles.map((insumo) => (
              <tr key={insumo.id}>
                <td>{insumo.codigo}</td>
                <td>{insumo.descripcion}</td>
                <td>{insumo.unidadUso || insumo.unidadCompra}</td>
                <td className="text-end">{dinero(insumo.precioUnitario, 2)}</td>
                <td className="text-end text-nowrap">
                  <Button type="button" size="sm" variant="outline-primary" onClick={() => setSeleccionado({ ...insumo })}>Editar</Button>
                  <Button type="button" size="sm" variant="outline-secondary" className="ms-1" onClick={() => void verHistorial(insumo.id)}>Historial</Button>
                  <Button type="button" size="sm" variant="outline-danger" className="ms-1" onClick={() => void desactivar(insumo.id)}>Desactivar</Button>
                </td>
              </tr>
            ))}
          </tbody>
        </Table>
        <Paginador pagina={pagina} total={lista.data?.total ?? 0} tamano={lista.data?.tamano ?? tamanoListado} alCambiar={setPagina} />
      </div>
      {seleccionado && (
        <Form className="catalogo-panel mt-3" onSubmit={(evento) => { evento.preventDefault(); void guardar(seleccionado) }}>
          <div className="row g-2">
            <div className="col-md-2"><Form.Label>Código</Form.Label><Form.Control value={seleccionado.codigo ?? ''} onChange={(evento) => setSeleccionado({ ...seleccionado, codigo: evento.target.value })} /></div>
            <div className="col-md-4"><Form.Label>Descripción</Form.Label><Form.Control value={seleccionado.descripcion} onChange={(evento) => setSeleccionado({ ...seleccionado, descripcion: evento.target.value })} /></div>
            <div className="col-md-2">
              <Form.Label>Tipo</Form.Label>
              <Form.Select value={seleccionado.tipo} onChange={(evento) => setSeleccionado({ ...seleccionado, tipo: evento.target.value })}>
                {tipos.map((item) => <option key={item}>{item}</option>)}
              </Form.Select>
            </div>
            <div className="col-md-2"><Form.Label>Unidad compra</Form.Label><Form.Control value={seleccionado.unidadCompra} onChange={(evento) => setSeleccionado({ ...seleccionado, unidadCompra: evento.target.value })} /></div>
            <div className="col-md-2"><Form.Label>Unidad uso</Form.Label><Form.Control value={seleccionado.unidadUso} onChange={(evento) => setSeleccionado({ ...seleccionado, unidadUso: evento.target.value })} /></div>
            <div className="col-md-2"><Form.Label>Precio base</Form.Label><Form.Control value={seleccionado.precioBase} onChange={(evento) => setSeleccionado({ ...seleccionado, precioBase: Number(evento.target.value) || 0 })} /></div>
            <div className="col-md-2"><Form.Label>% IVA</Form.Label><Form.Control value={seleccionado.porcentajeIva} onChange={(evento) => setSeleccionado({ ...seleccionado, porcentajeIva: Number(evento.target.value) || 0 })} /></div>
            <div className="col-md-2"><Form.Label>Cantidad por unidad</Form.Label><Form.Control value={seleccionado.cantidadPorUnidad} onChange={(evento) => setSeleccionado({ ...seleccionado, cantidadPorUnidad: Number(evento.target.value) || 1 })} /></div>
          </div>
          <p className="mt-2">Precio unitario {dinero(precio, 2)}</p>
          <div className="d-flex gap-2">
            <Button type="submit">Guardar</Button>
            {seleccionado.id > 0 && <Button type="button" variant="outline-secondary" onClick={() => void tomarPrecio(seleccionado.id)}>Tomar precio de proveedores</Button>}
          </div>
        </Form>
      )}
      <Modal show={historial !== null} onHide={() => setHistorial(null)} size="lg" centered>
        <Modal.Header closeButton><Modal.Title>Historial de precio</Modal.Title></Modal.Header>
        <Modal.Body>
          <div style={{ height: 220 }}>
            <ResponsiveContainer>
              <LineChart data={[...(historial ?? [])].reverse().map((fila) => ({ fecha: fila.fecha.slice(0, 10), precio: fila.precioBase }))}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="fecha" />
                <YAxis />
                <Tooltip />
                <Line dataKey="precio" stroke="#1abb9c" dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
          <Table size="sm" className="mt-3">
            <thead><tr><th>Fecha</th><th>Precio base</th><th>IVA</th><th>Usuario</th></tr></thead>
            <tbody>
              {(historial ?? []).map((fila) => (
                <tr key={fila.id}>
                  <td>{new Date(fila.fecha).toLocaleString('es-CO')}</td>
                  <td>{dinero(fila.precioBase, 2)}</td>
                  <td>{fila.porcentajeIva}</td>
                  <td>{fila.usuario ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        </Modal.Body>
      </Modal>
    </section>
  )

  async function tomarPrecio(id: number) {
    const confirmacion = await Swal.fire({ icon: 'question', title: '¿Tomar el precio del proveedor más barato?', showCancelButton: true, confirmButtonColor: '#1abb9c' })
    if (!confirmacion.isConfirmed) {
      return
    }
    try {
      await cliente.post(`/api/insumos/${id}/precio-proveedor`)
      await consultas.invalidateQueries({ queryKey: ['insumos'] })
      setSeleccionado(null)
    } catch (error) {
      await Swal.fire({ icon: 'error', title: 'Sin precio de proveedor', text: mensajeError(error) })
    }
  }
}
