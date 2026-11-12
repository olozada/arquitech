import { aIso } from '../actas/formato'

export type EstadoActa = '' | 'abierta' | 'cerrada'

export type FiltroDashboard = {
  desde: string
  hasta: string
  construccionId: number
  contratistaId: number
  arquitectoId: number
  revisorId: number
  estado: EstadoActa
}

export function filtroPorDefecto(hoy = new Date()): FiltroDashboard {
  return {
    desde: aIso(new Date(hoy.getFullYear(), hoy.getMonth() - 12, 1)),
    hasta: aIso(hoy),
    construccionId: 0,
    contratistaId: 0,
    arquitectoId: 0,
    revisorId: 0,
    estado: '',
  }
}

export function atajo(nombre: 'mes' | 'trimestre' | 'anio' | 'doce', hoy = new Date()): FiltroDashboard {
  if (nombre === 'mes') {
    return { ...filtroPorDefecto(hoy), desde: aIso(new Date(hoy.getFullYear(), hoy.getMonth(), 1)) }
  }

  if (nombre === 'trimestre') {
    const mes = Math.floor(hoy.getMonth() / 3) * 3
    return { ...filtroPorDefecto(hoy), desde: aIso(new Date(hoy.getFullYear(), mes, 1)) }
  }

  if (nombre === 'anio') {
    return { ...filtroPorDefecto(hoy), desde: aIso(new Date(hoy.getFullYear(), 0, 1)) }
  }

  return filtroPorDefecto(hoy)
}

export function leerFiltro(params: URLSearchParams): FiltroDashboard {
  const base = filtroPorDefecto()
  return {
    desde: params.get('desde') || base.desde,
    hasta: params.get('hasta') || base.hasta,
    construccionId: entero(params.get('construccionId')),
    contratistaId: entero(params.get('contratistaId')),
    arquitectoId: entero(params.get('arquitectoId')),
    revisorId: entero(params.get('revisorId')),
    estado: estadoDe(params.get('estado')),
  }
}

export function escribirFiltro(filtro: FiltroDashboard) {
  const params = new URLSearchParams()
  params.set('desde', filtro.desde)
  params.set('hasta', filtro.hasta)
  if (filtro.construccionId > 0) {
    params.set('construccionId', String(filtro.construccionId))
  }

  if (filtro.contratistaId > 0) {
    params.set('contratistaId', String(filtro.contratistaId))
  }

  if (filtro.arquitectoId > 0) {
    params.set('arquitectoId', String(filtro.arquitectoId))
  }

  if (filtro.revisorId > 0) {
    params.set('revisorId', String(filtro.revisorId))
  }

  if (filtro.estado) {
    params.set('estado', filtro.estado)
  }

  return params
}

export function periodoAnteriorTexto(desde: string, hasta: string) {
  const [anioDesde, mesDesde, diaDesde] = desde.split('-')
  const [anioHasta, mesHasta, diaHasta] = hasta.split('-')
  return `${diaDesde}/${mesDesde}/${anioDesde} – ${diaHasta}/${mesHasta}/${anioHasta}`
}

function entero(valor: string | null) {
  const numero = Number(valor)
  return Number.isInteger(numero) && numero > 0 ? numero : 0
}

function estadoDe(valor: string | null): EstadoActa {
  return valor === 'abierta' || valor === 'cerrada' ? valor : ''
}
