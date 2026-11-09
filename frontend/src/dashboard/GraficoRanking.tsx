import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { dinero } from '../actas/formato'
import type { Ranking } from '../api/dashboard'

type Propiedades = {
  titulo: string
  filas: Ranking[]
  alElegir?: (id: number) => void
}

export function GraficoRanking({ titulo, filas, alElegir }: Propiedades) {
  const datos = [...filas].reverse()

  return (
    <section className="catalogo-panel h-100">
      <h2>{titulo}</h2>
      {filas.length === 0 ? (
        <p className="kpi-subtitulo">No hay datos para este gráfico.</p>
      ) : (
        <div className="grafico-alto">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={datos} layout="vertical" margin={{ left: 16, right: 16 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis type="number" tickFormatter={(valor: number) => dinero(valor)} />
              <YAxis type="category" dataKey="nombre" width={130} tick={{ fontSize: 12 }} />
              <Tooltip
                formatter={(valor, nombre) => [nombre === 'Actas' ? valor : dinero(Number(valor)), nombre]}
              />
              <Bar
                dataKey="valor"
                name="Valor"
                fill="#1abb9c"
                onClick={(dato: { payload?: Ranking }) => {
                  if (alElegir && dato.payload && dato.payload.id > 0) {
                    alElegir(dato.payload.id)
                  }
                }}
              />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
    </section>
  )
}
