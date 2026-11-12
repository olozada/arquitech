import { Button, Col, Form, Row } from 'react-bootstrap'
import type { RegistroCatalogo } from '../catalogos/definiciones'
import { Selector } from './SelectorCatalogo'

export function EncabezadoActa({
  fechaInicial,
  fechaFinal,
  arquitectoId,
  revisorId,
  contratistaId,
  cerrada,
  editando,
  administrador,
  revisor,
  arquitectos,
  revisores,
  contratistas,
  alFechaInicial,
  alFechaFinal,
  alArquitecto,
  alRevisor,
  alContratista,
  alAgregar,
}: {
  fechaInicial: string
  fechaFinal: string
  arquitectoId: number
  revisorId: number
  contratistaId: number
  cerrada: boolean
  editando: boolean
  administrador: boolean
  revisor: boolean
  arquitectos: RegistroCatalogo[] | undefined
  revisores: RegistroCatalogo[] | undefined
  contratistas: RegistroCatalogo[] | undefined
  alFechaInicial: (valor: string) => void
  alFechaFinal: (valor: string) => void
  alArquitecto: (id: number) => void
  alRevisor: (id: number) => void
  alContratista: (id: number) => void
  alAgregar: () => void
}) {
  const bloqueado = cerrada || (editando && !administrador)
  return (
    <div className="catalogo-panel">
      <Row className="g-3">
        <Col md={6}>
          <FormFecha id="acta-fecha-inicial" etiqueta="Fecha inicial" valor={fechaInicial} deshabilitado={bloqueado} alCambiar={alFechaInicial} />
        </Col>
        <Col md={6}>
          <FormFecha id="acta-fecha-final" etiqueta="Fecha final" valor={fechaFinal} deshabilitado={bloqueado} alCambiar={alFechaFinal} />
        </Col>
        <Col md={4}>
          <Selector id="acta-arquitecto" etiqueta="Arquitecto" valor={arquitectoId} opciones={arquitectos} campo="nombre" deshabilitado={bloqueado} alCambiar={alArquitecto} />
        </Col>
        <Col md={4}>
          <Selector id="acta-revisor" etiqueta="Revisor" valor={revisorId} opciones={revisores} campo="nombre" deshabilitado={bloqueado} alCambiar={alRevisor} />
        </Col>
        <Col md={4}>
          <Selector id="acta-contratista" etiqueta="Contratista" valor={contratistaId} opciones={contratistas} campo="nombre" deshabilitado={bloqueado} alCambiar={alContratista} />
        </Col>
      </Row>
      {!revisor && !cerrada && (
        <Button type="button" className="mt-3" onClick={alAgregar}>
          Agregar ítems
        </Button>
      )}
    </div>
  )
}

function FormFecha({ id, etiqueta, valor, deshabilitado, alCambiar }: { id: string; etiqueta: string; valor: string; deshabilitado: boolean; alCambiar: (valor: string) => void }) {
  return (
    <Form.Group controlId={id}>
      <Form.Label>{etiqueta}</Form.Label>
      <Form.Control type="date" value={valor} disabled={deshabilitado} onChange={(evento) => alCambiar(evento.target.value)} />
    </Form.Group>
  )
}
