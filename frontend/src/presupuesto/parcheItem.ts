import type { CostoPresupuesto, ItemPresupuesto, PresupuestoDetalle } from '../api/presupuesto'

export type ParchePresupuesto = {
  rowVersion: string
  resumen: CostoPresupuesto
  items: ItemPresupuesto[]
}

export function fusionarParche(borrador: PresupuestoDetalle, parche: ParchePresupuesto): PresupuestoDetalle {
  const mapa = new Map(parche.items.map((fila) => [fila.id, fila]))
  return {
    ...borrador,
    rowVersion: parche.rowVersion,
    resumen: parche.resumen,
    capitulos: borrador.capitulos.map((capitulo) => ({
      ...capitulo,
      items: capitulo.items.map((item) => (mapa.has(item.id) ? { ...item, ...mapa.get(item.id)! } : item)),
    })),
  }
}

export type CamposParche = {
  rowVersion: string
  codigo?: string
  descripcion?: string
  unidad?: string
  unidadMedidaId?: number | null
  apuId?: number | null
  factorConversionApu?: number
  conceptoId?: number | null
  cantidad?: number
  formulaCantidad?: string | null
  valorMaterialUnitario?: number
  valorManoObraUnitario?: number
  desdeApu?: boolean
}

export function cambiosAParche(cambios: Partial<ItemPresupuesto>, rowVersion: string): CamposParche {
  const cuerpo: CamposParche = { rowVersion }
  if (cambios.codigo !== undefined) cuerpo.codigo = cambios.codigo
  if (cambios.descripcion !== undefined) cuerpo.descripcion = cambios.descripcion
  if (cambios.unidad !== undefined) cuerpo.unidad = cambios.unidad
  if (cambios.unidadMedidaId !== undefined) cuerpo.unidadMedidaId = cambios.unidadMedidaId
  if (cambios.apuId !== undefined) cuerpo.apuId = cambios.apuId
  if (cambios.factorConversionApu !== undefined) cuerpo.factorConversionApu = cambios.factorConversionApu
  if (cambios.conceptoId !== undefined) cuerpo.conceptoId = cambios.conceptoId
  if (cambios.cantidad !== undefined) cuerpo.cantidad = cambios.cantidad
  if ('formulaCantidad' in cambios) cuerpo.formulaCantidad = cambios.formulaCantidad ?? null
  if (cambios.valorMaterialUnitario !== undefined) cuerpo.valorMaterialUnitario = cambios.valorMaterialUnitario
  if (cambios.valorManoObraUnitario !== undefined) cuerpo.valorManoObraUnitario = cambios.valorManoObraUnitario
  if (cambios.desdeApu !== undefined) cuerpo.desdeApu = cambios.desdeApu
  return cuerpo
}