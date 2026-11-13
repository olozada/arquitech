import type { CapituloPresupuesto, PresupuestoDetalle } from '../api/presupuesto'

export function totalCapitulo(capitulo: CapituloPresupuesto) {
  return capitulo.items.reduce((suma, item) => suma + item.cantidad * (item.valorMaterialUnitario + item.valorManoObraUnitario), 0)
}

export function calcular(presupuesto: PresupuestoDetalle) {
  const subtotal = presupuesto.capitulos.reduce((suma, capitulo) => suma + totalCapitulo(capitulo), 0)
  const imprevistos = subtotal * presupuesto.pctImprevistos
  const incremento = subtotal * presupuesto.pctIncremento
  const administracion = subtotal * presupuesto.pctAdministracion
  const utilidad = subtotal * presupuesto.pctUtilidad
  const iva = utilidad * presupuesto.pctIvaUtilidad
  const total = subtotal + imprevistos + incremento + administracion + utilidad + iva
  return { subtotal, imprevistos, incremento, administracion, utilidad, iva, total, m2: presupuesto.areaConstruidaM2 > 0 ? total / presupuesto.areaConstruidaM2 : null }
}

export type ResumenCosto = ReturnType<typeof calcular>

export function cuerpo(presupuesto: PresupuestoDetalle) {
  return {
    construccionId: presupuesto.construccionId,
    nombre: presupuesto.nombre,
    rowVersion: presupuesto.rowVersion,
    fecha: presupuesto.fecha,
    mesesProyecto: presupuesto.mesesProyecto,
    areaConstruidaM2: presupuesto.areaConstruidaM2,
    pctImprevistos: presupuesto.pctImprevistos,
    pctIncremento: presupuesto.pctIncremento,
    pctAdministracion: presupuesto.pctAdministracion,
    pctUtilidad: presupuesto.pctUtilidad,
    pctIvaUtilidad: presupuesto.pctIvaUtilidad,
    capitulos: presupuesto.capitulos.map((capitulo) => ({
      id: capitulo.id >= 0 ? capitulo.id : null,
      numero: capitulo.numero,
      nombre: capitulo.nombre,
      items: capitulo.items.map((item) => ({
        id: item.id >= 0 ? item.id : null,
        codigo: item.codigo,
        descripcion: item.descripcion,
        unidad: item.unidad,
        unidadMedidaId: item.unidadMedidaId,
        apuId: item.apuId,
        factorConversionApu: item.factorConversionApu || 1,
        conceptoId: item.conceptoId,
        cantidad: item.cantidad,
        cantidadOrigenItemId: item.cantidadOrigenItemId,
        cantidadOrigenFactor: item.cantidadOrigenFactor || 1,
        valorMaterialUnitario: item.valorMaterialUnitario,
        valorManoObraUnitario: item.valorManoObraUnitario,
        mediciones: item.mediciones.map((medicion) => ({
          ubicacion: medicion.ubicacion,
          veces: medicion.veces || 1,
          largo: medicion.largo,
          ancho: medicion.ancho,
          alto: medicion.alto,
        })),
      })),
    })),
  }
}
