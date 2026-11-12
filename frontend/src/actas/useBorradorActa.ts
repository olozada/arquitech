import { useEffect } from 'react'
import { perfilArquitecto } from '../auth/perfiles'

export type LineaBorrador = {
  clave: string
  id: number | null
  tipoUsuarioId: number
  tipoUsuario: string
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
  aprobado: boolean
  presupuestoItemId: number | null
}

export type Borrador = {
  fechaInicial: string
  fechaFinal: string
  arquitectoId: number
  revisorId: number
  contratistaId: number
  observacion: string
  detalles: LineaBorrador[]
}

const CLAVE = 'arquitectos.borradorActa'

export function descartarBorradorActa() {
  localStorage.removeItem(CLAVE)
}

export function borradorVacio(): Borrador {
  return {
    fechaInicial: '',
    fechaFinal: '',
    arquitectoId: 0,
    revisorId: 0,
    contratistaId: 0,
    observacion: '',
    detalles: [],
  }
}

export function marcaDeActaNueva(estado: unknown) {
  if (!estado || typeof estado !== 'object' || !('nueva' in estado)) {
    return 0
  }

  const marca = (estado as { nueva?: unknown }).nueva
  return typeof marca === 'number' ? marca : 0
}

export function leerBorrador(): Borrador {
  const vacio = borradorVacio()
  try {
    const texto = localStorage.getItem(CLAVE)
    if (!texto) {
      return vacio
    }

    const valor = JSON.parse(texto) as Partial<Borrador>
    const detalles = Array.isArray(valor.detalles)
      ? valor.detalles.map((item) => ({
          ...item,
          id: typeof item.id === 'number' ? item.id : null,
          tipoUsuarioId: typeof item.tipoUsuarioId === 'number' ? item.tipoUsuarioId : perfilArquitecto,
          tipoUsuario: typeof item.tipoUsuario === 'string' ? item.tipoUsuario : 'Arquitecto',
          aprobado: item.aprobado !== false,
          presupuestoItemId: typeof item.presupuestoItemId === 'number' ? item.presupuestoItemId : null,
        }))
      : []
    return { ...vacio, ...valor, detalles }
  } catch {
    return vacio
  }
}

export function useBorradorActa(editando: boolean, marcaNueva: number, marcaAplicada: number, borrador: Borrador) {
  const { fechaInicial, fechaFinal, arquitectoId, revisorId, contratistaId, observacion, detalles } = borrador
  useEffect(() => {
    if (editando || marcaNueva !== marcaAplicada) {
      return
    }

    localStorage.setItem(CLAVE, JSON.stringify({ fechaInicial, fechaFinal, arquitectoId, revisorId, contratistaId, observacion, detalles }))
  }, [editando, marcaNueva, marcaAplicada, fechaInicial, fechaFinal, arquitectoId, revisorId, contratistaId, observacion, detalles])
}
