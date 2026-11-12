import { cliente } from './cliente'

export type Pagina<T> = {
  items: T[]
  total: number
  pagina: number
  tamano: number
}

export const tamanoListado = 20

export async function listarTodo<T>(ruta: string, params?: Record<string, unknown>) {
  const tamano = 200
  const items: T[] = []
  let pagina = 1
  let total = 0
  do {
    const { data } = await cliente.get<Pagina<T>>(ruta, { params: { ...params, pagina, tamano } })
    items.push(...data.items)
    total = data.total
    pagina += 1
    if (data.items.length === 0) {
      break
    }
  } while (items.length < total)
  return items
}
