import { dinero as formatoDinero } from '../actas/formato'

export function dinero(valor: number) {
  return formatoDinero(valor, 2)
}

export type Linea = {
  clave: number
  insumoId: number | null
  cuadrillaId: number | null
  subApuId: number | null
  descripcion: string
  unidad: string
  cantidad: number
  precioManual: number | null
  modo: string
  factor: number
  precio: number
}

export type Grupo = { clave: number; grupo: string; divisor: number; lineas: Linea[] }

export type ApuServidor = {
  id: number
  codigo: string | null
  descripcion: string
  unidad: string
  unidadMedidaId: number | null
  esSubanalisis: boolean
  divisorGeneral: number
  conceptoId: number | null
  activo: boolean
  costoTotal: number
  grupos: Array<{
    id: number
    grupo: string
    divisor: number
    subtotal: number
    lineas: Array<Omit<Linea, 'clave'> & { id: number }>
  }>
}

export type Uso = { tipo: string; id: number; nombre: string; aprobado: boolean }
export type Unidad = { id: number; nombre: string; activo: boolean }
export type Concepto = { id: number; descripcion: string; activo: boolean }

export const gruposPosibles = ['Material', 'ManoObra', 'Equipo', 'Transporte', 'Otros']
export const modos = ['Desperdicio', 'Rendimiento', 'Divisor', 'PorcentajeManoObra']

let clave = 1
export const nuevaClave = () => clave++

export function desdeServidor(apu: ApuServidor): Grupo[] {
  return apu.grupos.map((grupo) => ({
    clave: nuevaClave(),
    grupo: grupo.grupo,
    divisor: grupo.divisor,
    lineas: grupo.lineas.map((linea) => ({ ...linea, clave: nuevaClave() })),
  }))
}

export function vacio(unidad: string): { descripcion: string; unidad: string; grupos: Grupo[] } {
  return {
    descripcion: 'Nuevo análisis',
    unidad,
    grupos: [
      { clave: nuevaClave(), grupo: 'Material', divisor: 1, lineas: [] },
      { clave: nuevaClave(), grupo: 'ManoObra', divisor: 1, lineas: [] },
    ],
  }
}

export function ayuda(linea: Linea, grupo: string) {
  if (linea.modo === 'PorcentajeManoObra') {
    return grupo === 'ManoObra' ? 'porcentaje sobre la mano de obra del grupo (0,10)' : ''
  }
  if (linea.modo === 'Rendimiento' && linea.cuadrillaId) {
    return `rendimiento (unidades por día). Factor guardado ${linea.factor.toLocaleString('es-CO', { maximumFractionDigits: 4 })}`
  }
  if (linea.modo === 'Rendimiento') {
    return 'el factor multiplica cantidad × precio'
  }
  if (linea.modo === 'Divisor') {
    return 'usos por unidad'
  }
  return '% desperdicio (0,05 = 5 %)'
}
