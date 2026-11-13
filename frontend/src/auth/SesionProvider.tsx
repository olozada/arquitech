import { useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { cliente, type Peticion } from '../api/cliente'
import { consultas } from '../api/consultas'
import { borrarSesion, guardarSesion, leerSesion } from '../api/sesion'
import type { Sesion } from '../api/sesion'
import { ContextoSesion } from './contextoSesion'
import type { ValorSesion } from './contextoSesion'

const MS_VALIDAR = 8_000

function limpiarSesionLocal(setSesion: (sesion: Sesion | null) => void) {
  borrarSesion()
  consultas.clear()
  setSesion(null)
}

export function SesionProvider({ children }: { children: ReactNode }) {
  const [sesion, setSesion] = useState<Sesion | null>(() => leerSesion())
  const [listo, setListo] = useState(() => leerSesion() === null)

  useEffect(() => {
    sessionStorage.removeItem('arquitectos.recarga-chunk')

    if (!leerSesion()) {
      setListo(true)
      return
    }

    let activo = true
    const vencimiento = window.setTimeout(() => {
      if (!activo) {
        return
      }

      limpiarSesionLocal(setSesion)
      setListo(true)
    }, MS_VALIDAR)

    cliente
      .get('/api/menu', { _sinRenovacion: true, timeout: MS_VALIDAR } as Peticion)
      .then(() => {
        if (activo) {
          setListo(true)
        }
      })
      .catch(() => {
        if (!activo) {
          return
        }

        limpiarSesionLocal(setSesion)
        setListo(true)
      })
      .finally(() => {
        window.clearTimeout(vencimiento)
      })

    return () => {
      activo = false
      window.clearTimeout(vencimiento)
    }
  }, [])

  const valor = useMemo<ValorSesion>(
    () => ({
      sesion,
      iniciarSesion: (nueva) => {
        guardarSesion(nueva)
        setSesion(nueva)
      },
      cerrarSesion: () => {
        void cliente.post('/api/auth/salir').finally(() => {
          limpiarSesionLocal(setSesion)
        })
      },
    }),
    [sesion],
  )

  if (!listo) {
    return <p className="cargando-pagina">Cargando…</p>
  }

  return <ContextoSesion.Provider value={valor}>{children}</ContextoSesion.Provider>
}
