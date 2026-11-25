import type { ItemPresupuesto } from '../api/presupuesto'
import { evaluar } from './CeldaNumero'
import { resolverUnidad } from './unidades'

export const columnasPegado = ['codigo', 'descripcion', 'unidad', 'cantidad', 'material', 'mo'] as const

export type ColumnaPegado = (typeof columnasPegado)[number]

export function interpretarPegado(
  texto: string,
  columnaInicial: ColumnaPegado,
  unidades: Array<{ id: number; nombre: string }>,
  cantidadDe: (codigo: string) => number | undefined,
): Partial<ItemPresupuesto>[] {
  const indice0 = columnasPegado.indexOf(columnaInicial)
  if (indice0 < 0) return []
  const filas = texto
    .replace(/\r\n/g, '\n')
    .split('\n')
    .map((fila) => fila.split('\t'))
    .filter((celdas) => celdas.some((c) => c.trim().length > 0))

  return filas.map((celdas) => {
    const parcial: Partial<ItemPresupuesto> = {}
    celdas.forEach((celda, i) => {
      const col = columnasPegado[indice0 + i]
      if (!col) return
      const valor = celda.trim()
      if (!valor) return
      switch (col) {
        case 'codigo':
          parcial.codigo = valor
          break
        case 'descripcion':
          parcial.descripcion = valor
          break
        case 'unidad': {
          const u = resolverUnidad(valor, unidades)
          if (u) {
            parcial.unidadMedidaId = u.id
            parcial.unidad = u.nombre
          } else {
            parcial.unidad = valor
          }
          break
        }
        case 'cantidad': {
          if (valor.startsWith('=') || valor.includes('[')) {
            parcial.formulaCantidad = valor.startsWith('=') ? valor : `=${valor}`
            const n = evaluar(parcial.formulaCantidad, cantidadDe)
            if (n != null) parcial.cantidad = n
          } else {
            const n = evaluar(valor, cantidadDe)
            parcial.cantidad = n ?? (Number(valor.replace(',', '.')) || 0)
            parcial.formulaCantidad = null
          }
          break
        }
        case 'material':
          parcial.valorMaterialUnitario = evaluar(valor, cantidadDe) ?? (Number(valor.replace(/\./g, '').replace(',', '.')) || 0)
          parcial.desdeApu = false
          break
        case 'mo':
          parcial.valorManoObraUnitario = evaluar(valor, cantidadDe) ?? (Number(valor.replace(/\./g, '').replace(',', '.')) || 0)
          parcial.desdeApu = false
          break
      }
    })
    return parcial
  })
}