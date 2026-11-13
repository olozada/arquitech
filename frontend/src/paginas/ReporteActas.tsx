import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Row } from 'react-bootstrap'
import { cliente, mensajeDeError, mensajeError } from '../api/cliente'
import { abrirInformeItems, descargarExcel } from '../api/archivos'
import type { CampoOrdenInforme } from '../api/archivos'
import { hoyIso, inicioDeMesIso } from '../actas/formato'
import type { EstadoActa } from '../dashboard/filtroUrl'
import { avisar, fechasInvalidas, useCatalogo } from '../informes/comun'
import { SelectorInforme } from '../informes/SelectorInforme'
import { TAMANO_PAGINA, lineasEncabezado, ordenImpresion, parametros, type Direccion, type Filtro, type Informe, type VistaInforme } from '../informes/reporteActas'
import { TablaItems } from '../informes/TablaReporteActas'

export function ReporteActas() {
  const [usarFechas, setUsarFechas] = useState(false)
  const [desde, setDesde] = useState(inicioDeMesIso)
  const [hasta, setHasta] = useState(hoyIso)
  const [construccionId, setConstruccionId] = useState(0)
  const [contratistaId, setContratistaId] = useState(0)
  const [arquitectoId, setArquitectoId] = useState(0)
  const [revisorId, setRevisorId] = useState(0)
  const [estado, setEstado] = useState<EstadoActa>('')
  const [vista, setVista] = useState<VistaInforme>('detallado')
  const [romperPorActa, setRomperPorActa] = useState(false)
  const [campo, setCampo] = useState<CampoOrdenInforme>('subtotal')
  const [direccion, setDireccion] = useState<Direccion>('desc')
  const [pagina, setPagina] = useState(1)
  const [filtro, setFiltro] = useState<Filtro | null>(null)
  const [exportando, setExportando] = useState(false)
  const [imprimiendo, setImprimiendo] = useState(false)
  const informeDisponible = construccionId > 0 || contratistaId > 0 || arquitectoId > 0 || revisorId > 0

  const construcciones = useCatalogo('construcciones')
  const contratistas = useCatalogo('contratistas')
  const arquitectos = useCatalogo('arquitectos')
  const revisores = useCatalogo('revisores')
  const reporte = useQuery({
    queryKey: ['reporte-actas', filtro, pagina, campo, direccion],
    enabled: filtro != null,
    queryFn: async () => {
      const { data } = await cliente.get<Informe>('/api/actas/items', {
        params: {
          ...parametros(filtro!),
          vista: filtro!.vista,
          romperPorActa: filtro!.romperPorActa || undefined,
          orden: campo,
          direccion,
          pagina,
          tamano: TAMANO_PAGINA,
        },
      })
      return data
    },
  })

  function criterioActual() {
    return { usarFechas, desde, hasta, construccionId, contratistaId, arquitectoId, revisorId, estado, vista, romperPorActa }
  }

  async function consultar(evento: FormEvent) {
    evento.preventDefault()
    if (fechasInvalidas(usarFechas, desde, hasta)) {
      await avisar('La fecha final debe ser mayor o igual que la fecha inicial.')
      return
    }

    setPagina(1)
    setFiltro(criterioActual())
  }

  async function exportar() {
    if (fechasInvalidas(usarFechas, desde, hasta)) {
      await avisar('La fecha final debe ser mayor o igual que la fecha inicial.')
      return
    }

    setExportando(true)
    try {
      await descargarExcel(criterioActual())
    } catch (error) {
      await avisar(await mensajeDeError(error))
    } finally {
      setExportando(false)
    }
  }

  async function imprimir() {
    if (fechasInvalidas(usarFechas, desde, hasta)) {
      await avisar('La fecha final debe ser mayor o igual que la fecha inicial.')
      return
    }

    setImprimiendo(true)
    try {
      await abrirInformeItems(criterioActual(), vista, ordenImpresion(vista, campo, direccion, romperPorActa, construccionId > 0))
    } catch (error) {
      await avisar(await mensajeDeError(error))
    } finally {
      setImprimiendo(false)
    }
  }

  return (
    <section>
      <h1>Reporte de actas</h1>
      <p><Link to="/informes/materiales">Ver por materiales</Link></p>
      <Form className="catalogo-panel reporte-filtros" onSubmit={(evento) => void consultar(evento)}>
        <Row className="g-3 align-items-end">
          <Col xs={12} sm="auto">
            <Form.Check
              id="reporte-usar-fechas"
              className="reporte-filtro-check"
              type="checkbox"
              label="Filtrar por fecha"
              checked={usarFechas}
              onChange={(evento) => {
                const activo = evento.target.checked
                setUsarFechas(activo)
                if (activo && fechasInvalidas(true, desde, hasta)) return
                setPagina(1)
                setFiltro((actual) => (actual ? { ...actual, usarFechas: activo, desde, hasta } : actual))
              }}
            />
          </Col>
          <Col xs={12} sm="auto">
            <Form.Check
              id="reporte-romper-acta"
              className="reporte-filtro-check"
              type="checkbox"
              label="Agrupar"
              checked={romperPorActa}
              onChange={(evento) => {
                const activo = evento.target.checked
                setRomperPorActa(activo)
                setPagina(1)
                setFiltro((actual) => (actual ? { ...actual, romperPorActa: activo } : actual))
              }}
            />
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-fecha">
            <Form.Group controlId="reporte-desde">
              <Form.Label>Desde</Form.Label>
              <Form.Control
                type="date"
                value={desde}
                disabled={!usarFechas}
                onChange={(evento) => {
                  if (evento.currentTarget.disabled) return
                  setDesde(evento.currentTarget.value)
                }}
              />
            </Form.Group>
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-fecha">
            <Form.Group controlId="reporte-hasta">
              <Form.Label>Hasta</Form.Label>
              <Form.Control
                type="date"
                value={hasta}
                disabled={!usarFechas}
                onChange={(evento) => {
                  if (evento.currentTarget.disabled) return
                  setHasta(evento.currentTarget.value)
                }}
              />
            </Form.Group>
          </Col>
        </Row>
        <Row className="g-3 align-items-end mt-1">
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="reporte-construccion">
              <Form.Label>Construcción</Form.Label>
              <SelectorInforme valor={construccionId} opciones={construcciones.data ?? []} alCambiar={setConstruccionId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="reporte-arquitecto">
              <Form.Label>Arquitecto</Form.Label>
              <SelectorInforme valor={arquitectoId} opciones={arquitectos.data ?? []} alCambiar={setArquitectoId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="reporte-revisor">
              <Form.Label>Revisor</Form.Label>
              <SelectorInforme valor={revisorId} opciones={revisores.data ?? []} alCambiar={setRevisorId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="reporte-contratista">
              <Form.Label>Contratista</Form.Label>
              <SelectorInforme valor={contratistaId} opciones={contratistas.data ?? []} alCambiar={setContratistaId} />
            </Form.Group>
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-corto">
            <Form.Group controlId="reporte-vista">
              <Form.Label>Vista</Form.Label>
              <Form.Select
                value={vista}
                onChange={(evento) => {
                  const siguiente: VistaInforme = evento.target.value === 'agrupado' ? 'agrupado' : 'detallado'
                  setVista(siguiente)
                  setPagina(1)
                  setFiltro((actual) => (actual ? { ...actual, vista: siguiente } : actual))
                }}
              >
                <option value="detallado">Detallado</option>
                <option value="agrupado">Agrupado</option>
              </Form.Select>
            </Form.Group>
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-corto">
            <Form.Group controlId="reporte-estado">
              <Form.Label>Estado</Form.Label>
              <Form.Select
                value={estado}
                onChange={(evento) => setEstado(evento.target.value === 'abierta' || evento.target.value === 'cerrada' ? evento.target.value : '')}
              >
                <option value="">Todos</option>
                <option value="abierta">Abierta</option>
                <option value="cerrada">Cerrada</option>
              </Form.Select>
            </Form.Group>
          </Col>
        </Row>
        <div className="catalogo-acciones mt-3">
          <Button type="submit">Consultar</Button>
          <Button type="button" variant="outline-primary" disabled={exportando} onClick={() => void exportar()}>
            Exportar Excel
          </Button>
          {informeDisponible && (
            <Button type="button" variant="outline-primary" disabled={imprimiendo} onClick={() => void imprimir()}>
              {imprimiendo ? 'Imprimiendo…' : 'Imprimir'}
            </Button>
          )}
        </div>
      </Form>

      {reporte.isError && <p>{mensajeError(reporte.error)}</p>}
      {reporte.data && reporte.data.items.length === 0 && <p>No hay registros.</p>}
      {reporte.data && reporte.data.items.length > 0 && (
        <TablaItems
          informe={reporte.data}
          pagina={pagina}
          campo={campo}
          direccion={direccion}
          romper={filtro?.romperPorActa ?? false}
          ocultarConstruccion={(filtro?.construccionId ?? 0) > 0}
          encabezado={filtro ? lineasEncabezado(filtro, construcciones.data ?? [], arquitectos.data ?? [], revisores.data ?? [], contratistas.data ?? []) : []}
          alCambiarPagina={setPagina}
          alOrdenar={(siguiente) => {
            setPagina(1)
            if (campo === siguiente) {
              setDireccion((actual) => (actual === 'asc' ? 'desc' : 'asc'))
              return
            }

            setCampo(siguiente)
            setDireccion('asc')
          }}
        />
      )}
    </section>
  )
}
