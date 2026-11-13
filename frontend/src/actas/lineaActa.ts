export type LineaFormulario = {
  construccionId: string
  conceptoId: string
  unidadMedidaId: string
  insumoId: string
  cantidad: string
  precio: string
  descripcion: string
  presupuestoItemId: string
}

export type { ItemPresupuestoActa } from '../api/actas'

export function lineaVacia(): LineaFormulario {
  return {
    construccionId: '',
    conceptoId: '',
    unidadMedidaId: '',
    insumoId: '',
    cantidad: '',
    precio: '',
    descripcion: '',
    presupuestoItemId: '',
  }
}
