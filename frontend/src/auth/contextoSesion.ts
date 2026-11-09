import { createContext, useContext } from 'react'
import type { Sesion } from '../api/sesion'

export type ValorSesion = {
  sesion: Sesion | null
  iniciarSesion: (sesion: Sesion) => void
  cerrarSesion: () => void
}

export const ContextoSesion = createContext<ValorSesion | null>(null)

export function useSesion() {
  const valor = useContext(ContextoSesion)
  if (!valor) {
    throw new Error('useSesion debe usarse dentro de SesionProvider.')
  }

  return valor
}
