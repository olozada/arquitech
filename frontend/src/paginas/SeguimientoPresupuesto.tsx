import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Form, Table } from 'react-bootstrap'
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { cliente } from '../api/cliente'
import type { Seguimiento } from '../api/presupuesto'
import { dinero as formatoDinero } from '../actas/formato'

function dinero(valor: number) {
  return formatoDinero(valor, 2)
}

export function SeguimientoPresupuesto() {
  const presupuestoId = Number(useParams().id)
  const [capituloId, setCapituloId] = useState(0)
  const consulta = useQuery({
    queryKey: ['seguimiento', presupuestoId],
    enabled: presupuestoId > 0,
    queryFn: async () => (await cliente.get<Seguimiento>(`/api/presupuestos/${presupuestoId}/seguimiento`)).data,
  })
  const datos = consulta.data
  const items = useMemo(
    () => (datos?.items ?? []).filter((item) => capituloId === 0 || item.capituloId === capituloId),
    [datos, capituloId],
  )
  if (consulta.isError) {
    return <p>No se pudo abrir el seguimiento.</p>
  }

  if (!datos) {
    return <p>Cargando seguimiento…</p>
  }

  return (
    <section>
      <div className="d-flex justify-content-between mb-3">
        <h1 className="mb-0">Seguimiento · {datos.construccion}</h1>
        <Link to={`/presupuestos/${presupuestoId}`}>Volver al presupuesto</Link>
      </div>
      <div className="catalogo-panel mb-3">
        <p>Presupuestado {dinero(datos.resumen.total)}</p>
        <p>Ejecutado {dinero(datos.ejecutado)} · Proyección al cierre {dinero(datos.proyeccion)}</p>
        <p>Ejecutado no presupuestado {dinero(datos.noPresupuestado)}</p>
      </div>
      <div className="catalogo-panel grafico-alto mb-3">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={datos.acumuladoMensual.map((punto) => ({ ...punto, etiqueta: `${punto.mes}/${punto.anio}` }))}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="etiqueta" />
            <YAxis />
            <Tooltip formatter={(valor) => dinero(Number(valor))} />
            <Line dataKey="acumulado" name="Ejecutado acumulado" stroke="#0f766e" strokeWidth={2} dot={false} />
          </LineChart>
        </ResponsiveContainer>
      </div>
      <Form.Group className="mb-3" style={{ maxWidth: '20rem' }}>
        <Form.Label>Capítulo</Form.Label>
        <Form.Select value={capituloId} onChange={(evento) => setCapituloId(Number(evento.target.value))}>
          <option value={0}>Todos</option>
          {datos.capitulos.map((capitulo) => (
            <option key={capitulo.capituloId} value={capitulo.capituloId ?? 0}>
              {capitulo.codigo}. {capitulo.descripcion}
            </option>
          ))}
        </Form.Select>
      </Form.Group>
      <Tabla filas={items} />
      {datos.noPresupuestados.length > 0 && (
        <>
          <h2 className="h5 mt-4">Ejecutado no presupuestado</h2>
          <Tabla filas={datos.noPresupuestados} />
        </>
      )}
    </section>
  )
}

function Tabla({ filas }: { filas: Seguimiento['items'] }) {
  return (
    <div className="catalogo-panel">
      <Table responsive size="sm" className="mb-0">
        <thead>
          <tr>
            <th>Ítem</th>
            <th className="text-end">Cant. presup.</th>
            <th className="text-end">Cant. ejec.</th>
            <th className="text-end">Avance físico</th>
            <th className="text-end">Valor presup.</th>
            <th className="text-end">Valor ejec.</th>
            <th className="text-end">Saldo</th>
            <th className="text-end">Precio actas</th>
            <th className="text-end">Proyección</th>
          </tr>
        </thead>
        <tbody>
          {filas.map((fila) => (
            <tr key={`${fila.itemId ?? fila.codigo}-${fila.descripcion}`}>
              <td>
                {fila.codigo} {fila.descripcion}
                <div className="progress mt-1" style={{ height: '0.4rem' }}>
                  <div className="progress-bar" style={{ width: `${Math.min(100, fila.avanceFisico * 100)}%`, background: '#1abb9c' }} />
                </div>
              </td>
              <td className="text-end">{fila.cantidadPresupuestada.toLocaleString('es-CO')}</td>
              <td className="text-end">{fila.cantidadEjecutada.toLocaleString('es-CO')}</td>
              <td className="text-end">{(fila.avanceFisico * 100).toLocaleString('es-CO', { maximumFractionDigits: 1 })} %</td>
              <td className="text-end">{dinero(fila.valorPresupuestado)}</td>
              <td className="text-end">{dinero(fila.valorEjecutado)}</td>
              <td className="text-end">{dinero(fila.saldo)}</td>
              <td className="text-end">{dinero(fila.precioActas)}</td>
              <td className="text-end">{dinero(fila.proyeccionCierre)}</td>
            </tr>
          ))}
        </tbody>
      </Table>
    </div>
  )
}
