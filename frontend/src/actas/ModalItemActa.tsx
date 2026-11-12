import { Button, Form, Modal } from 'react-bootstrap'
import type { RegistroCatalogo } from '../catalogos/definiciones'
import type { ItemPresupuestoActa, LineaFormulario } from './lineaActa'
import { Selector } from './SelectorCatalogo'

export function ModalItemActa({
  abierto,
  lineaClave,
  linea,
  construcciones,
  conceptos,
  unidades,
  insumos,
  itemsPresupuesto,
  alCerrar,
  alLinea,
  alGuardar,
  alEliminar,
}: {
  abierto: boolean
  lineaClave: string | null
  linea: LineaFormulario
  construcciones: RegistroCatalogo[] | undefined
  conceptos: RegistroCatalogo[] | undefined
  unidades: RegistroCatalogo[] | undefined
  insumos: RegistroCatalogo[] | undefined
  itemsPresupuesto: ItemPresupuestoActa[] | undefined
  alCerrar: () => void
  alLinea: (linea: LineaFormulario) => void
  alGuardar: () => void
  alEliminar: () => void
}) {
  return (
    <Modal show={abierto} onHide={alCerrar} centered>
      <Modal.Header closeButton>
        <Modal.Title>{lineaClave ? 'Modificar ítem' : 'Agregar ítem'}</Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <Selector id="linea-construccion" etiqueta="Construcción" valor={Number(linea.construccionId)} opciones={construcciones} campo="descripcion" alCambiar={(id) => alLinea({ ...linea, construccionId: id ? String(id) : '', presupuestoItemId: '' })} />
        {(itemsPresupuesto?.length ?? 0) > 0 && (
          <Form.Group className="mb-3" controlId="linea-presupuesto">
            <Form.Label>Ítem presupuesto</Form.Label>
            <Form.Select
              value={linea.presupuestoItemId}
              onChange={(evento) => {
                const id = Number(evento.target.value)
                const item = itemsPresupuesto?.find((fila) => fila.id === id)
                if (!item) {
                  alLinea({ ...linea, presupuestoItemId: '' })
                  return
                }
                alLinea({
                  ...linea,
                  presupuestoItemId: String(item.id),
                  conceptoId: item.conceptoId ? String(item.conceptoId) : linea.conceptoId,
                  unidadMedidaId: item.unidadMedidaId ? String(item.unidadMedidaId) : linea.unidadMedidaId,
                  descripcion: item.descripcion,
                  precio: item.precio == null ? linea.precio : String(item.precio),
                })
              }}
            >
              <option value="">Sin ligar</option>
              {itemsPresupuesto?.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.codigo} · {item.descripcion}
                </option>
              ))}
            </Form.Select>
          </Form.Group>
        )}
        <Selector id="linea-concepto" etiqueta="Concepto" valor={Number(linea.conceptoId)} opciones={conceptos} campo="descripcion" alCambiar={(id) => alLinea({ ...linea, conceptoId: id ? String(id) : '' })} />
        <Selector id="linea-um" etiqueta="Unidad de medida" valor={Number(linea.unidadMedidaId)} opciones={unidades} campo="nombre" alCambiar={(id) => alLinea({ ...linea, unidadMedidaId: id ? String(id) : '' })} />
        <Selector id="linea-material" etiqueta="Insumo" valor={Number(linea.insumoId)} opciones={insumos} campo="descripcion" alCambiar={(id) => alLinea({ ...linea, insumoId: id ? String(id) : '' })} />
        <Form.Group className="mb-3" controlId="linea-precio">
          <Form.Label>Precio</Form.Label>
          <Form.Control value={linea.precio} inputMode="decimal" onChange={(evento) => alLinea({ ...linea, precio: evento.target.value })} />
        </Form.Group>
        <Form.Group className="mb-3" controlId="linea-cantidad">
          <Form.Label>Cantidad</Form.Label>
          <Form.Control value={linea.cantidad} inputMode="decimal" onChange={(evento) => alLinea({ ...linea, cantidad: evento.target.value })} />
        </Form.Group>
        <Form.Group controlId="linea-descripcion">
          <Form.Label>Descripción</Form.Label>
          <Form.Control value={linea.descripcion} maxLength={500} onChange={(evento) => alLinea({ ...linea, descripcion: evento.target.value })} />
        </Form.Group>
      </Modal.Body>
      <Modal.Footer>
        {lineaClave == null ? (
          <Button type="button" onClick={alGuardar}>
            Agregar
          </Button>
        ) : (
          <>
            <Button type="button" onClick={alGuardar}>
              Modificar
            </Button>
            <Button type="button" variant="danger" onClick={alEliminar}>
              Eliminar
            </Button>
          </>
        )}
        <Button type="button" variant="outline-secondary" onClick={alCerrar}>
          Cancelar
        </Button>
      </Modal.Footer>
    </Modal>
  )
}
