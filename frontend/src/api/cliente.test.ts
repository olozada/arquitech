import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios'
import { afterEach, expect, test } from 'vitest'
import { cliente } from './cliente'

const adaptador = cliente.defaults.adapter

afterEach(() => {
  cliente.defaults.adapter = adaptador
})

test('el encabezado anti-csrf va en post, put, patch y delete', async () => {
  const vistos = new Map<string, string | undefined>()
  cliente.defaults.adapter = async (config) => {
    const metodo = (config.method ?? 'get').toLowerCase()
    const valor = config.headers.get('X-Requested-With')
    vistos.set(metodo, typeof valor === 'string' ? valor : undefined)
    return respuestaVacia(config)
  }

  await cliente.post('/api/x')
  await cliente.put('/api/x')
  await cliente.patch('/api/x')
  await cliente.delete('/api/x')
  await cliente.get('/api/x')
  await cliente.head('/api/x')

  expect(vistos.get('post')).toBe('arquitectos')
  expect(vistos.get('put')).toBe('arquitectos')
  expect(vistos.get('patch')).toBe('arquitectos')
  expect(vistos.get('delete')).toBe('arquitectos')
  expect(vistos.get('get')).toBeUndefined()
  expect(vistos.get('head')).toBeUndefined()
})

function respuestaVacia(config: InternalAxiosRequestConfig): AxiosResponse {
  return { data: {}, status: 200, statusText: 'OK', headers: {}, config }
}
