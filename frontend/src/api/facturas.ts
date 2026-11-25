export type LineaFactura = {
  id: number
  construccionId: number
  construccion: string
  insumoId: number
  codigoInsumo: string | null
  insumo: string
  unidadMedidaId: number | null
  unidadMedida: string
  cantidad: number
  precio: number
  descripcion: string
  subtotal: number
}

export type Factura = {
  id: number
  numero: string
  fecha: string
  proveedorId: number
  proveedor: string
  ordenCompra: string | null
  observacion: string | null
  fechaRegistro: string
  usuarioRegistroId: number | null
  total: number
  detalles: LineaFactura[]
  rowVersion: string
}

export type FacturaResumen = {
  id: number
  numero: string
  fecha: string
  proveedor: string
  ordenCompra: string | null
  observacion: string | null
  total: number
}

export type ConsultaFacturas = {
  items: FacturaResumen[]
  total: number
  pagina: number
  tamano: number
  totalGeneral: number
}

export type ItemFacturaInforme = {
  facturaId: number | null
  numero: string | null
  fecha: string | null
  construccion: string
  codigo: string
  insumo: string
  descripcion: string
  unidad: string
  cantidad: number
  precio: number
  subtotal: number
}

export type InformeFacturas = {
  agrupado: boolean
  desde: string | null
  hasta: string | null
  items: ItemFacturaInforme[]
  total: number
  encabezado: string[]
  cantidad: number
  pagina: number
  tamano: number
}

export type LineaFacturaSolicitud = {
  construccionId: number
  insumoId: number
  unidadMedidaId: number | null
  cantidad: number
  precio: number
  descripcion: string
}

export type FacturaSolicitud = {
  numero: string
  fecha: string
  proveedorId: number
  ordenCompra: string | null
  observacion: string | null
  detalles: LineaFacturaSolicitud[]
  rowVersion?: string | null
}
