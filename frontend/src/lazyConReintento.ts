import { lazy, type ComponentType } from 'react'

type Modulo<T> = { default: T }

/** Si el navegador tiene un index.html viejo, el chunk puede fallar; recarga una vez. */
export function lazyConReintento<T extends ComponentType<unknown>>(
  importar: () => Promise<Modulo<T>>,
) {
  return lazy(async () => {
    try {
      return await importar()
    } catch (error) {
      const clave = 'arquitectos.recarga-chunk'
      if (!sessionStorage.getItem(clave)) {
        sessionStorage.setItem(clave, '1')
        window.location.reload()
      }

      throw error
    }
  })
}
