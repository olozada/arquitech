import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Col, Form, Row } from 'react-bootstrap'
import { Area, CartesianGrid, ComposedChart, Legend, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { cliente } from '../api/cliente'
import { dinero } from '../actas/formato'
import { usePrecioUnitario } from '../api/dashboard'

export function GraficoPrecio({ desde, hasta, estado }: { desde: string; hasta: string; estado: '' | 'abierta' | 'cerrada' }) {
  const [conceptoId, setConceptoId] = useState(0)
  const [insumoId, setInsumoId] = useState(0)
  const conceptos = useCatalogo('conceptos')
  const insumos = useQuery({
    queryKey: ['insumos-precio'],
    queryFn: async () =>
      (await cliente.get<Array<{ id: number; codigo: string | null; descripcion: string; activo: boolean }>>('/api/insumos')).data.filter(
        (insumo) => insumo.activo,
      ),
  })
  const serie = usePrecioUnitario(desde, hasta, conceptoId, insumoId, estado)
  const datos = (serie.data ?? []).map((punto) => ({
    ...punto,
    etiqueta: new Date(punto.anio, punto.mes - 1, 1).toLocaleDateString('es-CO', { month: 'short', year: '2-digit' }),
  }))

  return (
    <section className="catalogo-panel h-100">
      <h2>Evolución del precio unitario</h2>
      <Row className="g-2 mb-3">
        <Col md={6}>
          <Form.Label>Concepto</Form.Label>
          <Form.Select value={conceptoId} onChange={(evento) => setConceptoId(Number(evento.target.value))}>
            <option value={0}>Todos</option>
            {(conceptos.data ?? []).map((opcion) => (
              <option key={opcion.id} value={opcion.id}>
                {String(opcion.descripcion ?? opcion.nombre ?? opcion.id)}
              </option>
            ))}
          </Form.Select>
        </Col>
        <Col md={6}>
          <Form.Label>Insumo</Form.Label>
          <Form.Select value={insumoId} onChange={(evento) => setInsumoId(Number(evento.target.value))}>
            <option value={0}>Todos</option>
            {(insumos.data ?? []).map((opcion) => (
              <option key={opcion.id} value={opcion.id}>
                {opcion.codigo ? `${opcion.codigo} ${opcion.descripcion}` : opcion.descripcion}
              </option>
            ))}
          </Form.Select>
        </Col>
      </Row>
      {conceptoId === 0 && insumoId === 0 ? (
        <p className="kpi-subtitulo">Selecciona un concepto o un insumo.</p>
      ) : serie.isLoading ? (
        <p className="kpi-subtitulo">Cargando precios…</p>
      ) : datos.length === 0 ? (
        <p className="kpi-subtitulo">No hay precios en el rango.</p>
      ) : (
        <div className="grafico-alto">
          <ResponsiveContainer width="100%" height="100%">
            <ComposedChart data={datos}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="etiqueta" />
              <YAxis tickFormatter={(valor: number) => dinero(valor)} width={80} />
              <Tooltip formatter={(valor, nombre) => [dinero(Number(valor)), nombre]} />
              <Legend />
              <Area dataKey="maximo" name="Máximo" stroke="none" fill="#1abb9c33" />
              <Area dataKey="minimo" name="Mínimo" stroke="none" fill="#ffffff" />
              <Line dataKey="promedio" name="Promedio" stroke="#1abb9c" strokeWidth={2} dot={false} />
            </ComposedChart>
          </ResponsiveContainer>
        </div>
      )}
    </section>
  )
}

function useCatalogo(recurso: string) {
  return useQuery({
    queryKey: ['catalogo', recurso],
    queryFn: async () =>
      (await cliente.get<Array<{ id: number; descripcion?: string; nombre?: string }>>(`/api/${recurso}`)).data,
  })
}
