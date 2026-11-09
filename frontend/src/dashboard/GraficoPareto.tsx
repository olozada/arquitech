import { Bar, CartesianGrid, ComposedChart, Legend, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { dinero } from '../actas/formato'
import type { Ranking } from '../api/dashboard'

export function GraficoPareto({ filas, alElegir }: { filas: Ranking[]; alElegir: (id: number) => void }) {
  const tres = filas.slice(0, 3).reduce((suma, fila) => suma + fila.participacion, 0)

  return (
    <section className="catalogo-panel h-100">
      <h2>Concentración por contratista</h2>
      <p className="kpi-subtitulo">Los 3 primeros suman {tres.toLocaleString('es-CO', { maximumFractionDigits: 1 })} %.</p>
      {filas.length === 0 ? (
        <p className="kpi-subtitulo">No hay datos para este gráfico.</p>
      ) : (
        <div className="grafico-alto">
          <ResponsiveContainer width="100%" height="100%">
            <ComposedChart data={filas} margin={{ left: 8, right: 8, bottom: 48 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="nombre" interval={0} angle={-30} textAnchor="end" height={70} tick={{ fontSize: 11 }} />
              <YAxis yAxisId="valor" tickFormatter={(valor: number) => dinero(valor)} width={80} />
              <YAxis yAxisId="pct" orientation="right" domain={[0, 100]} unit="%" />
              <Tooltip formatter={(valor, nombre) => [nombre === 'Acumulado' ? `${valor} %` : dinero(Number(valor)), nombre]} />
              <Legend />
              <Bar
                yAxisId="valor"
                dataKey="valor"
                name="Valor"
                fill="#23323c"
                onClick={(dato: { payload?: Ranking }) => {
                  if (dato.payload && dato.payload.id > 0) {
                    alElegir(dato.payload.id)
                  }
                }}
              />
              <Line yAxisId="pct" dataKey="participacionAcumulada" name="Acumulado" stroke="#1abb9c" strokeWidth={2} />
            </ComposedChart>
          </ResponsiveContainer>
        </div>
      )}
    </section>
  )
}
