import { dinero } from '../actas/formato'
import type { ResumenCosto } from './costosPresupuesto'

export function ResumenCostos({ resumen }: { resumen: ResumenCosto }) {
  return (
    <div className="catalogo-panel presupuesto-resumen">
      <p>Subtotal {dinero(resumen.subtotal, 2)}</p>
      <p>Imprevistos {dinero(resumen.imprevistos, 2)} · Incremento {dinero(resumen.incremento, 2)}</p>
      <p>Administración {dinero(resumen.administracion, 2)} · Utilidad {dinero(resumen.utilidad, 2)} · IVA {dinero(resumen.iva, 2)}</p>
      <p className="presupuesto-total">Total {dinero(resumen.total, 2)}</p>
      <p>Valor por m² {resumen.m2 == null ? '—' : dinero(resumen.m2, 2)}</p>
    </div>
  )
}
