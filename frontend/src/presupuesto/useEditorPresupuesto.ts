import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import axios from 'axios'
import { useQueryClient } from '@tanstack/react-query'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { usePresupuesto } from '../api/presupuesto'
import type { ItemPresupuesto, PresupuestoDetalle } from '../api/presupuesto'
import { calcular, cuerpo } from './costosPresupuesto'
import { recalcularFormulas } from './formulasPresupuesto'
import { cambiosAParche, fusionarParche, type CamposParche, type ParchePresupuesto } from './parcheItem'

function fusionarTrasGuardado(actual: PresupuestoDetalle, data: PresupuestoDetalle): PresupuestoDetalle {
  return {
    ...actual,
    rowVersion: data.rowVersion,
    capitulos: actual.capitulos.map((capitulo, indiceCapitulo) => ({
      ...capitulo,
      id: capitulo.id < 0 ? data.capitulos[indiceCapitulo]?.id ?? capitulo.id : capitulo.id,
      items: capitulo.items.map((item, indiceItem) =>
        item.id < 0 ? { ...item, id: data.capitulos[indiceCapitulo]?.items[indiceItem]?.id ?? item.id } : item,
      ),
    })),
  }
}

function hayPendientesSinGuardar(presupuesto: PresupuestoDetalle) {
  return presupuesto.capitulos.some(
    (capitulo) => capitulo.id < 0 || capitulo.items.some((item) => item.id < 0),
  )
}

const maxHistorial = 50

export function useEditorPresupuesto(presupuestoId: number, administrador: boolean) {
  const consulta = usePresupuesto(presupuestoId)
  const [borrador, setBorradorInterno] = useState<PresupuestoDetalle | null>(null)
  const [servidor, setServidor] = useState(consulta.data)
  const [guardando, setGuardando] = useState(false)
  const [sucio, setSucio] = useState(false)
  const [avisoGuardado, setAvisoGuardado] = useState('')
  const [conflicto, setConflicto] = useState(false)
  const espera = useRef<number | undefined>(undefined)
  const esperaParche = useRef<number | undefined>(undefined)
  const revisionEdicion = useRef(0)
  const consultas = useQueryClient()
  const colaParches = useRef<Map<number, CamposParche>>(new Map())
  const historial = useRef<PresupuestoDetalle[]>([])
  const rehacer = useRef<PresupuestoDetalle[]>([])
  const borradorRef = useRef(borrador)
  borradorRef.current = borrador

  const sustituirBorrador = useCallback((data: PresupuestoDetalle | null) => {
    historial.current = []
    rehacer.current = []
    colaParches.current.clear()
    setBorradorInterno(data)
  }, [])

  const setBorrador = useCallback(
    (actualizado: PresupuestoDetalle | null | ((prev: PresupuestoDetalle | null) => PresupuestoDetalle | null)) => {
      revisionEdicion.current += 1
      setBorradorInterno(actualizado)
    },
    [],
  )

  const apilarHistorial = useCallback(() => {
    if (!borradorRef.current) return
    historial.current.push(structuredClone(borradorRef.current))
    if (historial.current.length > maxHistorial) {
      historial.current.shift()
    }
    rehacer.current = []
  }, [])

  if (consulta.data !== servidor) {
    setServidor(consulta.data)
    if (consulta.data) {
      sustituirBorrador(structuredClone(consulta.data))
    }
  }

  const resumen = useMemo(() => (borrador ? calcular(borrador) : null), [borrador])
  const editable = administrador && borrador?.estado === 'Borrador'

  const vaciarParches = useCallback(async () => {
    const actual = borradorRef.current
    if (!actual || colaParches.current.size === 0) {
      return
    }
    const entradas = [...colaParches.current.entries()]
    colaParches.current.clear()
    const revisionAlInicio = revisionEdicion.current
    setGuardando(true)
    setAvisoGuardado('Guardando…')
    try {
      let fusionado = actual
      for (const [itemId, cuerpoParche] of entradas) {
        const { data } = await cliente.patch<ParchePresupuesto>(`/api/presupuestos/${actual.id}/items/${itemId}`, {
          ...cuerpoParche,
          rowVersion: fusionado.rowVersion,
        })
        fusionado = fusionarParche(fusionado, data)
      }
      setConflicto(false)
      const siguenEdiciones = revisionEdicion.current !== revisionAlInicio
      setBorradorInterno(fusionado)
      const quedaPut = siguenEdiciones || hayPendientesSinGuardar(fusionado) || sucio
      setSucio(quedaPut)
      setAvisoGuardado('Guardado')
      await consultas.invalidateQueries({ queryKey: ['presupuesto', actual.id] })
    } catch (error) {
      setAvisoGuardado('Error')
      entradas.forEach(([itemId, cuerpoParche]) => {
        colaParches.current.set(itemId, { ...colaParches.current.get(itemId), ...cuerpoParche })
      })
      if (axios.isAxiosError(error) && error.response?.status === 409) {
        setConflicto(true)
        return
      }
    } finally {
      setGuardando(false)
    }
  }, [consultas, sucio])

  const encolarParche = useCallback(
    (item: ItemPresupuesto, cambios: Partial<ItemPresupuesto>) => {
      if (item.id < 0) {
        return
      }
      const previo = colaParches.current.get(item.id)
      const base = cambiosAParche(cambios, borradorRef.current?.rowVersion ?? '')
      const fusion: CamposParche = {
        ...previo,
        ...base,
        rowVersion: borradorRef.current?.rowVersion ?? base.rowVersion,
      }
      colaParches.current.set(item.id, fusion)
      window.clearTimeout(esperaParche.current)
      esperaParche.current = window.setTimeout(() => {
        void vaciarParches()
      }, 700)
    },
    [vaciarParches],
  )

  const cambiarItem = useCallback(
    (indiceCapitulo: number, indiceItem: number, cambios: Partial<ItemPresupuesto>) => {
      apilarHistorial()
      setBorradorInterno((prev) => {
        if (!prev) {
          return prev
        }
        const copia = structuredClone(prev)
        const item = copia.capitulos[indiceCapitulo].items[indiceItem]
        copia.capitulos[indiceCapitulo].items[indiceItem] = { ...item, ...cambios }
        const recalculado = recalcularFormulas(copia)
        if (item.id >= 0) {
          encolarParche(item, cambios)
        } else {
          setSucio(true)
        }
        return recalculado
      })
    },
    [apilarHistorial, encolarParche],
  )

  const plantillaItem = (): ItemPresupuesto => ({
    id: -1,
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
    formulaCantidad: null,
    cantidadOrigenItemId: null,
    cantidadOrigenFactor: 1,
    valorMaterialUnitario: 0,
    valorManoObraUnitario: 0,
    valorUnitario: 0,
    total: 0,
    desdeApu: false,
    mediciones: [],
  })

  const idPegado = useRef(-100_000)
  const pegarFilas = useCallback(
    (indiceCapitulo: number, indiceInicio: number, filas: Partial<ItemPresupuesto>[]) => {
      if (filas.length === 0) {
        return
      }
      apilarHistorial()
      setBorradorInterno((prev) => {
        if (!prev) {
          return prev
        }
        const copia = structuredClone(prev)
        const items = copia.capitulos[indiceCapitulo].items
        while (items.length < indiceInicio + filas.length) {
          idPegado.current -= 1
          items.push({ ...plantillaItem(), id: idPegado.current })
        }
        filas.forEach((parcial, offset) => {
          const destino = indiceInicio + offset
          items[destino] = { ...items[destino], ...parcial }
        })
        setSucio(true)
        return recalcularFormulas(copia)
      })
    },
    [apilarHistorial],
  )

  const deshacer = useCallback(() => {
    const anterior = historial.current.pop()
    if (!anterior || !borradorRef.current) {
      return
    }
    rehacer.current.push(structuredClone(borradorRef.current))
    setBorradorInterno(anterior)
    revisionEdicion.current += 1
  }, [])

  const rehacerPaso = useCallback(() => {
    const siguiente = rehacer.current.pop()
    if (!siguiente || !borradorRef.current) {
      return
    }
    historial.current.push(structuredClone(borradorRef.current))
    setBorradorInterno(siguiente)
    revisionEdicion.current += 1
  }, [])

  const guardar = useCallback(
    async (silencioso = false) => {
      if (!borrador) {
        return
      }
      if (colaParches.current.size > 0) {
        await vaciarParches()
      }
      if (!hayPendientesSinGuardar(borrador) && !sucio) {
        return
      }
      const revisionAlInicio = revisionEdicion.current
      setGuardando(true)
      setAvisoGuardado('Guardando…')
      try {
        const { data } = await cliente.put<PresupuestoDetalle>(`/api/presupuestos/${borrador.id}`, cuerpo(borrador))
        setConflicto(false)
        if (silencioso) {
          setBorradorInterno((actual) => (actual ? fusionarTrasGuardado(actual, data) : data))
          const siguenEdiciones = revisionEdicion.current !== revisionAlInicio
          setSucio(siguenEdiciones || hayPendientesSinGuardar(data))
        } else {
          sustituirBorrador(data)
          setSucio(false)
        }
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
    },
    [borrador, consultas, sucio, sustituirBorrador, vaciarParches],
  )

  useEffect(() => {
    if (!sucio || !editable) {
      return
    }
    window.clearTimeout(espera.current)
    espera.current = window.setTimeout(() => {
      void guardar(true)
    }, 1200)
    return () => window.clearTimeout(espera.current)
  }, [borrador, sucio, editable, guardar])

  useEffect(() => {
    if (!editable) {
      return
    }
    function atajo(evento: KeyboardEvent) {
      if (!(evento.ctrlKey || evento.metaKey)) {
        return
      }
      const destino = evento.target as HTMLElement | null
      if (destino && (destino.tagName === 'INPUT' || destino.tagName === 'TEXTAREA' || destino.isContentEditable)) {
        return
      }
      if (evento.key === 'z' && !evento.shiftKey) {
        evento.preventDefault()
        deshacer()
      }
      if (evento.key === 'y' || (evento.key === 'z' && evento.shiftKey)) {
        evento.preventDefault()
        rehacerPaso()
      }
    }
    window.addEventListener('keydown', atajo)
    return () => window.removeEventListener('keydown', atajo)
  }, [deshacer, editable, rehacerPaso])

  useEffect(() => {
    if (!editable || !borrador) {
      return
    }
    function guardarSiVisible() {
      if (document.visibilityState === 'hidden' && (sucio || colaParches.current.size > 0)) {
        void guardar(true)
      }
    }
    const alSalir = () => {
      if (sucio || colaParches.current.size > 0) {
        void guardar(true)
      }
    }
    document.addEventListener('visibilitychange', guardarSiVisible)
    window.addEventListener('beforeunload', alSalir)
    return () => {
      document.removeEventListener('visibilitychange', guardarSiVisible)
      window.removeEventListener('beforeunload', alSalir)
    }
  }, [borrador, editable, guardar, sucio])

  return {
    consulta,
    borrador,
    setBorrador,
    sustituirBorrador,
    cambiarItem,
    pegarFilas,
    deshacer,
    rehacer: rehacerPaso,
    sucio,
    setSucio,
    guardando,
    setGuardando,
    avisoGuardado,
    conflicto,
    resumen,
    guardar,
  }
}