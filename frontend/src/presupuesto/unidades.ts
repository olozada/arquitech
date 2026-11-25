import { normalizar } from './texto'

/** Equivalente reducido de TextoPresupuesto.CanonicaUnidad (backend). */
export function canonicaUnidad(texto: string) {
  const valor = normalizar(texto).replace(/\s+/g, '').replace(/\./g, '').replace('²', '2').replace('³', '3')
  const mapa: Record<string, string> = {
    mts2: 'm2',
    mt2: 'm2',
    mts3: 'm3',
    mt3: 'm3',
    un: 'und',
    unid: 'und',
    unidad: 'und',
    'und.': 'und',
    global: 'glb',
    libra: 'lb',
    galon: 'gal',
    litro: 'l',
    lt: 'l',
    jornal: 'jorn',
    dia: 'dia',
    dias: 'dia',
    viajes: 'viaje',
    cun: 'cun',
    cunete: 'cunete',
    tub: '%tub',
  }
  if (mapa[valor]) return mapa[valor]
  if (valor.endsWith('dias')) return 'dia'
  return valor
}

export function resolverUnidad(texto: string, unidades: Array<{ id: number; nombre: string }>) {
  const limpio = texto.trim()
  if (!limpio) return null
  const clave = canonicaUnidad(limpio)
  const exacta = unidades.find((u) => canonicaUnidad(u.nombre) === clave || normalizar(u.nombre) === normalizar(clave))
  if (exacta) return exacta
  return unidades.find((u) => normalizar(u.nombre).includes(clave)) ?? null
}