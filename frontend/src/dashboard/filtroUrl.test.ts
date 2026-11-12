import { expect, test } from 'vitest'
import { atajo, escribirFiltro, filtroPorDefecto, leerFiltro, periodoAnteriorTexto } from './filtroUrl.ts'

const hoy = new Date(2026, 10, 12)

test('el filtro por defecto mira doce meses atrás', () => {
  expect(filtroPorDefecto(hoy)).toEqual({
    desde: '2025-11-01',
    hasta: '2026-11-12',
    construccionId: 0,
    contratistaId: 0,
    arquitectoId: 0,
    revisorId: 0,
    estado: '',
  })
})

test('los atajos mueven solo la fecha inicial', () => {
  expect(atajo('mes', hoy).desde).toBe('2026-11-01')
  expect(atajo('trimestre', hoy).desde).toBe('2026-10-01')
  expect(atajo('anio', hoy).desde).toBe('2026-01-01')
  expect(atajo('doce', hoy).desde).toBe('2025-11-01')
})

test('el filtro se escribe y se lee sin perder los ceros', () => {
  const filtro = {
    desde: '2026-01-01',
    hasta: '2026-06-30',
    construccionId: 4,
    contratistaId: 0,
    arquitectoId: 2,
    revisorId: 0,
    estado: 'abierta' as const,
  }

  expect(leerFiltro(escribirFiltro(filtro))).toEqual(filtro)
  expect(leerFiltro(new URLSearchParams('estado=otra&construccionId=-3')).estado).toBe('')
  expect(leerFiltro(new URLSearchParams('construccionId=1.5')).construccionId).toBe(0)
})

test('el periodo anterior se muestra día mes año', () => {
  expect(periodoAnteriorTexto('2026-01-02', '2026-03-04')).toBe('02/01/2026 – 04/03/2026')
})
