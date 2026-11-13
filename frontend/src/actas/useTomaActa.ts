import { useState } from 'react'
import axios from 'axios'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { listarTodo } from '../api/pagina'
import { useSesion } from '../auth/contextoSesion'
import { esAdministrador, esRevisor, perfilDeNuevoItem } from '../auth/perfiles'
import type { ItemPresupuestoActa } from './lineaActa'
import { lineaVacia } from './lineaActa'
import { useCatalogo } from './useCatalogo'
import { borradorVacio, descartarBorradorActa, leerBorrador, marcaDeActaNueva, useBorradorActa, type LineaBorrador } from './useBorradorActa'
import type { ActaCreada } from '../api/actas'
import { avisar, construirLinea, lineaDesdeServidor, validarEncabezado, type ActaGuardada } from './lineaFormulario'

export function useTomaActa() {
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

      const { data } = await cliente.post<ActaCreada>('/api/actas', cuerpo)
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

  return {
    errorCarga: editando && actaConsulta.isError ? actaConsulta.error : null,
    cargando: editando && !hidratada,
    editando,
    numeroRuta,
    conflicto,
    recargarActa,
    cerrada,
    administrador,
    pestana,
    setPestana,
    fechaInicial,
    fechaFinal,
    arquitectoId,
    revisorId,
    contratistaId,
    revisor,
    arquitectos,
    revisores,
    contratistas,
    setFechaInicial,
    setFechaFinal,
    setArquitectoId,
    setRevisorId,
    setContratistaId,
    abrirNuevaLinea,
    detalles,
    sesion,
    total,
    todosRechazados,
    hayRechazo,
    revisarLinea,
    abrirLinea,
    observacion,
    setObservacion,
    guardando,
    registrar,
    cambiarCierre,
    cancelar,
    numeroActa,
    modalAbierto,
    lineaClave,
    linea,
    construcciones,
    conceptos,
    unidades,
    insumos,
    itemsPresupuesto,
    setModalAbierto,
    setLinea,
    guardarLinea,
    eliminarLinea,
  }
}
