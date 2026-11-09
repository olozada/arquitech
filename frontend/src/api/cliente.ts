import axios from 'axios'
import type { AxiosError } from 'axios'
import { borrarSesion, leerSesion } from './sesion'

export const cliente = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '',
})

cliente.interceptors.request.use((config) => {
  const sesion = leerSesion()
  if (sesion) {
    config.headers.Authorization = `Bearer ${sesion.token}`
  }

  return config
})

cliente.interceptors.response.use(
  (respuesta) => respuesta,
  (error: AxiosError) => {
    const url = error.config?.url ?? ''
    if (error.response?.status === 401 && !url.includes('/api/auth/login')) {
      borrarSesion()
      if (window.location.pathname !== '/login') {
        window.location.assign('/login')
      }
    }

    return Promise.reject(error)
  },
)

type CuerpoProblema = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export function mensajeError(error: unknown): string {
  if (!axios.isAxiosError(error)) {
    return 'No se pudo completar la operación.'
  }

  return textoProblema(error.response?.data)
}

export async function mensajeDeError(error: unknown): Promise<string> {
  if (axios.isAxiosError(error) && error.response?.data instanceof Blob) {
    try {
      return textoProblema(JSON.parse(await error.response.data.text()) as CuerpoProblema)
    } catch {
      return 'No se pudo completar la operación.'
    }
  }

  return mensajeError(error)
}

function textoProblema(datos: CuerpoProblema | undefined) {
  const primero = datos?.errors ? Object.values(datos.errors).flat()[0] : undefined
  return primero || datos?.detail || datos?.title || 'No se pudo completar la operación.'
}
