import { fechaConsulta } from '../actas/formato'
import { cliente } from './cliente'

export async function abrirPdf(numero: number, ventana?: Window | null) {
  const { data } = await cliente.get<Blob>(`/api/actas/${numero}/pdf`, { responseType: 'blob' })
  const buffer = await data.arrayBuffer()
  const destino = ventana && !ventana.closed ? ventana : null
  if (!destino) {
    const url = URL.createObjectURL(new Blob([buffer], { type: 'application/pdf' }))
    window.open(url, '_blank', 'noopener')
    window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
    return
  }

  const ambito = destino as Window & { Blob: typeof Blob; URL: typeof URL }
  const archivo = new ambito.Blob([buffer], { type: 'application/pdf' })
  const url = ambito.URL.createObjectURL(archivo)
  destino.document.open()
  destino.document.write(
    `<!DOCTYPE html><html><head><meta charset="utf-8"><title>Acta ${numero}</title></head>` +
      `<body style="margin:0"><embed type="application/pdf" src="${url}" style="position:fixed;inset:0;width:100%;height:100%"></body></html>`,
  )
  destino.document.close()
  destino.setTimeout(() => ambito.URL.revokeObjectURL(url), 60_000)
}

export type CampoOrdenInforme = 'acta' | 'construccion' | 'concepto' | 'material' | 'unidad' | 'cantidad' | 'precio' | 'estado' | 'subtotal'

type FiltroReporte = {
  usarFechas: boolean
  desde: string
  hasta: string
  construccionId: number
  contratistaId: number
  arquitectoId: number
  revisorId: number
  estado: '' | 'abierta' | 'cerrada'
  romperPorActa?: boolean
}

export async function abrirInformeItems(
  filtro: FiltroReporte,
  vista: 'detallado' | 'agrupado',
  orden?: { campo: CampoOrdenInforme; direccion: 'asc' | 'desc' },
) {
  const { data } = await cliente.get<Blob>('/api/actas/informe', {
    responseType: 'blob',
    params: {
      desde: fechaConsulta(filtro.usarFechas, filtro.desde),
      hasta: fechaConsulta(filtro.usarFechas, filtro.hasta),
      construccionId: filtro.construccionId > 0 ? filtro.construccionId : undefined,
      contratistaId: filtro.contratistaId > 0 ? filtro.contratistaId : undefined,
      arquitectoId: filtro.arquitectoId > 0 ? filtro.arquitectoId : undefined,
      revisorId: filtro.revisorId > 0 ? filtro.revisorId : undefined,
      estado: filtro.estado || undefined,
      vista,
      orden: orden?.campo,
      direccion: orden?.direccion,
      romperPorActa: filtro.romperPorActa || undefined,
    },
  })
  const url = URL.createObjectURL(new Blob([data], { type: 'application/pdf' }))
  window.open(url, '_blank', 'noopener')
  window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export async function descargarExcel(filtro: FiltroReporte) {
  const { data } = await cliente.get<Blob>('/api/actas/export', {
    responseType: 'blob',
    params: {
      desde: fechaConsulta(filtro.usarFechas, filtro.desde),
      hasta: fechaConsulta(filtro.usarFechas, filtro.hasta),
      construccionId: filtro.construccionId > 0 ? filtro.construccionId : undefined,
      contratistaId: filtro.contratistaId > 0 ? filtro.contratistaId : undefined,
      arquitectoId: filtro.arquitectoId > 0 ? filtro.arquitectoId : undefined,
      revisorId: filtro.revisorId > 0 ? filtro.revisorId : undefined,
      estado: filtro.estado || undefined,
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
