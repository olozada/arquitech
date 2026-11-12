import { Button, Form, Modal } from 'react-bootstrap'

export function ModalMover({
  abierto,
  capitulos,
  capituloId,
  destino,
  alDestino,
  alConfirmar,
  alCerrar,
}: {
  abierto: boolean
  capitulos: Array<{ id: number; numero: number; nombre: string }>
  capituloId: number | undefined
  destino: number
  alDestino: (id: number) => void
  alConfirmar: () => void
  alCerrar: () => void
}) {
  return (
    <Modal show={abierto} onHide={alCerrar} centered>
      <Modal.Header closeButton>
        <Modal.Title>Mover ítem</Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <Form.Select value={destino} onChange={(evento) => alDestino(Number(evento.target.value))}>
          {capitulos.filter((capitulo) => capitulo.id !== capituloId && capitulo.id > 0).map((capitulo) => (
            <option key={capitulo.id} value={capitulo.id}>{capitulo.numero}. {capitulo.nombre}</option>
          ))}
        </Form.Select>
        <div className="d-flex gap-2 mt-3">
          <Button type="button" onClick={alConfirmar}>Mover</Button>
          <Button type="button" variant="outline-secondary" onClick={alCerrar}>Cancelar</Button>
        </div>
      </Modal.Body>
    </Modal>
  )
}
