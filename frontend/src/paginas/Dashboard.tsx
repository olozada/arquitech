import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { Col, Row } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { descargarDashboard } from '../api/archivos'
import { mensajeDeError, mensajeError } from '../api/cliente'
import { useDashboard } from '../api/dashboard'
import { CalidadDatos } from '../dashboard/CalidadDatos'
import { FiltrosDashboard } from '../dashboard/FiltrosDashboard'
import { GraficoConceptos } from '../dashboard/GraficoConceptos'
import { GraficoPareto } from '../dashboard/GraficoPareto'
import { GraficoPrecio } from '../dashboard/GraficoPrecio'
import { GraficoRanking } from '../dashboard/GraficoRanking'
import { GraficoTendencia } from '../dashboard/GraficoTendencia'
import { TablaAlertas } from '../dashboard/TablaAlertas'
import { TarjetaKpi } from '../dashboard/TarjetaKpi'
import { leerFiltro, periodoAnteriorTexto, escribirFiltro, type FiltroDashboard } from '../dashboard/filtroUrl'

const sentido: Record<string, boolean> = {
  K1: true,
  K2: true,
  K3: true,
  K4: true,
  K5: false,
  K6: false,
  K7: false,
  K8: false,
}

export function Dashboard() {
  const [params, setParams] = useSearchParams()
  const filtro = useMemo(() => leerFiltro(params), [params])
  const consulta = useDashboard(filtro)
  const [exportando, setExportando] = useState(false)
  const datos = consulta.data
  const actas = datos?.kpis.find((kpi) => kpi.codigo === 'K2')?.valor ?? 0

  useEffect(() => {
    const escrito = escribirFiltro(filtro)
    if (params.toString() !== escrito.toString()) {
      setParams(escrito, { replace: true })
    }
  }, [filtro, params, setParams])

  function actualizar(siguiente: FiltroDashboard) {
    setParams(escribirFiltro(siguiente), { replace: true })
  }

  async function exportar() {
    setExportando(true)
    try {
      await descargarDashboard({
        desde: filtro.desde,
        hasta: filtro.hasta,
        construccionId: filtro.construccionId > 0 ? filtro.construccionId : undefined,
        contratistaId: filtro.contratistaId > 0 ? filtro.contratistaId : undefined,
        arquitectoId: filtro.arquitectoId > 0 ? filtro.arquitectoId : undefined,
      })
    } catch (error) {
      await Swal.fire({ title: '¡Error!', text: await mensajeDeError(error), icon: 'error' })
    } finally {
      setExportando(false)
    }
  }

  return (
    <div className="dashboard">
      <h1>Tablero</h1>
      <FiltrosDashboard filtro={filtro} alCambiar={actualizar} exportando={exportando} alExportar={() => void exportar()} />
      {consulta.isLoading ? <Esqueleto /> : null}
      {consulta.isError ? <p className="dashboard-vacio">{mensajeError(consulta.error)}</p> : null}
      {datos && actas === 0 ? (
        <p className="dashboard-vacio">
          No hay actas en el rango. Ajusta los filtros o registra una en <Link to="/actas/nueva">Toma de actas</Link>.
        </p>
      ) : null}
      {datos && actas > 0 ? (
        <>
          <Row className="g-3 mb-3">
            {datos.kpis.slice(0, 4).map((kpi) => (
              <Col key={kpi.codigo} xs={6} lg={3}>
                <TarjetaKpi
                  kpi={kpi}
                  mayorEsMejor={sentido[kpi.codigo] ?? true}
                  periodoAnterior={periodoAnteriorTexto(datos.desdeAnterior, datos.hastaAnterior)}
                />
              </Col>
            ))}
          </Row>
          <Row className="g-3 mb-3">
            {datos.kpis.slice(4).map((kpi) => (
              <Col key={kpi.codigo} xs={6} lg={3}>
                <TarjetaKpi
                  kpi={kpi}
                  mayorEsMejor={sentido[kpi.codigo] ?? false}
                  periodoAnterior={periodoAnteriorTexto(datos.desdeAnterior, datos.hastaAnterior)}
                />
              </Col>
            ))}
          </Row>
          <Row className="g-3 mb-3">
            <Col xs={12}>
              <GraficoTendencia puntos={datos.tendencia} />
            </Col>
            <Col xs={12} lg={6}>
              <GraficoRanking
                titulo="Top construcciones por valor"
                filas={datos.topConstrucciones}
                alElegir={(construccionId) => actualizar({ ...filtro, construccionId })}
              />
            </Col>
            <Col xs={12} lg={6}>
              <GraficoPareto
                filas={datos.contratistas}
                alElegir={(contratistaId) => actualizar({ ...filtro, contratistaId })}
              />
            </Col>
            <Col xs={12} lg={6}>
              <GraficoConceptos filas={datos.conceptos} />
            </Col>
            <Col xs={12} lg={6}>
              <div className="d-grid gap-3">
                <GraficoRanking titulo="Carga por arquitecto" filas={datos.arquitectos} />
                <GraficoRanking titulo="Carga por revisor" filas={datos.revisores} />
              </div>
            </Col>
            <Col xs={12}>
              <TablaAlertas
                key={filtro.desde + filtro.hasta + filtro.construccionId + filtro.contratistaId + filtro.arquitectoId}
                filtro={filtro}
                alFiltrarConstruccion={(construccionId) => actualizar({ ...filtro, construccionId })}
              />
            </Col>
            <Col xs={12} lg={8}>
              <GraficoPrecio desde={filtro.desde} hasta={filtro.hasta} />
            </Col>
            <Col xs={12} lg={4}>
              <CalidadDatos calidad={datos.calidad} />
            </Col>
          </Row>
        </>
      ) : null}
    </div>
  )
}

function Esqueleto() {
  return (
    <Row className="g-3 mb-3">
      {Array.from({ length: 8 }, (_, indice) => (
        <Col key={indice} xs={6} lg={3}>
          <div className="kpi-tarjeta kpi-esqueleto" />
        </Col>
      ))}
    </Row>
  )
}
