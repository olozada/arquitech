import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import { dinero } from '../actas/formato'
import type { Ranking } from '../api/dashboard'

const colores = ['#1abb9c', '#23323c', '#4aa3df', '#f0ad4e', '#d9534f', '#5bc0de', '#9b59b6', '#95a5a6', '#7f8c8d']

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
                  <Cell key={fila.id} fill={colores[indice % colores.length]} />
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
