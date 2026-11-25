import type { PresupuestoDetalle } from '../api/presupuesto'
import { evaluar } from './CeldaNumero'

export function mapaCantidades(presupuesto: PresupuestoDetalle) {
  const mapa = new Map<string, number>()
  for (const item of presupuesto.capitulos.flatMap((c) => c.items)) {
    if (item.codigo.trim()) {
      mapa.set(item.codigo.trim(), item.cantidad)
    }
  }
  return mapa
}

export function recalcularFormulas(presupuesto: PresupuestoDetalle): PresupuestoDetalle {
  const copia = structuredClone(presupuesto)
  const cantidadDe = (codigo: string) => mapaCantidades(copia).get(codigo.trim())
  for (let paso = 0; paso < 8; paso++) {
    let cambio = false
    for (const capitulo of copia.capitulos) {
      for (const item of capitulo.items) {
        const formula = item.formulaCantidad
        if (!formula || item.mediciones.length > 0) continue
        const valor = evaluar(formula.startsWith('=') ? formula : `=${formula}`, cantidadDe)
        if (valor != null && valor !== item.cantidad) {
          item.cantidad = valor
          cambio = true
        }
      }
    }
    if (!cambio) break
  }
  return copia
}