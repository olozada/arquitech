import { expect, test } from 'vitest'
import { aIso, dinero, fecha, fechaConsulta, hoyIso, inicioDeMesIso, numero, redondear } from './formato.ts'

test('dinero formatea pesos colombianos', () => {
  expect(dinero(1500)).toMatch(/1\.500/)
  expect(dinero(1500, 2)).toMatch(/1\.500,00/)
})

test('fecha invierte una fecha iso y deja intacto lo demás', () => {
  expect(fecha('2026-11-12')).toBe('12/11/2026')
  expect(fecha('sin-fecha')).toBe('sin-fecha')
})

test('numero acepta coma decimal y rechaza texto', () => {
  expect(numero(' 12 ')).toBe(12)
  expect(numero('1,5')).toBe(1.5)
  expect(numero('abc')).toBeNull()
})

test('redondear corta a los decimales pedidos', () => {
  expect(redondear(1.234, 2)).toBe(1.23)
  expect(redondear(1.235, 2)).toBe(1.24)
})

test('fechaConsulta solo envía una fecha iso marcada', () => {
  expect(fechaConsulta(false, '2026-01-01')).toBeUndefined()
  expect(fechaConsulta(true, '2026-01-01')).toBe('2026-01-01')
  expect(fechaConsulta(true, 'ayer')).toBeUndefined()
})

test('las fechas de hoy y del mes usan el calendario local', () => {
  const ahora = new Date()
  expect(hoyIso()).toBe(aIso(ahora))
  expect(inicioDeMesIso()).toBe(aIso(new Date(ahora.getFullYear(), ahora.getMonth(), 1)))
})
