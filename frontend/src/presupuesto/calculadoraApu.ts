export type LineaCalculo = {
  modo: string
  cantidad: number
  factor: number
  precio: number
  herramientaMenor: boolean
  subApuId?: number | null
}

export type GrupoCalculo = {
  tipo: string
  divisor: number
  lineas: LineaCalculo[]
}

export type ResultadoGrupo = {
  subtotal: number
  lineas: number[]
}

export function precioUnitarioInsumo(precioBase: number, porcentajeIva: number, cantidadPorUnidad: number) {
  return cantidadPorUnidad === 0 ? 0 : (precioBase * (1 + porcentajeIva)) / cantidadPorUnidad
}

export function calcularGrupo(grupo: GrupoCalculo): ResultadoGrupo {
  let sumaManoObra = 0
  let total = 0
  const valores: number[] = []
  const esManoObra = grupo.tipo.toLowerCase() === 'manoobra'
  for (const linea of grupo.lineas) {
    const valor = valorLinea(linea, linea.precio, sumaManoObra)

    if (esManoObra && !linea.herramientaMenor) {
      sumaManoObra += valor
    }

    total += valor
    valores.push(valor)
  }

  const divisor = grupo.divisor === 0 ? 1 : grupo.divisor
  return { subtotal: total / divisor, lineas: valores }
}

export function calcularApu(grupos: GrupoCalculo[], divisorGeneral: number) {
  const costos: Record<string, number> = {}
  const detalle = grupos.map((grupo) => {
    const resultado = calcularGrupo(grupo)
    costos[grupo.tipo] = (costos[grupo.tipo] ?? 0) + resultado.subtotal
    return resultado
  })
  const divisor = divisorGeneral === 0 ? 1 : divisorGeneral
  const ajustados = Object.fromEntries(Object.entries(costos).map(([tipo, valor]) => [tipo, valor / divisor]))
  const total = Object.values(ajustados).reduce((suma, valor) => suma + valor, 0)
  return { total, costos: ajustados, detalle }
}

export type ApuEnlazado = {
  id: number
  divisorGeneral: number
  grupos: GrupoCalculo[]
}

export function calcularEnlazado(apuId: number, apus: ReadonlyMap<number, ApuEnlazado>) {
  const cache = new Map<number, { total: number; costos: Record<string, number> }>()
  return visitar(apuId, apus, new Set<number>(), cache)
}

function visitar(
  apuId: number,
  apus: ReadonlyMap<number, ApuEnlazado>,
  pila: Set<number>,
  cache: Map<number, { total: number; costos: Record<string, number> }>,
): { total: number; costos: Record<string, number> } {
  const guardado = cache.get(apuId)
  if (guardado) {
    return guardado
  }

  if (pila.has(apuId)) {
    throw new Error(`Hay un ciclo en el APU ${apuId}.`)
  }

  const apu = apus.get(apuId)
  if (!apu) {
    throw new Error(`No se encontró el APU ${apuId}.`)
  }

  pila.add(apuId)
  const costos = new Map<string, { tipo: string; subtotal: number }>()
  for (const grupo of apu.grupos) {
    let sumaManoObra = 0
    let total = 0
    const esManoObra = grupo.tipo.toLowerCase() === 'manoobra'
    for (const linea of grupo.lineas) {
      const precio = linea.subApuId == null ? linea.precio : visitar(linea.subApuId, apus, pila, cache).total
      const valor = valorLinea(linea, precio, sumaManoObra)
      if (esManoObra && !linea.herramientaMenor) {
        sumaManoObra += valor
      }

      total += valor
    }

    const divisor = grupo.divisor === 0 ? 1 : grupo.divisor
    const clave = grupo.tipo.toLowerCase()
    const actual = costos.get(clave)
    if (actual) {
      actual.subtotal += total / divisor
    } else {
      costos.set(clave, { tipo: grupo.tipo, subtotal: total / divisor })
    }
  }

  const divisorGeneral = apu.divisorGeneral === 0 ? 1 : apu.divisorGeneral
  const ajustados = Object.fromEntries(
    [...costos.values()].map((grupo) => [grupo.tipo, grupo.subtotal / divisorGeneral]),
  )
  const resultado = {
    total: Object.values(ajustados).reduce((suma, valor) => suma + valor, 0),
    costos: ajustados,
  }
  cache.set(apuId, resultado)
  pila.delete(apuId)
  return resultado
}

function valorLinea(linea: LineaCalculo, precio: number, sumaManoObra: number) {
  if (linea.herramientaMenor) {
    return linea.factor * sumaManoObra
  }

  const modo = linea.modo.toLowerCase()
  if (modo === 'divisor') {
    return linea.factor === 0 ? 0 : (linea.cantidad * precio) / linea.factor
  }

  if (modo === 'rendimiento') {
    return linea.cantidad * precio * linea.factor
  }

  return linea.cantidad * precio * (1 + linea.factor)
}
