import { expect, test } from 'vitest'
import { evaluar } from './CeldaNumero'

test('evaluar acepta coma y miles', () => {
  expect(evaluar('12,5')).toBe(12.5)
  expect(evaluar('1.234,56')).toBe(1234.56)
  expect(evaluar('0.125')).toBe(0.125)
  expect(evaluar('1.234.567')).toBe(1234567)
})

test('evaluar acepta aritmética y referencias', () => {
  expect(evaluar('=12*3,5')).toBe(42)
  expect(evaluar('=[1.01]*1,35', () => 100)).toBe(135)
  expect(evaluar('=[x]')).toBeNull()
  expect(evaluar('alert(1)')).toBeNull()
  expect(evaluar('=2/0')).toBeNull()
})