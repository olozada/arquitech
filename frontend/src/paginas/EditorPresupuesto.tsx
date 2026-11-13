import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Button } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { listarTodo } from '../api/pagina'
import { HistorialAuditoria } from '../componentes/HistorialAuditoria'
import type { ItemPresupuesto, PresupuestoDetalle, Recalculo } from '../api/presupuesto'
import { esAdministrador } from '../auth/perfiles'
import { useSesion } from '../auth/contextoSesion'
import { BarraEditor } from '../presupuesto/BarraEditor'
import { useEditorPresupuesto } from '../presupuesto/useEditorPresupuesto'
import { CapituloPresupuesto } from '../presupuesto/CapituloPresupuesto'
import { ModalesEditor } from '../presupuesto/ModalesEditor'
import { ResumenCostos } from '../presupuesto/ResumenCostos'

let idTemporal = 0

function nuevoId() {
  idTemporal -= 1
  return idTemporal
}

export function EditorPresupuesto() {
  const { id } = useParams()
  const presupuestoId = Number(id)
  const { sesion } = useSesion()
  const administrador = esAdministrador(sesion?.tipoUsuario)
  const {
    consulta,
    borrador,
    setBorrador,
    sucio,
    setSucio,
    guardando,
    setGuardando,
    avisoGuardado,
    conflicto,
    resumen,
    guardar,
  } = useEditorPresupuesto(presupuestoId, administrador)
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
  const [pestana, setPestana] = useState<'editor' | 'historial'>('editor')
  const consultas = useQueryClient()
  const apus = useQuery({
    queryKey: ['apus'],
    enabled: administrador,
    queryFn: async () => listarTodo<{ id: number; codigo: string | null; descripcion: string; unidad: string }>('/api/apu'),
  })

  useEffect(() => {
    if (resaltado == null) {
      return
    }
    document.getElementById(`item-${resaltado}`)?.scrollIntoView({ block: 'center' })
    const temporizador = window.setTimeout(() => setResaltado(null), 2000)
    return () => window.clearTimeout(temporizador)
  }, [resaltado])

  const editable = administrador && borrador?.estado === 'Borrador'
  const validacion = useQuery({
    queryKey: ['presupuesto-validacion', presupuestoId, borrador?.rowVersion],
    enabled: Number.isInteger(presupuestoId) && presupuestoId >= 0 && !sucio,
    queryFn: async () => (await cliente.get<Array<{ itemId: number | null; codigo: string; tipo: string; mensaje: string }>>(`/api/presupuestos/${presupuestoId}/validacion`)).data,
  })

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
      <BarraEditor
        borrador={borrador}
        administrador={administrador}
        guardando={guardando}
        editable={editable}
        aprobar={aprobar}
        postAccion={postAccion}
        guardarPlantilla={guardarPlantilla}
        accion={accion}
        guardar={guardar}
        previsualizarRecalculo={previsualizarRecalculo}
      />
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
        <CapituloPresupuesto
          key={capitulo.id || indiceCapitulo}
          indiceCapitulo={indiceCapitulo}
          borrador={borrador}
          editable={!!editable}
          resaltado={resaltado}
          filtroAvisos={filtroAvisos}
          avisos={validacion.data ?? []}
          apus={apus.data ?? []}
          alActualizar={(data) => setBorrador(data)}
          alCambiar={(item, cambios) => cambiar(indiceCapitulo, item, cambios)}
          alAgregarItem={() => agregarItem(indiceCapitulo)}
          alBuscarApu={() => { setBuscarApu(indiceCapitulo); setElegidos([]) }}
          alCrearApu={() => setPanelApu({ modo: 'crear', capitulo: indiceCapitulo, unidad: capitulo.items.at(-1)?.unidad ?? '' })}
          alMedicion={(item) => setMedicionItem({ capitulo: indiceCapitulo, item })}
          alDuplicar={(itemId) => void postAccion(`items/${itemId}/duplicar`, { rowVersion: borrador.rowVersion })}
          alMover={(itemId, destino) => { setMover({ itemId, capituloId: capitulo.id }); setDestinoCapitulo(destino) }}
          alConcepto={(item) => { setConceptoItem({ capitulo: indiceCapitulo, item }); setTextoConcepto('') }}
          alEditarApu={(apuId) => setPanelApu({ modo: 'editar', apuId })}
        />
      ))}
      {editable && (
        <Button
          type="button"
          variant="outline-secondary"
          className="mb-3"
          onClick={() =>
            setBorrador({
              ...borrador,
              capitulos: [...borrador.capitulos, { id: nuevoId(), numero: borrador.capitulos.length + 1, nombre: 'Nuevo capítulo', total: 0, incidencia: 0, items: [] }],
            })
          }
        >
          Agregar capítulo
        </Button>
      )}
      <ResumenCostos resumen={resumen} />
      <ModalesEditor
        borrador={borrador}
        editable={editable}
        administrador={administrador}
        medicionItem={medicionItem}
        setMedicionItem={setMedicionItem}
        cambiar={cambiar}
        buscarApu={buscarApu}
        setBuscarApu={setBuscarApu}
        textoApu={textoApu}
        setTextoApu={setTextoApu}
        apus={apus}
        elegidos={elegidos}
        setElegidos={setElegidos}
        postAccion={postAccion}
        panelApu={panelApu}
        setPanelApu={setPanelApu}
        aplicarRecalculo={aplicarRecalculo}
        mover={mover}
        setMover={setMover}
        destinoCapitulo={destinoCapitulo}
        setDestinoCapitulo={setDestinoCapitulo}
        confirmarMovimiento={confirmarMovimiento}
        conceptoItem={conceptoItem}
        setConceptoItem={setConceptoItem}
        textoConcepto={textoConcepto}
        setTextoConcepto={setTextoConcepto}
        recalculo={recalculo}
        setRecalculo={setRecalculo}
      />
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
        id: nuevoId(),
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
