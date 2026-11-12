export function dinero(valor: number, decimales = 0) {
  return new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP',
    minimumFractionDigits: decimales,
    maximumFractionDigits: decimales,
  }).format(valor)
}

export function fecha(valor: string) {
  const [anio, mes, dia] = valor.split('-')
  if (!anio || !mes || !dia) {
    return valor
  }

  return `${dia}/${mes}/${anio}`
}

export function hoyIso() {
  return aIso(new Date())
}

export function inicioDeMesIso() {
  const fechaActual = new Date()
  return aIso(new Date(fechaActual.getFullYear(), fechaActual.getMonth(), 1))
}

export function fechaConsulta(usar: boolean, valor: string) {
  return usar && /^\d{4}-\d{2}-\d{2}$/.test(valor) ? valor : undefined
}

export function numero(texto: string) {
  const limpio = texto.trim().replace(/\s/g, '').replace(',', '.')
  if (!/^\d+(\.\d+)?$/.test(limpio)) {
    return null
  }

  return Number(limpio)
}

export function redondear(valor: number, decimales: number) {
  const factor = 10 ** decimales
  return Math.round(valor * factor) / factor
}

export function aIso(fechaActual: Date) {
  const mes = String(fechaActual.getMonth() + 1).padStart(2, '0')
  const dia = String(fechaActual.getDate()).padStart(2, '0')
  return `${fechaActual.getFullYear()}-${mes}-${dia}`
}
