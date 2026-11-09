import { cliente } from './cliente'

export type OpcionMenu = {
  id: number
  padreId: number | null
  descripcion: string
  url: string | null
  icono: string | null
  orden: number
  hijos: OpcionMenu[]
}

export async function obtenerMenu(): Promise<OpcionMenu[]> {
  const { data } = await cliente.get<OpcionMenu[]>('/api/menu')
  return data.map(ordenar)
}

export function buscarOpcion(opciones: OpcionMenu[], url: string): OpcionMenu | undefined {
  for (const opcion of opciones) {
    if (opcion.url === url) {
      return opcion
    }

    const hijo = buscarOpcion(opcion.hijos ?? [], url)
    if (hijo) {
      return hijo
    }
  }

  return undefined
}

function ordenar(opcion: OpcionMenu): OpcionMenu {
  const hijos = [...(opcion.hijos ?? [])].map(ordenar).sort(comparar)
  return { ...opcion, hijos }
}

function comparar(a: OpcionMenu, b: OpcionMenu) {
  return a.orden - b.orden || a.descripcion.localeCompare(b.descripcion, 'es')
}
