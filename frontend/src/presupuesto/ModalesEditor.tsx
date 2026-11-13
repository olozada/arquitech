import type { Dispatch, SetStateAction } from 'react'
import { Button, Form, Modal, Offcanvas, Table } from 'react-bootstrap'
import { dinero } from '../actas/formato'
import type { ItemPresupuesto, PresupuestoDetalle, Recalculo } from '../api/presupuesto'
import { EditorApu } from './EditorApu'
import { Mediciones, SelectorConcepto } from './MedicionesItem'
import { parcial } from './mediciones'
import { ModalMover } from './ModalMover'

type IndiceItem = { capitulo: number; item: number }
type PanelApu = { modo: 'crear'; capitulo: number; unidad: string } | { modo: 'editar'; apuId: number }

export function ModalesEditor({
  borrador, editable, administrador, medicionItem, setMedicionItem, cambiar, buscarApu, setBuscarApu,
  textoApu, setTextoApu, apus, elegidos, setElegidos, postAccion, panelApu, setPanelApu, aplicarRecalculo,
  mover, setMover, destinoCapitulo, setDestinoCapitulo, confirmarMovimiento, conceptoItem, setConceptoItem,
  textoConcepto, setTextoConcepto, recalculo, setRecalculo,
}: {
  borrador: PresupuestoDetalle
  editable: boolean
  administrador: boolean
  medicionItem: IndiceItem | null
  setMedicionItem: Dispatch<SetStateAction<IndiceItem | null>>
  cambiar: (capitulo: number, item: number, cambios: Partial<ItemPresupuesto>) => void
  buscarApu: number | null
  setBuscarApu: Dispatch<SetStateAction<number | null>>
  textoApu: string
  setTextoApu: Dispatch<SetStateAction<string>>
  apus: { data?: Array<{ id: number; codigo: string | null; descripcion: string; unidad: string }> }
  elegidos: number[]
  setElegidos: Dispatch<SetStateAction<number[]>>
  postAccion: (ruta: string, cuerpo: unknown) => Promise<void>
  panelApu: PanelApu | null
  setPanelApu: Dispatch<SetStateAction<PanelApu | null>>
  aplicarRecalculo: () => Promise<void>
  mover: { itemId: number; capituloId: number } | null
  setMover: Dispatch<SetStateAction<{ itemId: number; capituloId: number } | null>>
  destinoCapitulo: number
  setDestinoCapitulo: Dispatch<SetStateAction<number>>
  confirmarMovimiento: () => Promise<void>
  conceptoItem: IndiceItem | null
  setConceptoItem: Dispatch<SetStateAction<IndiceItem | null>>
  textoConcepto: string
  setTextoConcepto: Dispatch<SetStateAction<string>>
  recalculo: Recalculo | null
  setRecalculo: Dispatch<SetStateAction<Recalculo | null>>
}) {
  return (
    <>
      <Modal show={medicionItem !== null} onHide={() => setMedicionItem(null)} size="lg" centered>
        <Modal.Header closeButton>
          <Modal.Title>Mediciones</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {medicionItem && (
            <Mediciones
              item={borrador.capitulos[medicionItem.capitulo].items[medicionItem.item]}
              editable={editable}
              alCambiar={(mediciones) => {
                const cantidad = mediciones.reduce((suma, medicion) => suma + parcial(medicion.veces, medicion.largo, medicion.ancho, medicion.alto), 0)
                cambiar(medicionItem.capitulo, medicionItem.item, { mediciones, cantidad: mediciones.length > 0 ? cantidad : borrador.capitulos[medicionItem.capitulo].items[medicionItem.item].cantidad })
              }}
            />
          )}
        </Modal.Body>
      </Modal>
      <Modal show={buscarApu !== null} onHide={() => setBuscarApu(null)} centered>
        <Modal.Header closeButton>
          <Modal.Title>Biblioteca de APU</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <Form.Control className="mb-2" placeholder="Buscar" value={textoApu} onChange={(evento) => setTextoApu(evento.target.value)} />
          {(apus.data ?? []).filter((apu) => `${apu.codigo ?? ''} ${apu.descripcion}`.toLowerCase().includes(textoApu.toLowerCase())).slice(0, 20).map((apu) => (
            <Form.Check
              key={apu.id}
              label={`${apu.codigo ?? ''} ${apu.descripcion} · ${apu.unidad}`}
              checked={elegidos.includes(apu.id)}
              onChange={() => setElegidos((actual) => actual.includes(apu.id) ? actual.filter((id) => id !== apu.id) : [...actual, apu.id])}
            />
          ))}
          <Button
            type="button"
            className="mt-3"
            disabled={elegidos.length === 0 || buscarApu === null}
            onClick={() => {
              const capitulo = borrador.capitulos[buscarApu ?? 0]
              void postAccion(`capitulos/${capitulo.id}/items/desde-apu`, { apuIds: elegidos, rowVersion: borrador.rowVersion })
              setBuscarApu(null)
            }}
          >
            Agregar
          </Button>
        </Modal.Body>
      </Modal>
      <Offcanvas show={panelApu !== null} onHide={() => setPanelApu(null)} placement="end" className="panel-lateral">
        <Offcanvas.Header closeButton>
          <Offcanvas.Title>{panelApu?.modo === 'editar' ? 'Editar APU' : 'Crear APU'}</Offcanvas.Title>
        </Offcanvas.Header>
        <Offcanvas.Body>
          {panelApu && (
            <EditorApu
              key={panelApu.modo === 'editar' ? panelApu.apuId : `nuevo-${panelApu.capitulo}`}
              apuId={panelApu.modo === 'editar' ? panelApu.apuId : null}
              unidadInicial={panelApu.modo === 'crear' ? panelApu.unidad : undefined}
              embebido
              alCancelar={() => setPanelApu(null)}
              alGuardar={(apu) => {
                const panel = panelApu
                setPanelApu(null)
                if (panel.modo === 'crear' && apu.costoTotal > 0) {
                  const capitulo = borrador.capitulos[panel.capitulo]
                  void postAccion(`capitulos/${capitulo.id}/items/desde-apu`, { apuIds: [apu.id], rowVersion: borrador.rowVersion })
                }
                if (panel.modo === 'editar' && borrador.estado === 'Borrador' && administrador) {
                  void aplicarRecalculo()
                }
              }}
            />
          )}
        </Offcanvas.Body>
      </Offcanvas>
      <ModalMover
        abierto={mover !== null}
        capitulos={borrador.capitulos}
        capituloId={mover?.capituloId}
        destino={destinoCapitulo}
        alDestino={setDestinoCapitulo}
        alConfirmar={() => void confirmarMovimiento()}
        alCerrar={() => setMover(null)}
      />
      <Modal show={conceptoItem !== null} onHide={() => setConceptoItem(null)} centered>
        <Modal.Header closeButton>
          <Modal.Title>Concepto</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <SelectorConcepto
            texto={textoConcepto}
            alBuscar={setTextoConcepto}
            alElegir={(conceptoId) => {
              if (conceptoItem) {
                cambiar(conceptoItem.capitulo, conceptoItem.item, { conceptoId })
              }
              setConceptoItem(null)
            }}
          />
        </Modal.Body>
      </Modal>
      <Modal show={recalculo !== null} onHide={() => setRecalculo(null)} size="lg" centered>
        <Modal.Header closeButton>
          <Modal.Title>{borrador.estado === 'Borrador' ? 'Recalcular' : '¿Cuánto costaría hoy?'}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {recalculo && (
            <>
              {borrador.estado !== 'Borrador' && <p>El presupuesto aprobado no cambia.</p>}
              <p>Total actual {dinero(recalculo.actual.total, 2)} · nuevo {dinero(recalculo.propuesto.total, 2)}</p>
              <Table size="sm">
                <thead>
                  <tr>
                    <th>Código</th>
                    <th>Descripción</th>
                    <th className="text-end">Actual</th>
                    <th className="text-end">Nuevo</th>
                    <th className="text-end">Diferencia</th>
                  </tr>
                </thead>
                <tbody>
                  {[...recalculo.diferencias].sort((a, b) => Math.abs(b.diferencia) - Math.abs(a.diferencia)).map((fila) => {
                    const actual = fila.materialActual + fila.manoObraActual
                    const nuevo = fila.materialNuevo + fila.manoObraNueva
                    const porcentaje = actual === 0 ? '' : `${((nuevo - actual) / actual * 100).toLocaleString('es-CO', { maximumFractionDigits: 1 })} %`
                    return (
                      <tr key={fila.codigo + fila.descripcion}>
                        <td>{fila.codigo}</td>
                        <td>{fila.descripcion}</td>
                        <td className="text-end">{dinero(actual, 2)}</td>
                        <td className="text-end">{dinero(nuevo, 2)}</td>
                        <td className="text-end">{dinero(fila.diferencia, 2)} {porcentaje}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </Table>
            </>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button type="button" variant="outline-secondary" onClick={() => setRecalculo(null)}>Cancelar</Button>
          {borrador.estado === 'Borrador' && administrador && (
            <Button type="button" onClick={() => void aplicarRecalculo()}>Aplicar</Button>
          )}
        </Modal.Footer>
      </Modal>
    </>
  )
}
