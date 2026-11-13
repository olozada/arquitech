import { Link } from 'react-router-dom'
import { Button, Form } from 'react-bootstrap'
import { mensajeError } from '../api/cliente'
import { HistorialAuditoria } from '../componentes/HistorialAuditoria'
import { EncabezadoActa } from '../actas/EncabezadoActa'
import { imprimir } from '../actas/lineaFormulario'
import { ModalItemActa } from '../actas/ModalItemActa'
import { TablaItemsActa } from '../actas/TablaItemsActa'
import { useTomaActa } from '../actas/useTomaActa'

export function TomaActas() {
  const {
    errorCarga, cargando, editando, numeroRuta, conflicto, recargarActa, cerrada, administrador,
    pestana, setPestana, fechaInicial, fechaFinal, arquitectoId, revisorId, contratistaId, revisor,
    arquitectos, revisores, contratistas, setFechaInicial, setFechaFinal, setArquitectoId, setRevisorId,
    setContratistaId, abrirNuevaLinea, detalles, sesion, total, todosRechazados, hayRechazo, revisarLinea,
    abrirLinea, observacion, setObservacion, guardando, registrar, cambiarCierre, cancelar, numeroActa,
    modalAbierto, lineaClave, linea, construcciones, conceptos, unidades, insumos, itemsPresupuesto,
    setModalAbierto, setLinea, guardarLinea, eliminarLinea,
  } = useTomaActa()
  if (errorCarga) {
    return (
      <section>
        <p className="mb-2">
          <Link to="/actas">Volver a actas</Link>
        </p>
        <p>{mensajeError(errorCarga)}</p>
      </section>
    )
  }
  if (cargando) {
    return (
      <section>
        <p>Cargando acta…</p>
      </section>
    )
  }
  return (
    <section>
      <p className="mb-2">
        <Link to="/actas">Volver a actas</Link>
      </p>
      <h1>{editando ? `Acta N° ${numeroRuta}` : 'Toma de actas – Perfil Arquitecto'}</h1>
      {conflicto && (
        <p className="alert alert-warning" role="status">
          Otra persona modificó esta acta
          <Button type="button" size="sm" className="ms-2" variant="outline-dark" onClick={() => void recargarActa()}>
            Recargar
          </Button>
        </p>
      )}
      {cerrada && (
        <p className="alert alert-warning" role="status">
          El acta ya está cerrada. No se pueden agregar, modificar ni eliminar ítems.
        </p>
      )}
      {editando && administrador && (
        <div className="nav nav-tabs mb-3">
          <button type="button" className={pestana === 'acta' ? 'nav-link active' : 'nav-link'} onClick={() => setPestana('acta')}>
            Acta
          </button>
          <button type="button" className={pestana === 'historial' ? 'nav-link active' : 'nav-link'} onClick={() => setPestana('historial')}>
            Historial
          </button>
        </div>
      )}
      {pestana === 'historial' && editando ? (
        <HistorialAuditoria entidad="Acta" entidadId={String(numeroRuta)} />
      ) : (
      <>
      <EncabezadoActa fechaInicial={fechaInicial} fechaFinal={fechaFinal} arquitectoId={arquitectoId} revisorId={revisorId} contratistaId={contratistaId} cerrada={cerrada} editando={editando} administrador={administrador} revisor={revisor} arquitectos={arquitectos.data} revisores={revisores.data} contratistas={contratistas.data} alFechaInicial={setFechaInicial} alFechaFinal={setFechaFinal} alArquitecto={setArquitectoId} alRevisor={setRevisorId} alContratista={setContratistaId} alAgregar={abrirNuevaLinea} />

      <TablaItemsActa detalles={detalles} revisor={revisor} cerrada={cerrada} administrador={administrador} tipoUsuario={sesion?.tipoUsuario} total={total} todosRechazados={todosRechazados} hayRechazo={hayRechazo} alRevisar={revisarLinea} alAbrir={abrirLinea} />

      <div className="catalogo-panel">
        <Form.Group controlId="acta-comentario">
          <Form.Label>Comentario</Form.Label>
          <Form.Control as="textarea" rows={3} maxLength={1000} value={observacion} disabled={cerrada || (editando && !administrador)} onChange={(evento) => setObservacion(evento.target.value)} />
        </Form.Group>
        <div className="catalogo-acciones mt-3">
          {!cerrada && (
            <Button type="button" disabled={guardando} onClick={() => void registrar()}>
              {editando ? 'Guardar' : 'Registrar'}
            </Button>
          )}
          {editando && administrador && (
            <Button type="button" variant={cerrada ? 'outline-primary' : 'outline-danger'} disabled={guardando} onClick={() => void cambiarCierre(!cerrada)}>
              {cerrada ? 'Abrir acta' : 'Cerrar acta'}
            </Button>
          )}
          <Button type="button" variant="outline-secondary" onClick={() => void cancelar()}>
            Cancelar
          </Button>
          {(editando || numeroActa != null) && (
            <Button type="button" variant="outline-primary" onClick={() => imprimir(numeroActa ?? numeroRuta)}>
              Imprimir
            </Button>
          )}
        </div>
      </div>

      <ModalItemActa abierto={modalAbierto} lineaClave={lineaClave} linea={linea} construcciones={construcciones.data} conceptos={conceptos.data} unidades={unidades.data} insumos={insumos.data} itemsPresupuesto={itemsPresupuesto.data} alCerrar={() => setModalAbierto(false)} alLinea={setLinea} alGuardar={guardarLinea} alEliminar={eliminarLinea} />
      </>
      )}
    </section>
  )
}
