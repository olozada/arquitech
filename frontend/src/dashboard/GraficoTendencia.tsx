import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { dinero } from '../actas/formato'
import type { PuntoMensual } from '../api/dashboard'

export function GraficoTendencia({ puntos }: { puntos: PuntoMensual[] }) {
  const datos = puntos.map((punto) => ({
    ...punto,
    etiqueta: etiquetaMes(punto.anio, punto.mes),
  }))

  return (
    <section className="catalogo-panel">
      <h2>Tendencia mensual</h2>
      <div className="grafico-alto">
        <ResponsiveContainer width="100%" height="100%">
          <ComposedChart data={datos}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="etiqueta" />
            <YAxis yAxisId="valor" tickFormatter={(valor: number) => dinero(valor)} width={90} />
            <YAxis yAxisId="actas" orientation="right" allowDecimals={false} />
            <Tooltip formatter={(valor, nombre) => [nombre === 'Actas' ? valor : dinero(Number(valor)), nombre]} />
            <Legend />
            <Bar yAxisId="valor" dataKey="valor" name="Valor ejecutado" fill="#1abb9c" />
            <Line yAxisId="actas" dataKey="actas" name="Actas" stroke="#23323c" strokeWidth={2} dot={false} />
            <Line
              yAxisId="valor"
              dataKey="valorAnioAnterior"
              name="Año anterior"
              stroke="#f0ad4e"
              strokeDasharray="6 4"
              dot={false}
            />
          </ComposedChart>
        </ResponsiveContainer>
      </div>
    </section>
  )
}

function etiquetaMes(anio: number, mes: number) {
  return new Date(anio, mes - 1, 1).toLocaleDateString('es-CO', { month: 'short', year: '2-digit' })
}
