import { cliente } from './cliente'

export async function abrirPdf(numero: number, ventana?: Window | null) {
  const { data } = await cliente.get<Blob>(`/api/actas/${numero}/pdf`, { responseType: 'blob' })
  const url = URL.createObjectURL(data)
  if (ventana && !ventana.closed) {
    ventana.location.href = url
  } else {
    window.open(url, '_blank', 'noopener')
  }

  window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export async function descargarExcel(desde: string, hasta: string, arquitectoId: number) {
  const { data } = await cliente.get<Blob>('/api/actas/export', {
    responseType: 'blob',
    params: {
      desde,
      hasta,
      arquitectoId: arquitectoId > 0 ? arquitectoId : undefined,
    },
  })
  const url = URL.createObjectURL(data)
  const enlace = document.createElement('a')
  enlace.href = url
  enlace.download = 'ReporteActas.xlsx'
  enlace.click()
  URL.revokeObjectURL(url)
}

export async function descargarDashboard(parametros: Record<string, string | number | undefined>) {
  const { data } = await cliente.get<Blob>('/api/dashboard/export', {
    responseType: 'blob',
    params: parametros,
  })
  const url = URL.createObjectURL(data)
  const enlace = document.createElement('a')
  enlace.href = url
  enlace.download = 'Dashboard.xlsx'
  enlace.click()
  URL.revokeObjectURL(url)
}
