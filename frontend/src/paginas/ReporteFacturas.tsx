import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Row, Table } from 'react-bootstrap'
import { cliente, mensajeDeError, mensajeError } from '../api/cliente'
import type { InformeFacturas } from '../api/facturas'
import { dinero, fecha, hoyIso, inicioDeMesIso } from '../actas/formato'
import { avisar, fechasInvalidas, useCatalogo } from '../informes/comun'
import { Paginador } from '../componentes/Paginador'

type FiltroFacturas = {
  usarFechas: boolean
  desde: string
  hasta: string
  construccionId: number
  proveedorId: number
  vista: 'agrupado' | 'detallado'
}

const tamano = 20

export function ReporteFacturas() {
  const [usarFechas, setUsarFechas] = useState(false)
  const [desde, setDesde] = useState(inicioDeMesIso)
  const [hasta, setHasta] = useState(hoyIso)
  const [construccionId, setConstruccionId] = useState(0)
  const [proveedorId, setProveedorId] = useState(0)
  const [vista, setVista] = useState<'agrupado' | 'detallado'>('agrupado')
  const [pagina, setPagina] = useState(1)
  const [filtro, setFiltro] = useState<FiltroFacturas | null>(null)
  const [exportando, setExportando] = useState(false)
  const [imprimiendo, setImprimiendo] = useState(false)
  const construcciones = useCatalogo('construcciones')
  const proveedores = useCatalogo('proveedores')

  function criterioActual() {
    return { usarFechas, desde, hasta, construccionId, proveedorId, vista }
  }

  const reporte = useQuery({
    queryKey: ['reporte-facturas', filtro, pagina],
    enabled: filtro != null,
    queryFn: async () => {
      const { data } = await cliente.get<InformeFacturas>('/api/facturas/items', {
        params: parametros(filtro!, pagina),
      })
      return data
    },
  })

  async function consultar(evento: FormEvent) {
    evento.preventDefault()
    if (fechasInvalidas(usarFechas, desde, hasta)) {
      await avisar('La fecha final debe ser mayor o igual que la fecha inicial.')
      return
    }

    setPagina(1)
    setFiltro(criterioActual())
  }

  async function exportar() {
    setExportando(true)
    try {
      const { data } = await cliente.get<Blob>('/api/facturas/items/excel', {
        responseType: 'blob',
        params: parametros(criterioActual(), 1),
      })
      const url = URL.createObjectURL(data)
      const enlace = document.createElement('a')
      enlace.href = url
      enlace.download = 'ReporteFacturas.xlsx'
      enlace.click()
      URL.revokeObjectURL(url)
    } catch (error) {
      await avisar(await mensajeDeError(error))
    } finally {
      setExportando(false)
    }
  }

  async function imprimir() {
    setImprimiendo(true)
    try {
      const { data } = await cliente.get<Blob>('/api/facturas/items/informe', {
        responseType: 'blob',
        params: parametros(criterioActual(), 1),
      })
      const url = URL.createObjectURL(new Blob([data], { type: 'application/pdf' }))
      window.open(url, '_blank', 'noopener')
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
    } catch (error) {
      await avisar(await mensajeDeError(error))
    } finally {
      setImprimiendo(false)
    }
  }

  const items = reporte.data?.items ?? []

  return (
    <section>
      <h1>Reporte de facturas</h1>
      <p>
        <Link to="/informes/materiales">Materiales registrados en actas</Link>
      </p>
      <Form className="catalogo-panel reporte-filtros" onSubmit={(evento) => void consultar(evento)}>
        <Row className="g-3 align-items-end">
          <Col xs={12} sm="auto">
            <Form.Check
              id="facturas-usar-fechas"
              type="checkbox"
              label="Filtrar por fecha"
              checked={usarFechas}
              onChange={(evento) => setUsarFechas(evento.target.checked)}
            />
          </Col>
          <Col md={3}>
            <Form.Group controlId="facturas-informe-desde">
              <Form.Label>Desde</Form.Label>
              <Form.Control type="date" value={desde} disabled={!usarFechas} onChange={(evento) => setDesde(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="facturas-informe-hasta">
              <Form.Label>Hasta</Form.Label>
              <Form.Control type="date" value={hasta} disabled={!usarFechas} onChange={(evento) => setHasta(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="facturas-informe-obra">
              <Form.Label>Construcción</Form.Label>
              <Form.Select value={construccionId || ''} onChange={(evento) => setConstruccionId(Number(evento.target.value))}>
                <option value="">Todas</option>
                {(construcciones.data ?? []).map((obra) => (
                  <option key={obra.id} value={obra.id}>{String(obra.descripcion ?? '')}</option>
                ))}
              </Form.Select>
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="facturas-informe-proveedor">
              <Form.Label>Proveedor</Form.Label>
              <Form.Select value={proveedorId || ''} onChange={(evento) => setProveedorId(Number(evento.target.value))}>
                <option value="">Todos</option>
                {(proveedores.data ?? []).map((proveedor) => (
                  <option key={proveedor.id} value={proveedor.id}>{String(proveedor.nombre ?? '')}</option>
                ))}
              </Form.Select>
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="facturas-informe-vista">
              <Form.Label>Vista</Form.Label>
              <Form.Select value={vista} onChange={(evento) => setVista(evento.target.value === 'detallado' ? 'detallado' : 'agrupado')}>
                <option value="agrupado">Valor de factura acumulado</option>
                <option value="detallado">Valor de factura detallado</option>
              </Form.Select>
            </Form.Group>
          </Col>
        </Row>
        <div className="catalogo-acciones mt-3">
          <Button type="submit">Consultar</Button>
          <Button type="button" variant="outline-primary" disabled={exportando} onClick={() => void exportar()}>
            {exportando ? 'Exportando…' : 'Excel'}
          </Button>
          <Button type="button" variant="outline-primary" disabled={imprimiendo} onClick={() => void imprimir()}>
            {imprimiendo ? 'Generando…' : 'Imprimir'}
          </Button>
        </div>
      </Form>
      {reporte.isFetching && <p>Cargando informe…</p>}
      {reporte.isError && <p>{mensajeError(reporte.error)}</p>}
      {reporte.data && items.length === 0 && <p>No hay facturas con ese filtro.</p>}
      {items.length > 0 && (
        <div className="catalogo-panel table-responsive">
          <Table hover size="sm" className="mb-0">
            <thead>
              <tr>
                {filtro?.vista === 'detallado' && <th>Factura</th>}
                {filtro?.vista === 'detallado' && <th>Fecha</th>}
                <th>Construcción</th>
                <th>Insumo</th>
                <th>UM</th>
                <th className="text-end">Cantidad</th>
                <th className="text-end">Precio</th>
                <th className="text-end">Subtotal</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item, indice) => (
                <tr key={`${item.facturaId ?? 'g'}-${item.insumo}-${indice}`}>
                  {filtro?.vista === 'detallado' && <td>{item.numero}</td>}
                  {filtro?.vista === 'detallado' && <td>{item.fecha ? fecha(item.fecha) : ''}</td>}
                  <td>{item.construccion}</td>
                  <td>{item.codigo ? `${item.codigo} ${item.insumo}` : item.insumo}</td>
                  <td>{item.unidad}</td>
                  <td className="text-end">{item.cantidad}</td>
                  <td className="text-end">{dinero(item.precio, 2)}</td>
                  <td className="text-end">{dinero(item.subtotal, 2)}</td>
                </tr>
              ))}
            </tbody>
          </Table>
          <p className="mt-2 mb-0">Total: {dinero(reporte.data?.total ?? 0, 2)}</p>
          <Paginador pagina={pagina} total={reporte.data?.cantidad ?? 0} tamano={tamano} alCambiar={setPagina} />
        </div>
      )}
    </section>
  )
}

function parametros(filtro: FiltroFacturas, pagina: number) {
  return {
    desde: filtro.usarFechas ? filtro.desde : undefined,
    hasta: filtro.usarFechas ? filtro.hasta : undefined,
    construccionId: filtro.construccionId > 0 ? filtro.construccionId : undefined,
    proveedorId: filtro.proveedorId > 0 ? filtro.proveedorId : undefined,
    vista: filtro.vista,
    pagina,
    tamano,
  }
}
