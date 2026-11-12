import { useQuery } from '@tanstack/react-query'
import { cliente } from './cliente'

export type CostoPresupuesto = {
  subtotal: number
  totalMaterial: number
  totalManoObra: number
  imprevistos: number
  incremento: number
  administracion: number
  utilidad: number
  ivaUtilidad: number
  total: number
  valorPorM2: number | null
}

export type Medicion = {
  id: number
  ubicacion: string
  veces: number
  largo: number | null
  ancho: number | null
  alto: number | null
  parcial: number
}

export type ItemPresupuesto = {
  id: number
  codigo: string
  descripcion: string
  unidad: string
  unidadMedidaId: number | null
  apuId: number | null
  apu: string | null
  unidadApu: string | null
  factorConversionApu: number
  avisoUnidad: boolean
  conceptoId: number | null
  cantidad: number
  cantidadOrigenItemId: number | null
  cantidadOrigenFactor: number
  valorMaterialUnitario: number
  valorManoObraUnitario: number
  valorUnitario: number
  total: number
  desdeApu: boolean
  mediciones: Medicion[]
}

export type CapituloPresupuesto = {
  id: number
  numero: number
  nombre: string
  total: number
  incidencia: number
  items: ItemPresupuesto[]
}

export type PresupuestoDetalle = {
  id: number
  construccionId: number
  construccion: string
  nombre: string
  version: number
  estado: string
  fecha: string
  mesesProyecto: number
  areaConstruidaM2: number
  pctImprevistos: number
  pctIncremento: number
  pctAdministracion: number
  pctUtilidad: number
  pctIvaUtilidad: number
  resumen: CostoPresupuesto
  capitulos: CapituloPresupuesto[]
  rowVersion: string
  esPlantilla: boolean
}

export type ConfiguracionPresupuesto = {
  pctImprevistos: number
  pctIncremento: number
  pctAdministracion: number
  pctUtilidad: number
  pctIvaUtilidad: number
  autoguardadoMs: number
}

export type PlantillaResumen = {
  id: number
  nombre: string
  capitulos: number
  items: number
  actualizado: string
}

export type AvisoValidacion = {
  itemId: number | null
  codigo: string
  tipo: string
  mensaje: string
}

export type PresupuestoResumen = {
  id: number
  construccionId: number
  construccion: string
  nombre: string
  version: number
  estado: string
  total: number
  valorPorM2: number | null
  avanceFinanciero: number
}

export type InformeImportacion = {
  presupuestoId: number
  apusRevisados: number
  apusReproducidos: number
  unidadesDistintas: number
  codigosDuplicados: number
  itemsSinApu: number
  subtotal: number
  total: number
  valorPorM2: number
  unidadesQueNoCoinciden: string[]
  apusDistintos: string[]
}

export function usePresupuestos(construccionId: number) {
  return useQuery({
    queryKey: ['presupuestos', construccionId],
    queryFn: async () =>
      (await cliente.get<PresupuestoResumen[]>('/api/presupuestos', { params: { construccionId: construccionId || undefined } })).data,
  })
}

export function usePresupuesto(id: number) {
  return useQuery({
    queryKey: ['presupuesto', id],
    enabled: id > 0,
    queryFn: async () => (await cliente.get<PresupuestoDetalle>(`/api/presupuestos/${id}`)).data,
  })
}

export async function descargarPresupuesto(id: number, tipo: 'excel' | 'pdf') {
  const { data } = await cliente.get<Blob>(`/api/presupuestos/${id}/${tipo}`, { responseType: 'blob' })
  const url = URL.createObjectURL(data)
  const enlace = document.createElement('a')
  enlace.href = url
  enlace.download = `presupuesto-${id}.${tipo === 'excel' ? 'xlsx' : 'pdf'}`
  enlace.click()
  URL.revokeObjectURL(url)
}
