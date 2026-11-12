import { useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { cliente } from '../api/cliente'
import { consultas } from '../api/consultas'
import { borrarSesion, guardarSesion, leerSesion } from '../api/sesion'
import type { Sesion } from '../api/sesion'
import { ContextoSesion } from './contextoSesion'
import type { ValorSesion } from './contextoSesion'

export function SesionProvider({ children }: { children: ReactNode }) {
  const [sesion, setSesion] = useState<Sesion | null>(() => leerSesion())
  const valor = useMemo<ValorSesion>(
    () => ({
      sesion,
      iniciarSesion: (nueva) => {
        guardarSesion(nueva)
        setSesion(nueva)
      },
      cerrarSesion: () => {
        void cliente.post('/api/auth/salir').finally(() => {
          borrarSesion()
          consultas.clear()
          setSesion(null)
        })
      },
    }),
    [sesion],
  )

  return <ContextoSesion.Provider value={valor}>{children}</ContextoSesion.Provider>
}
