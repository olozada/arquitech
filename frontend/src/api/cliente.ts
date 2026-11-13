import axios from 'axios'
import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import { borrarSesion } from './sesion'

type Peticion = InternalAxiosRequestConfig & { _reintento?: boolean }

export const cliente = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '',
  withCredentials: true,
})

let renovacion: Promise<void> | null = null

const metodosLectura = new Set(['get', 'head', 'options', 'trace'])

cliente.interceptors.request.use((config) => {
  const metodo = (config.method ?? 'get').toLowerCase()
  if (!metodosLectura.has(metodo)) {
    config.headers['X-Requested-With'] = 'arquitectos'
  }

  return config
})

cliente.interceptors.response.use(
  (respuesta) => respuesta,
  async (error: AxiosError) => {
    const config = error.config as Peticion | undefined
    const url = config?.url ?? ''
    const esLogin = url.includes('/api/auth/login')
    const esRenovar = url.includes('/api/auth/renovar')
    if (error.response?.status !== 401 || !config || esLogin || config._reintento) {
      if (error.response?.status === 401 && esRenovar) {
        borrarSesion()
        irALogin()
      }

      return Promise.reject(error)
    }

    config._reintento = true
    try {
      renovacion ??= cliente
        .post('/api/auth/renovar')
        .then(() => undefined)
        .finally(() => {
          renovacion = null
        })
      await renovacion
      return cliente(config)
    } catch (renovacionError) {
      borrarSesion()
      irALogin()
      return Promise.reject(renovacionError)
    }
  },
)

function irALogin() {
  const destino = `${import.meta.env.BASE_URL}login`.replace(/\/{2,}/g, '/')
  if (!window.location.pathname.endsWith('/login')) {
    window.location.assign(destino)
  }
}

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
