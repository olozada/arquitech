import { useState } from 'react'
import axios from 'axios'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Form } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { listarTodo } from '../api/pagina'
import { HistorialAuditoria } from '../componentes/HistorialAuditoria'
import { useSesion } from '../auth/contextoSesion'
import { esAdministrador, esRevisor, perfilArquitecto, perfilDeNuevoItem } from '../auth/perfiles'
import { abrirPdf } from '../api/archivos'
import { numero, redondear } from '../actas/formato'
import { EncabezadoActa } from '../actas/EncabezadoActa'
import type { ItemPresupuestoActa, LineaFormulario } from '../actas/lineaActa'
import { lineaVacia } from '../actas/lineaActa'
import { ModalItemActa } from '../actas/ModalItemActa'
import { useCatalogo } from '../actas/useCatalogo'
import { TablaItemsActa } from '../actas/TablaItemsActa'
import { borradorVacio, descartarBorradorActa, leerBorrador, marcaDeActaNueva, useBorradorActa, type LineaBorrador } from '../actas/useBorradorActa'
import type { RegistroCatalogo } from '../catalogos/definiciones'

const mensajeFechas = 'La fecha final debe ser mayor o igual que la fecha inicial.'

function imprimir(numero: number) {
  const ventana = prepararVisor()
  void abrirPdf(numero, ventana).catch(async (error) => {
    ventana?.close()
    await avisar(mensajeError(error))
  })
}

function prepararVisor() {
  const ventana = window.open('', '_blank')
  if (!ventana) {
    return null
  }

  ventana.document.write(
    '<!DOCTYPE html><title>Acta</title><p style="font-family:sans-serif;padding:24px">Generando el acta…</p>',
  )
  ventana.document.close()
  return ventana
}

type ActaGuardada = {
  numeroActa: number
  fechaInicial: string
  fechaFinal: string
  arquitectoId: number
  revisorId: number
  contratistaId: number
  observacion: string | null
  detalles: Array<{
    id: number
    construccionId: number
    construccion: string
    conceptoId: number
    concepto: string
    unidadMedidaId: number | null
    unidadMedida: string
    insumoId: number | null
    insumo: string | null
    cantidad: number
    precio: number
    descripcion: string
    tipoUsuarioId: number
    tipoUsuario: string
    aprobado: boolean
    presupuestoItemId?: number | null
  }>
  cerrada: boolean
  rowVersion: string
}

export function TomaActas() {
  const { sesion } = useSesion()
  const administrador = esAdministrador(sesion?.tipoUsuario)
  const revisor = esRevisor(sesion?.tipoUsuario)
  const ubicacion = useLocation()
  const parametros = useParams()
  const navegar = useNavigate()
  const consultas = useQueryClient()
  const numeroRuta = Number(parametros.numero)
  const editando = Number.isInteger(numeroRuta) && numeroRuta > 0
  const marcaNueva = marcaDeActaNueva(ubicacion.state)
  const inicial = useState(() => (editando || marcaNueva ? borradorVacio() : leerBorrador()))[0]
  const [marcaAplicada, setMarcaAplicada] = useState(marcaNueva)
  const [fechaInicial, setFechaInicial] = useState(inicial.fechaInicial)
  const [fechaFinal, setFechaFinal] = useState(inicial.fechaFinal)
  const [arquitectoId, setArquitectoId] = useState(inicial.arquitectoId)
  const [revisorId, setRevisorId] = useState(inicial.revisorId)
  const [contratistaId, setContratistaId] = useState(inicial.contratistaId)
  const [observacion, setObservacion] = useState(inicial.observacion)
  const [detalles, setDetalles] = useState(inicial.detalles)
  const [numeroActa, setNumeroActa] = useState<number | null>(null)
  const [modalAbierto, setModalAbierto] = useState(false)
  const [lineaClave, setLineaClave] = useState<string | null>(null)
  const [linea, setLinea] = useState(lineaVacia)
  const [guardando, setGuardando] = useState(false)
  const [cerrada, setCerrada] = useState(false)
  const [version, setVersion] = useState('')
  const [conflicto, setConflicto] = useState(false)
  const [hidratada, setHidratada] = useState(!editando)
  const [pestana, setPestana] = useState<'acta' | 'historial'>('acta')

  const arquitectos = useCatalogo('arquitectos')
  const revisores = useCatalogo('revisores')
  const contratistas = useCatalogo('contratistas')
  const construcciones = useCatalogo('construcciones')
  const conceptos = useCatalogo('conceptos')
  const unidades = useCatalogo('unidades-medida')
  const insumos = useQuery({
    queryKey: ['insumos-acta'],
    queryFn: async () => {
      const data = await listarTodo<{ id: number; codigo: string | null; descripcion: string; activo: boolean }>('/api/insumos')
      return data
        .filter((insumo) => insumo.activo)
        .map((insumo) => ({
          id: insumo.id,
          descripcion: insumo.codigo ? `${insumo.codigo} ${insumo.descripcion}` : insumo.descripcion,
          activo: true,
        }))
    },
  })
  const obraLinea = Number(linea.construccionId)
  const itemsPresupuesto = useQuery({
    queryKey: ['presupuesto-items-acta', obraLinea],
    enabled: modalAbierto && obraLinea > 0,
    queryFn: async () =>
      (await cliente.get<ItemPresupuestoActa[]>(`/api/presupuestos/items-acta?construccionId=${obraLinea}`)).data,
  })
  const actaConsulta = useQuery({
    queryKey: ['acta', numeroRuta],
    enabled: editando,
    staleTime: Infinity,
    queryFn: async () => (await cliente.get<ActaGuardada>(`/api/actas/${numeroRuta}`)).data,
  })

  useBorradorActa(editando, marcaNueva, marcaAplicada, {
    fechaInicial,
    fechaFinal,
    arquitectoId,
    revisorId,
    contratistaId,
    observacion,
    detalles,
  })

  const hayRechazo = detalles.some((item) => !item.aprobado)
  const todosRechazados = detalles.length > 0 && detalles.every((item) => !item.aprobado)
  const total = detalles.reduce((suma, item) => suma + (item.aprobado ? item.cantidad * item.precio : 0), 0)

  function abrirNuevaLinea() {
    setLineaClave(null)
    setLinea(lineaVacia())
    setModalAbierto(true)
  }

  function abrirLinea(item: LineaBorrador) {
    setLineaClave(item.clave)
    setLinea({
      construccionId: String(item.construccionId),
      conceptoId: String(item.conceptoId),
      unidadMedidaId: item.unidadMedidaId ? String(item.unidadMedidaId) : '',
      insumoId: item.insumoId ? String(item.insumoId) : '',
      cantidad: String(item.cantidad),
      precio: String(item.precio),
      descripcion: item.descripcion,
      presupuestoItemId: item.presupuestoItemId ? String(item.presupuestoItemId) : '',
    })
    setModalAbierto(true)
  }

  function guardarLinea() {
    const resultadoLinea = construirLinea(linea, construcciones.data, conceptos.data, unidades.data, insumos.data)
    if (!resultadoLinea.ok) {
      void avisar(resultadoLinea.error)
      return
    }

    const construida = resultadoLinea.linea
    const tipo = perfilDeNuevoItem(sesion?.tipoUsuario)

    setNumeroActa(null)
    setDetalles((actual) =>
      lineaClave
        ? actual.map((item) =>
            item.clave === lineaClave
              ? { ...construida, clave: lineaClave, id: item.id, tipoUsuarioId: item.tipoUsuarioId, tipoUsuario: item.tipoUsuario, aprobado: item.aprobado }
              : item,
          )
        : [...actual, { ...construida, clave: crypto.randomUUID(), id: null, tipoUsuarioId: tipo.id, tipoUsuario: tipo.nombre }],
    )
    setModalAbierto(false)
  }

  function revisarLinea(clave: string, aprobado: boolean) {
    setNumeroActa(null)
    setDetalles((actual) => actual.map((item) => (item.clave === clave ? { ...item, aprobado } : item)))
  }

  function eliminarLinea() {
    if (!lineaClave) {
      return
    }

    setNumeroActa(null)
    setDetalles((actual) => actual.filter((item) => item.clave !== lineaClave))
    setModalAbierto(false)
  }

  async function registrar() {
    const error = validarEncabezado(fechaInicial, fechaFinal, arquitectoId, revisorId, contratistaId)
    if (error) {
      await avisar(error)
      return
    }

    const confirmacion = await Swal.fire({
      icon: 'question',
      title: editando ? '¿Desea guardar los cambios del acta?' : '¿Desea registrar el acta?',
      showCancelButton: true,
      confirmButtonText: editando ? 'Guardar' : 'Registrar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#1abb9c',
    })
    if (!confirmacion.isConfirmed) {
      return
    }

    setGuardando(true)
    const cuerpo = {
      fechaInicial,
      fechaFinal,
      arquitectoId,
      revisorId,
      contratistaId,
      observacion: observacion.trim() || null,
      rowVersion: version || null,
      detalles: detalles.map((item) => ({
        id: item.id,
        tipoUsuarioId: item.tipoUsuarioId,
        construccionId: item.construccionId,
        conceptoId: item.conceptoId,
        unidadMedidaId: item.unidadMedidaId,
        insumoId: item.insumoId,
        cantidad: item.cantidad,
        precio: item.precio,
        descripcion: item.descripcion,
        aprobado: item.aprobado,
        presupuestoItemId: item.presupuestoItemId,
      })),
    }
    try {
      if (editando) {
        const { data } = await cliente.put<ActaGuardada>(`/api/actas/${numeroRuta}`, cuerpo)
        aplicarActa(data)
        consultas.setQueryData(['acta', numeroRuta], data)
        await consultas.invalidateQueries({ queryKey: ['actas'] })
        await Swal.fire({
          icon: 'success',
          title: '¡Finalizado!',
          text: `Acta N° ${data.numeroActa}`,
          confirmButtonColor: '#1abb9c',
        })
        navegar('/actas')
        return
      }

      const { data } = await cliente.post<{ numeroActa: number }>('/api/actas', cuerpo)
      limpiarFormulario()
      setNumeroActa(data.numeroActa)
      descartarBorradorActa()
      await consultas.invalidateQueries({ queryKey: ['actas'] })
      await Swal.fire({
        icon: 'success',
        title: '¡Finalizado!',
        text: `Acta N° ${data.numeroActa}`,
        confirmButtonColor: '#1abb9c',
      })
      navegar('/actas')
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 409) {
        setConflicto(true)
        return
      }

      await avisar(mensajeError(error))
    } finally {
      setGuardando(false)
    }
  }

  async function cancelar() {
    const confirmacion = await Swal.fire({
      icon: 'warning',
      title: '¿Desea cancelar el acta?',
      showCancelButton: true,
      confirmButtonText: 'Sí, cancelar',
      cancelButtonText: 'Volver',
      confirmButtonColor: '#d9534f',
    })
    if (!confirmacion.isConfirmed) {
      return
    }

    if (!editando) {
      limpiarFormulario()
      setNumeroActa(null)
      descartarBorradorActa()
    }

    navegar('/actas')
  }

  async function cambiarCierre(cerrar: boolean) {
    const confirmacion = await Swal.fire({
      icon: 'question',
      title: cerrar ? '¿Desea cerrar el acta?' : '¿Desea abrir el acta?',
      text: cerrar
        ? 'Al cerrarla no se podrán agregar, modificar ni eliminar ítems.'
        : 'Al abrirla los perfiles podrán modificar el acta de nuevo.',
      showCancelButton: true,
      confirmButtonText: cerrar ? 'Cerrar' : 'Abrir',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#1abb9c',
    })
    if (!confirmacion.isConfirmed) {
      return
    }

    setGuardando(true)
    try {
      const { data } = await cliente.put<ActaGuardada>(`/api/actas/${numeroRuta}/cierre`, {
        cerrada: cerrar,
        rowVersion: version || null,
      })
      aplicarActa(data)
      consultas.setQueryData(['acta', numeroRuta], data)
      await consultas.invalidateQueries({ queryKey: ['actas'] })
      await Swal.fire({
        icon: 'success',
        title: cerrar ? 'Acta cerrada' : 'Acta abierta',
        text: `Acta N° ${data.numeroActa}`,
        confirmButtonColor: '#1abb9c',
      })
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 409) {
        setConflicto(true)
        return
      }

      await avisar(mensajeError(error))
    } finally {
      setGuardando(false)
    }
  }

  async function recargarActa() {
    try {
      const { data } = await cliente.get<ActaGuardada>(`/api/actas/${numeroRuta}`)
      aplicarActa(data)
      consultas.setQueryData(['acta', numeroRuta], data)
      setConflicto(false)
    } catch (error) {
      await avisar(mensajeError(error))
    }
  }

  function aplicarActa(acta: ActaGuardada) {
    setFechaInicial(acta.fechaInicial.slice(0, 10))
    setFechaFinal(acta.fechaFinal.slice(0, 10))
    setArquitectoId(acta.arquitectoId)
    setRevisorId(acta.revisorId)
    setContratistaId(acta.contratistaId)
    setObservacion(acta.observacion ?? '')
    setDetalles(acta.detalles.map(lineaDesdeServidor))
    setNumeroActa(acta.numeroActa)
    setCerrada(acta.cerrada)
    setVersion(acta.rowVersion)
    setConflicto(false)
  }

  function limpiarFormulario() {
    setFechaInicial('')
    setFechaFinal('')
    setArquitectoId(0)
    setRevisorId(0)
    setContratistaId(0)
    setObservacion('')
    setDetalles([])
    setCerrada(false)
    setVersion('')
    setConflicto(false)
  }

  if (editando && !hidratada && actaConsulta.data) {
    aplicarActa(actaConsulta.data)
    setHidratada(true)
  }

  if (marcaNueva !== marcaAplicada) {
    setMarcaAplicada(marcaNueva)
    limpiarFormulario()
    setNumeroActa(null)
    setModalAbierto(false)
    setLineaClave(null)
    setLinea(lineaVacia())
    descartarBorradorActa()
  }

  if (editando && actaConsulta.isError) {
    return (
      <section>
        <p className="mb-2">
          <Link to="/actas">Volver a actas</Link>
        </p>
        <p>{mensajeError(actaConsulta.error)}</p>
      </section>
    )
  }

  if (editando && !hidratada) {
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

function construirLinea(
  formulario: LineaFormulario,
  construcciones: RegistroCatalogo[] | undefined,
  conceptos: RegistroCatalogo[] | undefined,
  unidades: RegistroCatalogo[] | undefined,
  insumos: RegistroCatalogo[] | undefined,
): { ok: true; linea: Omit<LineaBorrador, 'clave'> } | { ok: false; error: string } {
  const construccion = buscar(construcciones, Number(formulario.construccionId))
  const concepto = buscar(conceptos, Number(formulario.conceptoId))
  const unidad = buscar(unidades, Number(formulario.unidadMedidaId))
  const insumo = formulario.insumoId ? buscar(insumos, Number(formulario.insumoId)) : null
  const cantidad = numero(formulario.cantidad)
  const precio = numero(formulario.precio)
  const descripcion = formulario.descripcion.trim()
  const faltantes: string[] = []
  if (!construccion) {
    faltantes.push('la construcción')
  }
  if (!concepto) {
    faltantes.push('el concepto')
  }
  if (formulario.unidadMedidaId && !unidad) {
    faltantes.push('la unidad de medida')
  }
  if (formulario.insumoId && !insumo) {
    faltantes.push('el insumo')
  }
  if (cantidad == null || cantidad <= 0) {
    faltantes.push('la cantidad')
  }
  if (precio == null || precio < 0) {
    faltantes.push('el precio')
  }
  if (faltantes.length > 0 || !construccion || !concepto || cantidad == null || precio == null) {
    return { ok: false, error: `Debe indicar ${enumerar(faltantes)}.` }
  }

  return {
    ok: true,
    linea: {
        id: null,
        tipoUsuarioId: perfilArquitecto,
        tipoUsuario: 'Arquitecto',
        construccionId: construccion.id,
      construccion: String(construccion.descripcion ?? ''),
      conceptoId: concepto.id,
      concepto: String(concepto.descripcion ?? ''),
      unidadMedidaId: unidad?.id ?? null,
      unidadMedida: unidad ? String(unidad.nombre ?? '') : '',
      insumoId: insumo?.id ?? null,
      insumo: insumo ? String(insumo.descripcion ?? '') : null,
      cantidad: redondear(cantidad, 4),
      precio: redondear(precio, 2),
      descripcion,
      aprobado: true,
      presupuestoItemId: Number(formulario.presupuestoItemId) > 0 ? Number(formulario.presupuestoItemId) : null,
    },
  }
}

function buscar(opciones: RegistroCatalogo[] | undefined, id: number) {
  return (opciones ?? []).find((opcion) => opcion.id === id)
}

function validarEncabezado(
  fechaInicial: string,
  fechaFinal: string,
  arquitectoId: number,
  revisorId: number,
  contratistaId: number,
) {
  const faltantes: string[] = []
  if (!fechaInicial) {
    faltantes.push('la fecha inicial')
  }
  if (!fechaFinal) {
    faltantes.push('la fecha final')
  }
  if (arquitectoId <= 0) {
    faltantes.push('el arquitecto')
  }
  if (revisorId <= 0) {
    faltantes.push('el revisor')
  }
  if (contratistaId <= 0) {
    faltantes.push('el contratista')
  }
  if (faltantes.length > 0) {
    return `Debe seleccionar ${enumerar(faltantes)}.`
  }

  if (fechaFinal < fechaInicial) {
    return mensajeFechas
  }

  return null
}

function enumerar(partes: string[]) {
  if (partes.length === 1) {
    return partes[0]
  }

  return `${partes.slice(0, -1).join(', ')} y ${partes[partes.length - 1]}`
}

function lineaDesdeServidor(item: ActaGuardada['detalles'][number]): LineaBorrador {
  return {
    clave: `srv-${item.id}`,
    id: item.id,
    tipoUsuarioId: item.tipoUsuarioId,
    tipoUsuario: item.tipoUsuario,
    construccionId: item.construccionId,
    construccion: item.construccion,
    conceptoId: item.conceptoId,
    concepto: item.concepto,
    unidadMedidaId: item.unidadMedidaId,
    unidadMedida: item.unidadMedida,
    insumoId: item.insumoId,
    insumo: item.insumo,
    cantidad: item.cantidad,
    precio: item.precio,
    descripcion: item.descripcion,
    aprobado: item.aprobado !== false,
    presupuestoItemId: item.presupuestoItemId ?? null,
  }
}

function avisar(texto: string) {
  return Swal.fire({ icon: 'error', title: '¡Error!', text: texto, confirmButtonColor: '#1abb9c' })
}
