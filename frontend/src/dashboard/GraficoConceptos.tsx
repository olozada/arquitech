import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import { dinero } from '../actas/formato'
import type { Ranking } from '../api/dashboard'
import { coloresCategorias } from './colores'

export function GraficoConceptos({ filas }: { filas: Ranking[] }) {
  return (
    <section className="catalogo-panel h-100">
      <h2>Mezcla de costos por concepto</h2>
      {filas.length === 0 ? (
        <p className="kpi-subtitulo">No hay datos para este gráfico.</p>
      ) : (
        <div className="grafico-alto">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie data={filas} dataKey="valor" nameKey="nombre" innerRadius={55} outerRadius={90} paddingAngle={1}>
                {filas.map((fila, indice) => (
                  <Cell key={fila.id} fill={coloresCategorias[indice % coloresCategorias.length]} />
                ))}
              </Pie>
              <Tooltip formatter={(valor, nombre) => [dinero(Number(valor)), nombre]} />
              <Legend />
            </PieChart>
          </ResponsiveContainer>
        </div>
      )}
    </section>
  )
}
