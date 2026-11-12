import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import axios from 'axios'
import { useQueryClient } from '@tanstack/react-query'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { usePresupuesto } from '../api/presupuesto'
import type { PresupuestoDetalle } from '../api/presupuesto'
import { calcular, cuerpo } from './costosPresupuesto'

export function useEditorPresupuesto(presupuestoId: number, administrador: boolean) {
  const consulta = usePresupuesto(presupuestoId)
  const [borrador, setBorrador] = useState<PresupuestoDetalle | null>(null)
  const [servidor, setServidor] = useState(consulta.data)
  const [guardando, setGuardando] = useState(false)
  const [sucio, setSucio] = useState(false)
  const [avisoGuardado, setAvisoGuardado] = useState('')
  const [conflicto, setConflicto] = useState(false)
  const espera = useRef<number | undefined>(undefined)
  const consultas = useQueryClient()

  if (consulta.data !== servidor) {
    setServidor(consulta.data)
    if (consulta.data) {
      setBorrador(structuredClone(consulta.data))
    }
  }

  const resumen = useMemo(() => (borrador ? calcular(borrador) : null), [borrador])
  const editable = administrador && borrador?.estado === 'Borrador'

  const guardar = useCallback(async (silencioso = false) => {
    if (!borrador) {
      return
    }
    setGuardando(true)
    setAvisoGuardado('Guardando…')
    try {
      const { data } = await cliente.put<PresupuestoDetalle>(`/api/presupuestos/${borrador.id}`, cuerpo(borrador))
      setSucio(false)
      setConflicto(false)
      setBorrador(data)
      setAvisoGuardado('Guardado')
      await consultas.invalidateQueries({ queryKey: ['presupuesto', borrador.id] })
      if (!silencioso) {
        await Swal.fire({ icon: 'success', title: 'Presupuesto guardado', confirmButtonColor: '#1abb9c' })
      }
    } catch (error) {
      setAvisoGuardado('Error')
      if (axios.isAxiosError(error) && error.response?.status === 409) {
        setConflicto(true)
        return
      }
      if (!silencioso) {
        await Swal.fire({ icon: 'error', title: 'No se pudo guardar', text: mensajeError(error) })
      }
    } finally {
      setGuardando(false)
    }
  }, [borrador, consultas])

  useEffect(() => {
    if (!sucio || !editable) {
      return
    }
    window.clearTimeout(espera.current)
    espera.current = window.setTimeout(() => {
      void guardar(true)
    }, 600)
    return () => window.clearTimeout(espera.current)
  }, [borrador, sucio, editable, guardar])

  return {
    consulta,
    borrador,
    setBorrador,
    sucio,
    setSucio,
    guardando,
    setGuardando,
    avisoGuardado,
    conflicto,
    resumen,
    guardar,
  }
}
