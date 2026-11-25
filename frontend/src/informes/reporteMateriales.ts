import { fecha, fechaConsulta } from '../actas/formato'
export type { ItemMaterial, InformeMateriales } from '../api/actas'
import type { RegistroCatalogo } from '../catalogos/definiciones'
import type { EstadoActa } from '../dashboard/filtroUrl'
import type { ItemMaterial } from '../api/actas'

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
  subtotales: boolean
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
    subtotales: filtro.vista === 'detallado' && filtro.subtotales ? true : undefined,
  }
}

export type FilaMaterial =
  | { tipo: 'linea'; item: ItemMaterial; indice: number }
  | { tipo: 'subtotal'; clave: string; material: string; cantidad: number; subtotal: number }

export function filasMateriales(items: ItemMaterial[], subtotales: boolean): FilaMaterial[] {
  if (!subtotales) {
    return items.map((item, indice) => ({ tipo: 'linea', item, indice }))
  }

  const grupos = new Map<string, ItemMaterial[]>()
  const orden: string[] = []
  for (const item of items) {
    const clave = normalizar(item.material) + '\u0000' + normalizar(item.unidad)
    const actual = grupos.get(clave)
    if (actual) {
      actual.push(item)
    } else {
      grupos.set(clave, [item])
      orden.push(clave)
    }
  }

  const filas: FilaMaterial[] = []
  let indice = 0
  for (const clave of orden) {
    const grupo = grupos.get(clave) ?? []
    for (const item of grupo) {
      filas.push({ tipo: 'linea', item, indice })
      indice += 1
    }

    if (grupo.length > 1) {
      const cantidad = grupo.reduce((suma, item) => suma + item.cantidad, 0)
      const subtotal = grupo.reduce((suma, item) => suma + item.subtotal, 0)
      filas.push({
        tipo: 'subtotal',
        clave,
        material: grupo[0].material,
        cantidad: Math.round(cantidad * 10000) / 10000,
        subtotal: Math.round(subtotal * 100) / 100,
      })
    }
  }

  return filas
}

function normalizar(texto: string) {
  return texto.trim().toLocaleLowerCase('es-CO')
}
