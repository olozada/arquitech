import { useEffect, useMemo, useRef, useState } from 'react'
import axios from 'axios'
import { Link, useParams } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Form, Modal, Offcanvas, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { HistorialAuditoria } from '../componentes/HistorialAuditoria'
import { descargarPresupuesto, usePresupuesto } from '../api/presupuesto'
import type { CapituloPresupuesto, ItemPresupuesto, PresupuestoDetalle } from '../api/presupuesto'
import { esAdministrador } from '../auth/perfiles'
import { useSesion } from '../auth/contextoSesion'
import { dinero, redondear } from '../actas/formato'
import { EditorApu } from '../presupuesto/EditorApu'

type Recalculo = {
  actual: { total: number }
  propuesto: { total: number }
  diferencias: Array<{
    codigo: string
    descripcion: string
    materialActual: number
    materialNuevo: number
    manoObraActual: number
    manoObraNueva: number
    diferencia: number
  }>
}

export function EditorPresupuesto() {
  const { id } = useParams()
  const presupuestoId = Number(id)
  const consulta = usePresupuesto(presupuestoId)
  const [borrador, setBorrador] = useState<PresupuestoDetalle | null>(null)
  const [guardando, setGuardando] = useState(false)
  const [sucio, setSucio] = useState(false)
  const [avisoGuardado, setAvisoGuardado] = useState('')
  const [conflicto, setConflicto] = useState(false)
  const [medicionItem, setMedicionItem] = useState<{ capitulo: number; item: number } | null>(null)
  const [buscarApu, setBuscarApu] = useState<number | null>(null)
  const [textoApu, setTextoApu] = useState('')
  const [panelApu, setPanelApu] = useState<{ modo: 'crear'; capitulo: number; unidad: string } | { modo: 'editar'; apuId: number } | null>(null)
  const [filtroAvisos, setFiltroAvisos] = useState(false)
  const [elegidos, setElegidos] = useState<number[]>([])
  const [mover, setMover] = useState<{ itemId: number; capituloId: number } | null>(null)
  const [destinoCapitulo, setDestinoCapitulo] = useState(0)
  const [conceptoItem, setConceptoItem] = useState<{ capitulo: number; item: number } | null>(null)
  const [textoConcepto, setTextoConcepto] = useState('')
  const [recalculo, setRecalculo] = useState<Recalculo | null>(null)
  const [resaltado, setResaltado] = useState<number | null>(null)
  const espera = useRef<number | undefined>(undefined)
  const consultas = useQueryClient()
  const { sesion } = useSesion()
  const administrador = esAdministrador(sesion?.tipoUsuario)
  const [pestana, setPestana] = useState<'editor' | 'historial'>('editor')
  const apus = useQuery({
    queryKey: ['apus'],
    enabled: administrador,
    queryFn: async () => (await cliente.get<Array<{ id: number; codigo: string | null; descripcion: string; unidad: string }>>('/api/apu')).data,
  })

  useEffect(() => {
    if (consulta.data) {
      setBorrador(structuredClone(consulta.data))
    }
  }, [consulta.data])

  useEffect(() => {
    if (resaltado == null) {
      return
    }
    document.getElementById(`item-${resaltado}`)?.scrollIntoView({ block: 'center' })
    const temporizador = window.setTimeout(() => setResaltado(null), 2000)
    return () => window.clearTimeout(temporizador)
  }, [resaltado])

  const resumen = useMemo(() => (borrador ? calcular(borrador) : null), [borrador])
  const editable = administrador && borrador?.estado === 'Borrador' 
  const validacion = useQuery({
    queryKey: ['presupuesto-validacion', presupuestoId, borrador?.rowVersion],
    enabled: presupuestoId > 0 && !sucio,
    queryFn: async () => (await cliente.get<Array<{ itemId: number | null; codigo: string; tipo: string; mensaje: string }>>(`/api/presupuestos/${presupuestoId}/validacion`)).data,
  })

  useEffect(() => {
    if (!sucio || !editable) {
      return
    }
    window.clearTimeout(espera.current)
    espera.current = window.setTimeout(() => {
      void guardar(true)
    }, 600)
    return () => window.clearTimeout(espera.current)
  }, [borrador, sucio, editable])

  async function guardar(silencioso = false) {
    if (!borrador) {
      return
    }
    setGuardando(true)
    setAvisoGuardado('Guardando…')
    try {
      const { data } = await cliente.put<PresupuestoDetalle>(`/api/presupuestos/${borrador.id}`, cuerpo(borrador))
      setSucio(false)
      setConflicto(false)
      setBorrador(data)
      setAvisoGuardado('Guardado')
      await consultas.invalidateQueries({ queryKey: ['presupuesto', borrador.id] })
      if (!silencioso) {
        await Swal.fire({ icon: 'success', title: 'Presupuesto guardado', confirmButtonColor: '#1abb9c' })
      }
    } catch (error) {
      setAvisoGuardado('Error')
      if (axios.isAxiosError(error) && error.response?.status === 409) {
        setConflicto(true)
        return
      }
      if (!silencioso) {
        await Swal.fire({ icon: 'error', title: 'No se pudo guardar', text: mensajeError(error) })
      }
    } finally {
      setGuardando(false)
    }
  }

  async function aprobar() {
    if (!borrador) {
      return
    }
    const avisos = validacion.data ?? []
    const bloqueos = avisos.filter((aviso) => aviso.tipo !== 'CantidadCero')
    if (bloqueos.length > 0) {
      await Swal.fire({ icon: 'warning', title: 'No se puede aprobar', text: bloqueos.map((aviso) => aviso.mensaje).join(' ') })
      return
    }
    let confirmar = false
    if (avisos.some((aviso) => aviso.tipo === 'CantidadCero')) {
      const respuesta = await Swal.fire({ icon: 'question', title: 'Hay cantidades en cero', text: '¿Aprueba de todos modos?', showCancelButton: true, confirmButtonColor: '#1abb9c' })
      if (!respuesta.isConfirmed) {
        return
      }
      confirmar = true
    }
    await accion(`aprobar?confirmarCantidadesCero=${confirmar}`, 'Presupuesto aprobado')
  }

  async function postAccion(ruta: string, cuerpoAccion: unknown) {
    if (!borrador) {
      return
    }
    try {
      const { data } = await cliente.post<PresupuestoDetalle>(`/api/presupuestos/${borrador.id}/${ruta}`, cuerpoAccion)
      setBorrador(data)
      setSucio(false)
    } catch (error) {
      await Swal.fire({ icon: 'error', text: mensajeError(error) })
    }
  }

  async function guardarPlantilla() {
    if (!borrador) {
      return
    }
    const respuesta = await Swal.fire({ title: 'Nombre de la plantilla', input: 'text', inputValue: borrador.nombre, showCancelButton: true, confirmButtonColor: '#1abb9c' })
    if (!respuesta.isConfirmed) {
      return
    }
    try {
      await cliente.post(`/api/presupuestos/${borrador.id}/guardar-como-plantilla`, { nombre: respuesta.value })
      await Swal.fire({ icon: 'success', title: 'Plantilla guardada', confirmButtonColor: '#1abb9c' })
    } catch (error) {
      await Swal.fire({ icon: 'error', text: mensajeError(error) })
    }
  }

  async function accion(ruta: string, titulo: string) {
    if (!borrador) {
      return
    }
    setGuardando(true)
    try {
      const { data } = await cliente.post<PresupuestoDetalle>(`/api/presupuestos/${borrador.id}/${ruta}`)
      if (ruta === 'nueva-version') {
        window.location.assign(`/presupuestos/${data.id}`)
        return
      }
      setBorrador(data)
      await consultas.invalidateQueries({ queryKey: ['presupuestos'] })
      await Swal.fire({ icon: 'success', title: titulo, confirmButtonColor: '#1abb9c' })
    } catch (error) {
      await Swal.fire({ icon: 'error', title: 'No se pudo completar', text: mensajeError(error) })
    } finally {
      setGuardando(false)
    }
  }

  if (consulta.isError) {
    return <p>No se pudo abrir el presupuesto.</p>
  }

  if (!borrador || !resumen) {
    return <p>Cargando presupuesto…</p>
  }

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
        <div>
          <h1 className="mb-0">{borrador.nombre}</h1>
          <p className="mb-0 text-secondary">
            {borrador.construccion} · versión {borrador.version} · {borrador.estado}
          </p>
        </div>
        <div className="d-flex gap-2 flex-wrap">
          <Link className="btn btn-outline-secondary" to={`/presupuestos/${borrador.id}/seguimiento`}>
            Seguimiento
          </Link>
          <Button type="button" variant="outline-primary" onClick={() => void descargarPresupuesto(borrador.id, 'excel')}>
            Excel
          </Button>
          <Button type="button" variant="outline-primary" onClick={() => void descargarPresupuesto(borrador.id, 'pdf')}>
            PDF
          </Button>
          {administrador && borrador.estado === 'Borrador' && !borrador.esPlantilla && (
            <Button type="button" variant="outline-success" disabled={guardando} onClick={() => void aprobar()}>
              Aprobar
            </Button>
          )}
          {editable && (
            <Button type="button" variant="outline-secondary" onClick={() => void postAccion('renumerar', { rowVersion: borrador.rowVersion })}>
              Renumerar
            </Button>
          )}
          {administrador && (
            <Button type="button" variant="outline-secondary" onClick={() => void guardarPlantilla()}>
              Plantilla
            </Button>
          )}
          {administrador && (
            <Button type="button" variant="outline-secondary" disabled={guardando} onClick={() => void accion('nueva-version', 'Nueva versión')}>
              Nueva versión
            </Button>
          )}
          {editable && (
            <Button type="button" disabled={guardando} onClick={() => void guardar()}>
              Guardar
            </Button>
          )}
          {(borrador.estado === 'Borrador' || borrador.estado === 'Aprobado' || borrador.estado === 'Cerrado') && (
            <Button type="button" variant="outline-primary" onClick={() => void previsualizarRecalculo()}>
              {borrador.estado === 'Borrador' ? 'Recalcular con precios actuales' : '¿Cuánto costaría hoy?'}
            </Button>
          )}
        </div>
      </div>
      {administrador && (
        <div className="nav nav-tabs mb-3">
          <button type="button" className={pestana === 'editor' ? 'nav-link active' : 'nav-link'} onClick={() => setPestana('editor')}>
            Presupuesto
          </button>
          <button type="button" className={pestana === 'historial' ? 'nav-link active' : 'nav-link'} onClick={() => setPestana('historial')}>
            Historial
          </button>
        </div>
      )}
      {pestana === 'historial' ? (
        <HistorialAuditoria entidad="Presupuesto" entidadId={String(borrador.id)} />
      ) : (
      <>
      {conflicto && (
        <div className="alert alert-warning">
          Otro usuario modificó este presupuesto. Lo que escribió sigue en esta pantalla.
          <Button type="button" size="sm" className="ms-2" variant="outline-dark" onClick={() => void consultas.invalidateQueries({ queryKey: ['presupuesto', borrador.id] })}>
            Recargar
          </Button>
        </div>
      )}
      <p className="text-secondary">
        {avisoGuardado}
        {(validacion.data?.length ?? 0) > 0 && (
          <button type="button" className="btn btn-link btn-sm" onClick={() => setFiltroAvisos((valor) => !valor)}>
            {validacion.data?.length} advertencias
          </button>
        )}
      </p>
      {borrador.capitulos.map((capitulo, indiceCapitulo) => (
        <details key={capitulo.id || indiceCapitulo} className="catalogo-panel mb-3" open>
          <summary
            draggable={editable}
            onDragStart={(evento) => evento.dataTransfer.setData('text/plain', String(indiceCapitulo))}
            onDragOver={(evento) => evento.preventDefault()}
            onDrop={(evento) => {
              evento.preventDefault()
              const origen = Number(evento.dataTransfer.getData('text/plain'))
              if (!Number.isInteger(origen) || origen === indiceCapitulo) {
                return
              }
              const capitulos = [...borrador.capitulos]
              const [movido] = capitulos.splice(origen, 1)
              capitulos.splice(indiceCapitulo, 0, movido)
              void cliente.put<PresupuestoDetalle>(`/api/presupuestos/${borrador.id}/orden`, {
                rowVersion: borrador.rowVersion,
                capitulos: capitulos.filter((capituloOrden) => capituloOrden.id > 0).map((capituloOrden, indice) => ({
                  id: capituloOrden.id,
                  orden: indice + 1,
                  items: capituloOrden.items.map((fila) => fila.id).filter((itemId) => itemId > 0),
                })),
              }).then(({ data }) => setBorrador(data))
            }}
          >
            <strong>
              {capitulo.numero}. {capitulo.nombre}
            </strong>{' '}
            <span className="text-secondary">{dinero(totalCapitulo(capitulo), 2)}</span>
          </summary>
          <Table responsive size="sm" className="presupuesto-hoja mt-2 mb-0">
            <thead>
              <tr>
                <th>Código</th>
                <th>Descripción</th>
                <th>Unidad</th>
                <th>Cantidad</th>
                <th>APU</th>
                <th>Factor</th>
                <th className="text-end">Material</th>
                <th className="text-end">MO</th>
                <th className="text-end">Total</th>
              </tr>
            </thead>
            <tbody>
              {capitulo.items.map((item, indiceItem) => (
                <tr
                  key={item.id || `${indiceCapitulo}-${indiceItem}`}
                  id={item.id > 0 ? `item-${item.id}` : undefined}
                  className={resaltado === item.id ? 'table-warning' : undefined}
                  hidden={filtroAvisos && !(validacion.data ?? []).some((aviso) => aviso.itemId === item.id)}
                >
                  <td>
                    <Form.Control size="sm" value={item.codigo} disabled={!editable} onChange={(evento) => cambiar(indiceCapitulo, indiceItem, { codigo: evento.target.value })} />
                  </td>
                  <td>
                    <Form.Control size="sm" value={item.descripcion} disabled={!editable} onChange={(evento) => cambiar(indiceCapitulo, indiceItem, { descripcion: evento.target.value })} />
                    {item.avisoUnidad && <small className="text-danger">La unidad no coincide con el APU. Indique un factor de conversión.</small>}
                  </td>
                  <td>
                    <Form.Control size="sm" value={item.unidad} disabled={!editable} onChange={(evento) => cambiar(indiceCapitulo, indiceItem, { unidad: evento.target.value })} />
                  </td>
                  <td>
                    <Form.Control size="sm" value={item.cantidad} disabled={!editable || item.mediciones.length > 0} onChange={(evento) => cambiar(indiceCapitulo, indiceItem, { cantidad: Number(evento.target.value) || 0 })} />
                    {editable && !borrador.esPlantilla && (
                      <>
                        <Button type="button" variant="link" size="sm" className="p-0" onClick={() => setMedicionItem({ capitulo: indiceCapitulo, item: indiceItem })}>
                          Mediciones
                        </Button>
                        <Form.Select
                          size="sm"
                          value={item.cantidadOrigenItemId ?? ''}
                          onChange={(evento) => cambiar(indiceCapitulo, indiceItem, { cantidadOrigenItemId: Number(evento.target.value) || null })}
                        >
                          <option value="">Digitar</option>
                          {borrador.capitulos.flatMap((grupo) => grupo.items).filter((candidato) => candidato.id > 0 && candidato.id !== item.id).map((candidato) => (
                            <option key={candidato.id} value={candidato.id}>
                              = {candidato.codigo} × {item.cantidadOrigenFactor}
                            </option>
                          ))}
                        </Form.Select>
                      </>
                    )}
                  </td>
                  <td>
                    <Form.Select size="sm" value={item.apuId ?? ''} disabled={!editable} onChange={(evento) => cambiar(indiceCapitulo, indiceItem, { apuId: Number(evento.target.value) || null, desdeApu: Number(evento.target.value) > 0 })}>
                      <option value="">Valor directo</option>
                      {(apus.data ?? []).map((apu) => (
                        <option key={apu.id} value={apu.id}>
                          {(apu.codigo ? `${apu.codigo} ` : '') + apu.descripcion}
                        </option>
                      ))}
                    </Form.Select>
                  </td>
                  <td>
                    <Form.Control size="sm" value={item.factorConversionApu} disabled={!editable} onChange={(evento) => cambiar(indiceCapitulo, indiceItem, { factorConversionApu: Number(evento.target.value) || 1 })} />
                  </td>
                  <td className="text-end">
                    {editable && !item.desdeApu ? (
                      <Form.Control size="sm" className="text-end" value={redondear(item.valorMaterialUnitario, 2)} onChange={(evento) => cambiar(indiceCapitulo, indiceItem, { valorMaterialUnitario: Number(evento.target.value) || 0 })} />
                    ) : (
                      dinero(item.valorMaterialUnitario, 2)
                    )}
                  </td>
                  <td className="text-end">
                    {editable && !item.desdeApu ? (
                      <Form.Control size="sm" className="text-end" value={redondear(item.valorManoObraUnitario, 2)} onChange={(evento) => cambiar(indiceCapitulo, indiceItem, { valorManoObraUnitario: Number(evento.target.value) || 0 })} />
                    ) : (
                      dinero(item.valorManoObraUnitario, 2)
                    )}
                  </td>
                  <td className="text-end">{dinero(item.cantidad * (item.valorMaterialUnitario + item.valorManoObraUnitario), 2)}</td>
                  <td>
                    {editable && item.id > 0 && (
                      <>
                        <Button type="button" size="sm" variant="link" onClick={() => void postAccion(`items/${item.id}/duplicar`, { rowVersion: borrador.rowVersion })}>
                          Duplicar
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="link"
                          onClick={() => {
                            setMover({ itemId: item.id, capituloId: capitulo.id })
                            setDestinoCapitulo(borrador.capitulos.find((candidato) => candidato.id !== capitulo.id && candidato.id > 0)?.id ?? 0)
                          }}
                        >
                          Mover
                        </Button>
                        <Button type="button" size="sm" variant="link" onClick={() => { setConceptoItem({ capitulo: indiceCapitulo, item: indiceItem }); setTextoConcepto('') }}>
                          Concepto
                        </Button>
                        {item.apuId != null && item.apuId > 0 && (
                          <Button type="button" size="sm" variant="link" onClick={() => setPanelApu({ modo: 'editar', apuId: item.apuId ?? 0 })}>
                            Editar APU
                          </Button>
                        )}
                      </>
                    )}
                    {(validacion.data ?? []).filter((aviso) => aviso.itemId === item.id).map((aviso) => (
                      <div key={aviso.tipo} className="text-danger small" title={aviso.mensaje}>
                        {aviso.mensaje}
                      </div>
                    ))}
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
          {editable && !borrador.esPlantilla && (
            <div className="d-flex gap-2">
              <Button type="button" variant="link" onClick={() => agregarItem(indiceCapitulo)}>
                Ítem manual
              </Button>
              <Button type="button" variant="link" onClick={() => { setBuscarApu(indiceCapitulo); setElegidos([]) }}>
                Desde APU
              </Button>
              <Button type="button" variant="link" onClick={() => setPanelApu({ modo: 'crear', capitulo: indiceCapitulo, unidad: capitulo.items.at(-1)?.unidad ?? '' })}>
                Crear APU
              </Button>
            </div>
          )}
        </details>
      ))}
      {editable && (
        <Button
          type="button"
          variant="outline-secondary"
          className="mb-3"
          onClick={() =>
            setBorrador({
              ...borrador,
              capitulos: [...borrador.capitulos, { id: 0, numero: borrador.capitulos.length + 1, nombre: 'Nuevo capítulo', total: 0, incidencia: 0, items: [] }],
            })
          }
        >
          Agregar capítulo
        </Button>
      )}
      <div className="catalogo-panel presupuesto-resumen">
        <p>Subtotal {dinero(resumen.subtotal, 2)}</p>
        <p>Imprevistos {dinero(resumen.imprevistos, 2)} · Incremento {dinero(resumen.incremento, 2)}</p>
        <p>Administración {dinero(resumen.administracion, 2)} · Utilidad {dinero(resumen.utilidad, 2)} · IVA {dinero(resumen.iva, 2)}</p>
        <p className="presupuesto-total">Total {dinero(resumen.total, 2)}</p>
        <p>Valor por m² {resumen.m2 == null ? '—' : dinero(resumen.m2, 2)}</p>
      </div>
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
      <Modal show={mover !== null} onHide={() => setMover(null)} centered>
        <Modal.Header closeButton>
          <Modal.Title>Mover ítem</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <Form.Select value={destinoCapitulo} onChange={(evento) => setDestinoCapitulo(Number(evento.target.value))}>
            {borrador.capitulos.filter((capitulo) => capitulo.id !== mover?.capituloId && capitulo.id > 0).map((capitulo) => (
              <option key={capitulo.id} value={capitulo.id}>{capitulo.numero}. {capitulo.nombre}</option>
            ))}
          </Form.Select>
          <div className="d-flex gap-2 mt-3">
            <Button type="button" onClick={() => void confirmarMovimiento()}>Mover</Button>
            <Button type="button" variant="outline-secondary" onClick={() => setMover(null)}>Cancelar</Button>
          </div>
        </Modal.Body>
      </Modal>
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
      )}
    </section>
  )

  async function confirmarMovimiento() {
    if (!mover || !borrador || destinoCapitulo <= 0) {
      return
    }
    const { data } = await cliente.post<PresupuestoDetalle>(`/api/presupuestos/${borrador.id}/items/${mover.itemId}/mover`, {
      capituloDestinoId: destinoCapitulo,
      rowVersion: borrador.rowVersion,
    })
    setBorrador(data)
    setMover(null)
    setResaltado(mover.itemId)
  }

  async function previsualizarRecalculo() {
    if (!borrador) {
      return
    }
    try {
      const { data } = await cliente.post<Recalculo>(`/api/presupuestos/${borrador.id}/recalcular`, null, { params: { aplicar: false } })
      setRecalculo(data)
    } catch (error) {
      await Swal.fire({ icon: 'error', text: mensajeError(error) })
    }
  }

  async function aplicarRecalculo() {
    if (!borrador) {
      return
    }
    try {
      await cliente.post(`/api/presupuestos/${borrador.id}/recalcular`, null, { params: { aplicar: true, rowVersion: borrador.rowVersion } })
      setRecalculo(null)
      await consultas.invalidateQueries({ queryKey: ['presupuesto', borrador.id] })
    } catch (error) {
      await Swal.fire({ icon: 'error', text: mensajeError(error) })
    }
  }

  function cambiar(capitulo: number, item: number, cambios: Partial<ItemPresupuesto>) {
    setBorrador((actual) => {
      if (!actual) {
        return actual
      }
      const copia = structuredClone(actual)
      copia.capitulos[capitulo].items[item] = { ...copia.capitulos[capitulo].items[item], ...cambios }
      setSucio(true)
      return copia
    })
  }

  function agregarItem(capitulo: number) {
    setBorrador((actual) => {
      if (!actual) {
        return actual
      }
      const copia = structuredClone(actual)
      copia.capitulos[capitulo].items.push({
        id: 0,
        codigo: '',
        descripcion: '',
        unidad: '',
        unidadMedidaId: null,
        apuId: null,
        apu: null,
        unidadApu: null,
        factorConversionApu: 1,
        avisoUnidad: false,
        conceptoId: null,
        cantidad: 0,
        cantidadOrigenItemId: null,
        cantidadOrigenFactor: 1,
        valorMaterialUnitario: 0,
        valorManoObraUnitario: 0,
        valorUnitario: 0,
        total: 0,
        desdeApu: false,
        mediciones: [],
      })
      return copia
    })
  }
}

function SelectorConcepto({
  texto,
  alBuscar,
  alElegir,
}: {
  texto: string
  alBuscar: (texto: string) => void
  alElegir: (conceptoId: number) => void
}) {
  const conceptos = useQuery({
    queryKey: ['conceptos'],
    queryFn: async () => (await cliente.get<Array<{ id: number; descripcion: string }>>('/api/conceptos')).data,
  })
  const filtro = texto.trim().toLowerCase()
  return (
    <>
      <Form.Control className="mb-2" placeholder="Buscar concepto" value={texto} onChange={(evento) => alBuscar(evento.target.value)} />
      {(conceptos.data ?? []).filter((concepto) => concepto.descripcion.toLowerCase().includes(filtro)).slice(0, 20).map((concepto) => (
        <Button key={concepto.id} type="button" variant="link" className="d-block text-start" onClick={() => alElegir(concepto.id)}>
          {concepto.descripcion}
        </Button>
      ))}
    </>
  )
}

function Mediciones({
  item,
  editable,
  alCambiar,
}: {
  item: ItemPresupuesto
  editable: boolean
  alCambiar: (mediciones: ItemPresupuesto['mediciones']) => void
}) {
  return (
    <>
      <Table size="sm">
        <thead>
          <tr>
            <th>Ubicación</th>
            <th>Veces</th>
            <th>Largo</th>
            <th>Ancho</th>
            <th>Alto</th>
            <th>Parcial</th>
          </tr>
        </thead>
        <tbody>
          {item.mediciones.map((medicion, indice) => (
            <tr key={indice}>
              <td>
                <Form.Control size="sm" value={medicion.ubicacion} disabled={!editable} onChange={(evento) => editar(indice, { ubicacion: evento.target.value })} />
              </td>
              {(['veces', 'largo', 'ancho', 'alto'] as const).map((campo) => (
                <td key={campo}>
                  <Form.Control
                    size="sm"
                    value={medicion[campo] ?? ''}
                    disabled={!editable}
                    onChange={(evento) => {
                      const texto = evento.target.value
                      const numero = Number(texto)
                      if (campo === 'veces') {
                        editar(indice, { veces: texto === '' || !Number.isFinite(numero) ? 1 : numero })
                        return
                      }
                      editar(indice, { [campo]: texto === '' ? null : numero })
                    }}
                  />
                </td>
              ))}
              <td>{parcial(medicion.veces, medicion.largo, medicion.ancho, medicion.alto).toLocaleString('es-CO')}</td>
            </tr>
          ))}
        </tbody>
      </Table>
      {editable && (
        <Button type="button" variant="link" onClick={() => alCambiar([...item.mediciones, { id: 0, ubicacion: '', veces: 1, largo: null, ancho: null, alto: null, parcial: 1 }])}>
          Agregar medida
        </Button>
      )}
    </>
  )

  function editar(indice: number, cambios: Partial<ItemPresupuesto['mediciones'][number]>) {
    alCambiar(item.mediciones.map((medicion, posicion) => (posicion === indice ? { ...medicion, ...cambios } : medicion)))
  }
}

function cuerpo(presupuesto: PresupuestoDetalle) {
  return {
    construccionId: presupuesto.construccionId,
    nombre: presupuesto.nombre,
    rowVersion: presupuesto.rowVersion,
    fecha: presupuesto.fecha,
    mesesProyecto: presupuesto.mesesProyecto,
    areaConstruidaM2: presupuesto.areaConstruidaM2,
    pctImprevistos: presupuesto.pctImprevistos,
    pctIncremento: presupuesto.pctIncremento,
    pctAdministracion: presupuesto.pctAdministracion,
    pctUtilidad: presupuesto.pctUtilidad,
    pctIvaUtilidad: presupuesto.pctIvaUtilidad,
    capitulos: presupuesto.capitulos.map((capitulo) => ({
      id: capitulo.id > 0 ? capitulo.id : null,
      numero: capitulo.numero,
      nombre: capitulo.nombre,
      items: capitulo.items.map((item) => ({
        id: item.id > 0 ? item.id : null,
        codigo: item.codigo,
        descripcion: item.descripcion,
        unidad: item.unidad,
        unidadMedidaId: item.unidadMedidaId,
        apuId: item.apuId,
        factorConversionApu: item.factorConversionApu || 1,
        conceptoId: item.conceptoId,
        cantidad: item.cantidad,
        cantidadOrigenItemId: item.cantidadOrigenItemId,
        cantidadOrigenFactor: item.cantidadOrigenFactor || 1,
        valorMaterialUnitario: item.valorMaterialUnitario,
        valorManoObraUnitario: item.valorManoObraUnitario,
        mediciones: item.mediciones.map((medicion) => ({
          ubicacion: medicion.ubicacion,
          veces: medicion.veces || 1,
          largo: medicion.largo,
          ancho: medicion.ancho,
          alto: medicion.alto,
        })),
      })),
    })),
  }
}

function calcular(presupuesto: PresupuestoDetalle) {
  const subtotal = presupuesto.capitulos.reduce((suma, capitulo) => suma + totalCapitulo(capitulo), 0)
  const imprevistos = subtotal * presupuesto.pctImprevistos
  const incremento = subtotal * presupuesto.pctIncremento
  const administracion = subtotal * presupuesto.pctAdministracion
  const utilidad = subtotal * presupuesto.pctUtilidad
  const iva = utilidad * presupuesto.pctIvaUtilidad
  const total = subtotal + imprevistos + incremento + administracion + utilidad + iva
  return { subtotal, imprevistos, incremento, administracion, utilidad, iva, total, m2: presupuesto.areaConstruidaM2 > 0 ? total / presupuesto.areaConstruidaM2 : null }
}

function totalCapitulo(capitulo: CapituloPresupuesto) {
  return capitulo.items.reduce((suma, item) => suma + item.cantidad * (item.valorMaterialUnitario + item.valorManoObraUnitario), 0)
}

function parcial(veces: number, largo: number | null, ancho: number | null, alto: number | null) {
  return (veces || 1) * (largo ?? 1) * (ancho ?? 1) * (alto ?? 1)
}
