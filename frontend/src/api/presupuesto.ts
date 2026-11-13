import { useQuery } from '@tanstack/react-query'
import { cliente } from './cliente'
import type { components } from './esquema'

type Esquemas = components['schemas']

export type CostoPresupuesto = Esquemas['CostoPresupuestoDto']
export type Medicion = Esquemas['MedicionDto']
export type ItemPresupuesto = Esquemas['ItemPresupuestoDto']
export type CapituloPresupuesto = Esquemas['CapituloPresupuestoDto']
export type PresupuestoDetalle = Esquemas['PresupuestoDetalleDto']
export type ConfiguracionPresupuesto = Esquemas['OpcionesPresupuesto']
export type PlantillaResumen = Esquemas['PlantillaResumenDto']
export type AvisoValidacion = Esquemas['ValidacionPresupuestoDto']
export type PresupuestoResumen = Esquemas['PresupuestoResumenDto']
export type InformeImportacion = Esquemas['InformeImportacionDto']
export type Recalculo = Esquemas['RecalculoDto']
export type Seguimiento = Esquemas['SeguimientoDto']

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
    enabled: Number.isInteger(id) && id >= 0,
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
