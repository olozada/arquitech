import { useQuery } from '@tanstack/react-query'
import { cliente } from './cliente'
import type { FiltroDashboard } from '../dashboard/filtroUrl'

export type Kpi = {
  codigo: string
  titulo: string
  valor: number
  valorAnterior: number | null
  variacionPct: number | null
  formato: 'moneda' | 'entero' | 'dias' | 'pct'
  complemento: number | null
}

export type PuntoMensual = {
  anio: number
  mes: number
  valor: number
  actas: number
  valorAnioAnterior: number | null
}

export type Ranking = {
  id: number
  nombre: string
  valor: number
  actas: number
  participacion: number
  participacionAcumulada: number
}

export type Alerta = {
  tipo: string
  severidad: 'Alta' | 'Media' | 'Baja'
  titulo: string
  detalle: string
  numeroActa: number | null
  construccionId: number | null
  construccion: string | null
  impacto: number | null
}

export type CalidadDatos = {
  pctMaterialesConPrecio: number
  materialesUnProveedor: number
  lineasSinMaterial: number
  actasSinObservacion: number
}

export type DashboardDatos = {
  filtro: {
    desde: string
    hasta: string
  }
  desdeAnterior: string
  hastaAnterior: string
  kpis: Kpi[]
  tendencia: PuntoMensual[]
  topConstrucciones: Ranking[]
  contratistas: Ranking[]
  conceptos: Ranking[]
  arquitectos: Ranking[]
  revisores: Ranking[]
  alertasPorSeveridad: Record<string, number>
  alertasTop: Alerta[]
  calidad: CalidadDatos
}

export type PaginaAlertas = {
  alertas: Alerta[]
  total: number
  pagina: number
  tamano: number
}

export type PuntoPrecio = {
  anio: number
  mes: number
  promedio: number
  minimo: number
  maximo: number
  lineas: number
}

export function useDashboard(filtro: FiltroDashboard) {
  return useQuery({
    queryKey: ['dashboard', filtro],
    queryFn: async () => (await cliente.get<DashboardDatos>('/api/dashboard', { params: parametrosDe(filtro) })).data,
  })
}

export function useAlertas(filtro: FiltroDashboard, tipo: string, severidad: string, pagina: number) {
  return useQuery({
    queryKey: ['dashboard-alertas', filtro, tipo, severidad, pagina],
    queryFn: async () =>
      (
        await cliente.get<PaginaAlertas>('/api/dashboard/alertas', {
          params: { ...parametrosDe(filtro), tipo: tipo || undefined, severidad: severidad || undefined, pagina },
        })
      ).data,
  })
}

export function usePrecioUnitario(
  desde: string,
  hasta: string,
  conceptoId: number,
  insumoId: number,
  estado: FiltroDashboard['estado'],
) {
  return useQuery({
    queryKey: ['dashboard-precio', desde, hasta, conceptoId, insumoId, estado],
    enabled: conceptoId > 0 || insumoId > 0,
    queryFn: async () =>
      (
        await cliente.get<PuntoPrecio[]>('/api/dashboard/precio-unitario', {
          params: {
            desde,
            hasta,
            conceptoId: conceptoId > 0 ? conceptoId : undefined,
            insumoId: insumoId > 0 ? insumoId : undefined,
            estado: estado || undefined,
          },
        })
      ).data,
  })
}

function parametrosDe(filtro: FiltroDashboard) {
  return {
    desde: filtro.desde,
    hasta: filtro.hasta,
    construccionId: filtro.construccionId > 0 ? filtro.construccionId : undefined,
    contratistaId: filtro.contratistaId > 0 ? filtro.contratistaId : undefined,
    arquitectoId: filtro.arquitectoId > 0 ? filtro.arquitectoId : undefined,
    revisorId: filtro.revisorId > 0 ? filtro.revisorId : undefined,
    estado: filtro.estado || undefined,
  }
}
