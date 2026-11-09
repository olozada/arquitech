import { Link } from 'react-router-dom'
import { dinero } from '../actas/formato'
import type { Kpi } from '../api/dashboard'

type Propiedades = {
  kpi: Kpi
  mayorEsMejor: boolean
  periodoAnterior: string
}

export function TarjetaKpi({ kpi, mayorEsMejor, periodoAnterior }: Propiedades) {
  const variacion = kpi.variacionPct
  const clase =
    variacion === null || variacion === 0 ? 'neutro' : (variacion > 0) === mayorEsMejor ? 'bien' : 'mal'

  return (
    <article className="kpi-tarjeta">
      <p className="kpi-titulo">{kpi.titulo}</p>
      <p className="kpi-valor">{textoValor(kpi)}</p>
      <p className={`kpi-variacion ${clase}`}>
        {variacion === null ? 'Sin periodo anterior' : `${variacion > 0 ? '▲' : variacion < 0 ? '▼' : '•'} ${formatoPct(variacion)}`}
      </p>
      <p className="kpi-subtitulo">
        {textoComplemento(kpi)}
        Anterior ({periodoAnterior}): {textoAnterior(kpi)}
      </p>
      <Link to="/informes/actas">Ver detalle</Link>
    </article>
  )
}

function textoValor(kpi: Kpi) {
  if (kpi.codigo === 'K4') {
    return `${formatoEntero(kpi.valor)} / ${formatoEntero(kpi.complemento ?? 0)}`
  }

  return presentar(kpi.valor, kpi.formato)
}

function textoAnterior(kpi: Kpi) {
  if (kpi.valorAnterior === null) {
    return '—'
  }

  if (kpi.codigo === 'K4') {
    return formatoEntero(kpi.valorAnterior)
  }

  return presentar(kpi.valorAnterior, kpi.formato)
}

function textoComplemento(kpi: Kpi) {
  if (kpi.codigo === 'K7' && kpi.complemento !== null) {
    return `${formatoPct(kpi.complemento)} se registran tarde. `
  }

  if (kpi.codigo === 'K8') {
    return 'A1 a A4. '
  }

  return ''
}

function presentar(valor: number, formato: Kpi['formato']) {
  if (formato === 'moneda') {
    return dinero(valor)
  }

  if (formato === 'dias') {
    return `${new Intl.NumberFormat('es-CO', { maximumFractionDigits: 1 }).format(valor)} días`
  }

  return formatoEntero(valor)
}

function formatoEntero(valor: number) {
  return new Intl.NumberFormat('es-CO', { maximumFractionDigits: 0 }).format(valor)
}

function formatoPct(valor: number) {
  return `${new Intl.NumberFormat('es-CO', { maximumFractionDigits: 1 }).format(Math.abs(valor))} %`
}
