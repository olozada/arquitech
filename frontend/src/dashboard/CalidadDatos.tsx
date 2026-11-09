import type { CalidadDatos as Calidad } from '../api/dashboard'

export function CalidadDatos({ calidad }: { calidad: Calidad }) {
  return (
    <section className="catalogo-panel h-100">
      <h2>Calidad de datos</h2>
      <dl className="calidad-lista">
        <div>
          <dt>Materiales con precio de proveedor</dt>
          <dd>{calidad.pctMaterialesConPrecio.toLocaleString('es-CO', { maximumFractionDigits: 1 })} %</dd>
        </div>
        <div>
          <dt>Materiales con un solo proveedor</dt>
          <dd>{calidad.materialesUnProveedor}</dd>
        </div>
        <div>
          <dt>Líneas de acta sin material</dt>
          <dd>{calidad.lineasSinMaterial}</dd>
        </div>
        <div>
          <dt>Actas sin observación</dt>
          <dd>{calidad.actasSinObservacion}</dd>
        </div>
      </dl>
    </section>
  )
}
