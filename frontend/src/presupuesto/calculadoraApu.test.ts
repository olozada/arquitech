import { readFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { expect, test } from 'vitest'
import { calcularApu, calcularEnlazado, type ApuEnlazado, type LineaCalculo } from './calculadoraApu.ts'

function linea(
  modo: string,
  cantidad: number,
  factor: number,
  precio: number,
  subApuId: number | null = null,
  herramientaMenor = false,
): LineaCalculo {
  return { modo, cantidad, factor, precio, herramientaMenor, subApuId }
}

test('los cuatro modos de línea aplican la fórmula', () => {
  const total = calcularApu(
    [
      {
        tipo: 'Material',
        divisor: 1,
        lineas: [linea('Desperdicio', 2, 0.05, 100), linea('Rendimiento', 1, 0.5, 80), linea('Divisor', 60, 6, 12)],
      },
    ],
    1,
  ).total

  expect(total).toBe(2 * 100 * 1.05 + 1 * 80 * 0.5 + (60 * 12) / 6)
})

test('el divisor del grupo y el general dividen el costo', () => {
  const total = calcularApu(
    [{ tipo: 'Material', divisor: 2, lineas: [linea('Desperdicio', 1, 0, 120)] }],
    6,
  ).total

  expect(total).toBe(10)
})

test('la herramienta menor es un porcentaje de la mano de obra', () => {
  const resultado = calcularApu(
    [
      {
        tipo: 'ManoObra',
        divisor: 1,
        lineas: [linea('Desperdicio', 1, 0, 1000), linea('Rendimiento', 1, 0.1, 0, null, true)],
      },
    ],
    1,
  )

  expect(resultado.total).toBe(1100)
  expect(resultado.costos.ManoObra).toBe(1100)
})

test('un subanálisis de dos niveles usa el costo del interno', () => {
  const mapa = new Map<number, ApuEnlazado>([
    [2, { id: 2, divisorGeneral: 1, grupos: [{ tipo: 'Material', divisor: 1, lineas: [linea('Desperdicio', 1, 0, 50)] }] }],
    [
      1,
      {
        id: 1,
        divisorGeneral: 1,
        grupos: [{ tipo: 'Material', divisor: 1, lineas: [linea('Desperdicio', 2, 0, 0, 2)] }],
      },
    ],
  ])

  expect(calcularEnlazado(1, mapa).total).toBe(100)
})

test('un ciclo entre análisis se rechaza', () => {
  const mapa = new Map<number, ApuEnlazado>([
    [1, { id: 1, divisorGeneral: 1, grupos: [{ tipo: 'Material', divisor: 1, lineas: [linea('Desperdicio', 1, 0, 0, 2)] }] }],
    [2, { id: 2, divisorGeneral: 1, grupos: [{ tipo: 'Material', divisor: 1, lineas: [linea('Desperdicio', 1, 0, 0, 1)] }] }],
  ])

  expect(() => calcularEnlazado(1, mapa)).toThrow(/ciclo/i)
})

test('el apu concreto zapatas reproduce el excel', () => {
  const ruta = join(
    dirname(fileURLToPath(import.meta.url)),
    '../../../backend/tests/Arquitectos.Tests/Fixtures/presupuesto_casa_murillo.json',
  )
  const modelo = JSON.parse(readFileSync(ruta, 'utf8')) as {
    apus: {
      id: number
      descripcion: string
      divisor: number
      grupos: {
        tipo: string
        divisor: number
        lineas: {
          cantidad: number
          precio: number
          factor: number
          modo: string
          tipo?: string | null
          porcentajeMO?: number | null
          subApuId?: number | null
        }[]
      }[]
    }[]
  }
  const mapa = new Map<number, ApuEnlazado>(
    modelo.apus.map((apu) => [
      apu.id,
      {
        id: apu.id,
        divisorGeneral: apu.divisor === 0 ? 1 : apu.divisor,
        grupos: apu.grupos.map((grupo) => ({
          tipo: grupo.tipo,
          divisor: grupo.divisor === 0 ? 1 : grupo.divisor,
          lineas: grupo.lineas.map((item) =>
            linea(
              item.modo,
              item.cantidad,
              item.porcentajeMO ?? item.factor,
              item.precio,
              item.subApuId ?? null,
              item.tipo?.toLowerCase() === 'herramientamenor',
            ),
          ),
        })),
      },
    ]),
  )
  const zapatas = modelo.apus.find((apu) => apu.descripcion === 'Concreto Zapatas')

  expect(zapatas).toBeDefined()
  expect(Math.abs(calcularEnlazado(zapatas!.id, mapa).total - 803590.4927212801)).toBeLessThanOrEqual(1)
})
