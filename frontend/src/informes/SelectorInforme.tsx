import { Form } from 'react-bootstrap'
import type { RegistroCatalogo } from '../catalogos/definiciones'

export function SelectorInforme({
  valor,
  opciones,
  alCambiar,
}: {
  valor: number
  opciones: RegistroCatalogo[]
  alCambiar: (id: number) => void
}) {
  return (
    <Form.Select value={valor} onChange={(evento) => alCambiar(Number(evento.target.value))}>
      <option value={0}>Todos</option>
      {opciones.map((opcion) => (
        <option key={opcion.id} value={opcion.id}>
          {String(opcion.nombre ?? opcion.descripcion ?? opcion.id)}
        </option>
      ))}
    </Form.Select>
  )
}
