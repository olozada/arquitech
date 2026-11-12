import { Form } from 'react-bootstrap'
import type { RegistroCatalogo } from '../catalogos/definiciones'

export function Selector({
  id,
  etiqueta,
  valor,
  opciones,
  campo,
  deshabilitado = false,
  alCambiar,
}: {
  id: string
  etiqueta: string
  valor: number
  opciones: RegistroCatalogo[] | undefined
  campo: 'nombre' | 'descripcion'
  deshabilitado?: boolean
  alCambiar: (id: number) => void
}) {
  return (
    <Form.Group className="mb-3" controlId={id}>
      <Form.Label>{etiqueta}</Form.Label>
      <Form.Select value={valor || ''} disabled={deshabilitado} onChange={(evento) => alCambiar(Number(evento.target.value))}>
        <option value="">Seleccione</option>
        {(opciones ?? []).map((opcion) => (
          <option key={opcion.id} value={opcion.id}>
            {String(opcion[campo] ?? '')}
          </option>
        ))}
      </Form.Select>
    </Form.Group>
  )
}
