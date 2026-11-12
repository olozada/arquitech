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

export type ItemPresupuestoActa = {
  id: number
  codigo: string
  descripcion: string
  unidad: string
  conceptoId: number | null
  unidadMedidaId: number | null
  precio: number | null
}

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
