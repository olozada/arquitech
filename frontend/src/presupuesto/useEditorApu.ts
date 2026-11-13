import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { calcularApu } from './calculadoraApu'
import { dinero, desdeServidor, nuevaClave, vacio, type ApuServidor, type Concepto, type Grupo, type Linea, type Unidad, type Uso } from './modeloApu'
import type { OrigenLinea } from './SelectorOrigenLinea'

export type PropsEditorApu = {
  apuId: number | null
  unidadInicial?: string
  embebido?: boolean
  alGuardar?: (apu: ApuServidor) => void
  alCancelar?: () => void
}

export function useEditorApu({ apuId, unidadInicial, embebido, alGuardar, alCancelar }: PropsEditorApu) {
  const consulta = useQuery({
    queryKey: ['apu', apuId],
    enabled: apuId != null && apuId >= 0,
    queryFn: async () => (await cliente.get<ApuServidor>(`/api/apu/${apuId}`)).data,
  })
  const usos = useQuery({
    queryKey: ['apu-usos', apuId],
    enabled: apuId != null && apuId >= 0 && !embebido,
    queryFn: async () => (await cliente.get<Uso[]>(`/api/apu/${apuId}/usos`)).data,
  })
  const unidades = useQuery({
    queryKey: ['unidades-medida'],
    queryFn: async () => (await cliente.get<Unidad[]>('/api/unidades-medida')).data,
  })
  const conceptos = useQuery({
    queryKey: ['conceptos'],
    queryFn: async () => (await cliente.get<Concepto[]>('/api/conceptos')).data,
  })
  const [grupos, setGrupos] = useState<Grupo[]>(() => (apuId ? [] : vacio(unidadInicial ?? '').grupos))
  const [codigo, setCodigo] = useState<string | null>(null)
  const [descripcion, setDescripcion] = useState(() => (apuId ? '' : 'Nuevo análisis'))
  const [unidadMedidaId, setUnidadMedidaId] = useState<number | null>(null)
  const [esSubanalisis, setEsSubanalisis] = useState(false)
  const [divisorGeneral, setDivisorGeneral] = useState(1)
  const [conceptoId, setConceptoId] = useState<number | null>(null)
  const [activo, setActivo] = useState(true)
  const [origen, setOrigen] = useState<number | null>(null)
  const [aplicado, setAplicado] = useState<number | null>(null)
  const [listo, setListo] = useState(apuId == null)
  const consultas = useQueryClient()
  const navegar = useNavigate()

  if (consulta.data && !listo && aplicado !== consulta.data.id) {
    setAplicado(consulta.data.id)
    setCodigo(consulta.data.codigo)
    setDescripcion(consulta.data.descripcion)
    setUnidadMedidaId(consulta.data.unidadMedidaId)
    setEsSubanalisis(consulta.data.esSubanalisis)
    setDivisorGeneral(consulta.data.divisorGeneral || 1)
    setConceptoId(consulta.data.conceptoId)
    setActivo(consulta.data.activo)
    setGrupos(desdeServidor(consulta.data))
    setListo(true)
  }

  if (unidadMedidaId == null && unidadInicial && unidades.data) {
    const coincidencia = unidades.data.find((item) => item.nombre.toLowerCase() === unidadInicial.toLowerCase())
    if (coincidencia) {
      setUnidadMedidaId(coincidencia.id)
    }
  }

  const gruposActuales = grupos
  const unidad = (unidades.data ?? []).find((item) => item.id === unidadMedidaId)?.nombre ?? consulta.data?.unidad ?? unidadInicial ?? ''
  const calculo = useMemo(
    () =>
      calcularApu(
        gruposActuales.map((grupo) => ({
          tipo: grupo.grupo,
          divisor: grupo.divisor,
          lineas: grupo.lineas.map((linea) => ({
            modo: linea.modo,
            cantidad: linea.cantidad,
            factor: linea.factor,
            precio: linea.precioManual ?? linea.precio,
            herramientaMenor: linea.modo === 'PorcentajeManoObra',
          })),
        })),
        divisorGeneral,
      ),
    [gruposActuales, divisorGeneral],
  )

  function actualizarLinea(grupoClave: number, lineaClave: number, cambios: Partial<Linea>) {
    setGrupos(
      gruposActuales.map((grupo) =>
        grupo.clave === grupoClave
          ? { ...grupo, lineas: grupo.lineas.map((linea) => (linea.clave === lineaClave ? { ...linea, ...cambios } : linea)) }
          : grupo,
      ),
    )
  }

  async function guardar() {
    const cuerpo = {
      codigo,
      descripcion,
      unidad,
      unidadMedidaId,
      esSubanalisis,
      divisorGeneral: divisorGeneral || 1,
      conceptoId,
      activo,
      grupos: gruposActuales.map((grupo) => ({
        grupo: grupo.grupo,
        divisor: grupo.divisor || 1,
        lineas: grupo.lineas.map((linea) => ({
          insumoId: linea.insumoId,
          cuadrillaId: linea.cuadrillaId,
          subApuId: linea.subApuId,
          descripcion: linea.descripcion,
          unidad: linea.unidad,
          cantidad: linea.cantidad,
          precioManual: linea.insumoId || linea.cuadrillaId || linea.subApuId ? null : linea.precioManual,
          modo: linea.modo,
          factor: linea.factor,
        })),
      })),
    }
    const tieneValor = calculo.detalle.some((grupo) => grupo.lineas.some((valor) => valor > 0))
    if (apuId == null && !tieneValor) {
      await Swal.fire({ icon: 'warning', text: 'Agregue al menos una línea con valor mayor que cero.' })
      return
    }
    try {
      const { data } = apuId
        ? await cliente.put<ApuServidor>(`/api/apu/${apuId}`, cuerpo)
        : await cliente.post<ApuServidor>('/api/apu', cuerpo)
      if (Math.abs(data.costoTotal - calculo.total) > 1) {
        await Swal.fire({ icon: 'info', text: `El servidor calculó ${dinero(data.costoTotal)} y la vista previa ${dinero(calculo.total)}.` })
      }
      await consultas.invalidateQueries({ queryKey: ['apu', data.id] })
      await consultas.invalidateQueries({ queryKey: ['apus'] })
      alGuardar?.(data)
      if (!embebido && apuId == null) {
        navegar(`/presupuesto/apu/${data.id}`)
      }
    } catch (error) {
      const texto = mensajeError(error)
      await Swal.fire({
        icon: 'error',
        text: texto.toLowerCase().includes('circular') ? 'Este sub-análisis crearía una referencia circular' : texto,
      })
    }
  }

  async function duplicar() {
    if (!apuId) {
      return
    }
    const { data } = await cliente.post<ApuServidor>(`/api/apu/${apuId}/duplicar`)
    navegar(`/presupuesto/apu/${data.id}`)
  }

  function agregarGrupo(tipo: string) {
    if (!tipo) {
      return
    }
    setGrupos([...gruposActuales, { clave: nuevaClave(), grupo: tipo, divisor: 1, lineas: [] }])
  }

  async function quitarGrupo(grupoClave: number) {
    const confirmacion = await Swal.fire({ icon: 'warning', title: '¿Eliminar el grupo?', showCancelButton: true, confirmButtonColor: '#1abb9c' })
    if (confirmacion.isConfirmed) {
      setGrupos(gruposActuales.filter((grupo) => grupo.clave !== grupoClave))
    }
  }

  function agregarLinea(grupoClave: number, elegido: OrigenLinea) {
    const linea: Linea = {
      clave: nuevaClave(),
      insumoId: elegido.insumoId ?? null,
      cuadrillaId: elegido.cuadrillaId ?? null,
      subApuId: elegido.subApuId ?? null,
      descripcion: elegido.descripcion,
      unidad: elegido.unidad,
      cantidad: 1,
      precioManual: elegido.precioManual,
      modo: 'Desperdicio',
      factor: 0,
      precio: elegido.precio,
    }
    setGrupos(gruposActuales.map((grupo) => (grupo.clave === grupoClave ? { ...grupo, lineas: [...grupo.lineas, linea] } : grupo)))
  }

  function mover(grupoClave: number, indice: number, delta: number) {
    setGrupos(
      gruposActuales.map((grupo) => {
        if (grupo.clave !== grupoClave) {
          return grupo
        }
        const destino = indice + delta
        if (destino < 0 || destino >= grupo.lineas.length) {
          return grupo
        }
        const lineas = [...grupo.lineas]
        const [linea] = lineas.splice(indice, 1)
        lineas.splice(destino, 0, linea)
        return { ...grupo, lineas }
      }),
    )
  }

  const usadoEnAprobado = (usos.data ?? []).some((uso) => uso.aprobado)

  return {
    cargando: Boolean(apuId && !consulta.data),
    embebido,
    descripcion,
    alCancelar,
    apuId,
    duplicar,
    guardar,
    calculo,
    consulta,
    codigo,
    setCodigo,
    setDescripcion,
    unidadMedidaId,
    setUnidadMedidaId,
    unidades,
    conceptoId,
    setConceptoId,
    conceptos,
    divisorGeneral,
    setDivisorGeneral,
    esSubanalisis,
    setEsSubanalisis,
    activo,
    setActivo,
    gruposActuales,
    setGrupos,
    quitarGrupo,
    actualizarLinea,
    mover,
    setOrigen,
    agregarGrupo,
    usadoEnAprobado,
    usos,
    origen,
    agregarLinea,
  }
}

export type ModeloEditorApu = ReturnType<typeof useEditorApu>
