import { fecha, fechaConsulta } from '../actas/formato'
export type { ItemMaterial, InformeMateriales } from '../api/actas'
import type { RegistroCatalogo } from '../catalogos/definiciones'
import type { EstadoActa } from '../dashboard/filtroUrl'

export type VistaInforme = 'detallado' | 'agrupado'

export type Filtro = {
  usarFechas: boolean
  desde: string
  hasta: string
  construccionId: number
  contratistaId: number
  arquitectoId: number
  revisorId: number
  proveedorId: number
  estado: EstadoActa
  vista: VistaInforme
}

export type Campo = 'acta' | 'codigo' | 'material' | 'unidad' | 'cantidad' | 'precio' | 'estado' | 'subtotal'
export type Direccion = 'asc' | 'desc'

export const TAMANO_PAGINA = 20

export function lineasEncabezado(
  filtro: Filtro,
  construcciones: RegistroCatalogo[],
  arquitectos: RegistroCatalogo[],
  revisores: RegistroCatalogo[],
  contratistas: RegistroCatalogo[],
  proveedores: RegistroCatalogo[],
) {
  const lineas: string[] = []
  const desde = fechaConsulta(filtro.usarFechas, filtro.desde)
  const hasta = fechaConsulta(filtro.usarFechas, filtro.hasta)
  if (desde && hasta) lineas.push(`Del ${fecha(desde)} al ${fecha(hasta)}`)
  else if (desde) lineas.push(`Desde ${fecha(desde)}`)
  else if (hasta) lineas.push(`Hasta ${fecha(hasta)}`)

  const texto = (id: number, opciones: RegistroCatalogo[], etiqueta: string) => {
    const opcion = opciones.find((item) => item.id === id)
    const nombre = opcion ? String(opcion.nombre ?? opcion.descripcion ?? '') : ''
    if (nombre) lineas.push(`${etiqueta}: ${nombre}`)
  }

  texto(filtro.construccionId, construcciones, 'Construcción')
  texto(filtro.arquitectoId, arquitectos, 'Arquitecto')
  texto(filtro.revisorId, revisores, 'Revisor')
  texto(filtro.contratistaId, contratistas, 'Contratista')
  texto(filtro.proveedorId, proveedores, 'Proveedor')
  if (filtro.estado === 'abierta') lineas.push('Estado: Abierta')
  if (filtro.estado === 'cerrada') lineas.push('Estado: Cerrada')
  return lineas
}

export function parametros(filtro: Filtro) {
  return {
    desde: fechaConsulta(filtro.usarFechas, filtro.desde),
    hasta: fechaConsulta(filtro.usarFechas, filtro.hasta),
    construccionId: filtro.construccionId > 0 ? filtro.construccionId : undefined,
    contratistaId: filtro.contratistaId > 0 ? filtro.contratistaId : undefined,
    arquitectoId: filtro.arquitectoId > 0 ? filtro.arquitectoId : undefined,
    revisorId: filtro.revisorId > 0 ? filtro.revisorId : undefined,
    proveedorId: filtro.proveedorId > 0 ? filtro.proveedorId : undefined,
    estado: filtro.estado || undefined,
    vista: filtro.vista,
  }
}
