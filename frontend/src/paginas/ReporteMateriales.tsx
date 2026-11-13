import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Row } from 'react-bootstrap'
import { cliente, mensajeDeError, mensajeError } from '../api/cliente'
import { hoyIso, inicioDeMesIso } from '../actas/formato'
import type { EstadoActa } from '../dashboard/filtroUrl'
import { avisar, fechasInvalidas, useCatalogo } from '../informes/comun'
import { SelectorInforme } from '../informes/SelectorInforme'
import { TAMANO_PAGINA, lineasEncabezado, parametros, type Campo, type Direccion, type Filtro, type InformeMateriales, type VistaInforme } from '../informes/reporteMateriales'
import { TablaMateriales } from '../informes/TablaReporteMateriales'

export function ReporteMateriales() {
  const [usarFechas, setUsarFechas] = useState(false)
  const [desde, setDesde] = useState(inicioDeMesIso)
  const [hasta, setHasta] = useState(hoyIso)
  const [construccionId, setConstruccionId] = useState(0)
  const [contratistaId, setContratistaId] = useState(0)
  const [arquitectoId, setArquitectoId] = useState(0)
  const [revisorId, setRevisorId] = useState(0)
  const [proveedorId, setProveedorId] = useState(0)
  const [estado, setEstado] = useState<EstadoActa>('')
  const [vista, setVista] = useState<VistaInforme>('agrupado')
  const [campo, setCampo] = useState<Campo>('subtotal')
  const [direccion, setDireccion] = useState<Direccion>('desc')
  const [pagina, setPagina] = useState(1)
  const [filtro, setFiltro] = useState<Filtro | null>(null)
  const [exportando, setExportando] = useState(false)
  const [imprimiendo, setImprimiendo] = useState(false)
  const informeDisponible = construccionId > 0 || contratistaId > 0 || arquitectoId > 0 || revisorId > 0 || proveedorId > 0

  const construcciones = useCatalogo('construcciones')
  const contratistas = useCatalogo('contratistas')
  const arquitectos = useCatalogo('arquitectos')
  const revisores = useCatalogo('revisores')
  const proveedores = useCatalogo('proveedores')
  const reporte = useQuery({
    queryKey: ['reporte-materiales', filtro, pagina, campo, direccion],
    enabled: filtro != null,
    queryFn: async () => {
      const { data } = await cliente.get<InformeMateriales>('/api/actas/materiales', {
        params: { ...parametros(filtro!), orden: campo, direccion, pagina, tamano: TAMANO_PAGINA },
      })
      return data
    },
  })

  function criterioActual(): Filtro {
    return { usarFechas, desde, hasta, construccionId, contratistaId, arquitectoId, revisorId, proveedorId, estado, vista }
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
      const { data } = await cliente.get<Blob>('/api/actas/materiales/excel', {
        responseType: 'blob',
        params: parametros(criterioActual()),
      })
      const url = URL.createObjectURL(data)
      const enlace = document.createElement('a')
      enlace.href = url
      enlace.download = 'ReporteMateriales.xlsx'
      enlace.click()
      URL.revokeObjectURL(url)
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
      const { data } = await cliente.get<Blob>('/api/actas/materiales/informe', {
        responseType: 'blob',
        params: { ...parametros(criterioActual()), orden: campo, direccion },
      })
      const url = URL.createObjectURL(new Blob([data], { type: 'application/pdf' }))
      window.open(url, '_blank', 'noopener')
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
    } catch (error) {
      await avisar(await mensajeDeError(error))
    } finally {
      setImprimiendo(false)
    }
  }

  const encabezado = filtro
    ? lineasEncabezado(filtro, construcciones.data ?? [], arquitectos.data ?? [], revisores.data ?? [], contratistas.data ?? [], proveedores.data ?? [])
    : []

  return (
    <section>
      <h1>Reporte por materiales</h1>
      <p><Link to="/informes/actas">Ver por actas</Link></p>
      <Form className="catalogo-panel reporte-filtros" onSubmit={(evento) => void consultar(evento)}>
        <Row className="g-3 align-items-end">
          <Col xs={12} sm="auto">
            <Form.Check
              id="materiales-usar-fechas"
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
          <Col xs={6} sm="auto" className="reporte-campo-fecha">
            <Form.Group controlId="materiales-desde">
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
            <Form.Group controlId="materiales-hasta">
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
            <Form.Group controlId="materiales-construccion">
              <Form.Label>Construcción</Form.Label>
              <SelectorInforme valor={construccionId} opciones={construcciones.data ?? []} alCambiar={setConstruccionId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="materiales-arquitecto">
              <Form.Label>Arquitecto</Form.Label>
              <SelectorInforme valor={arquitectoId} opciones={arquitectos.data ?? []} alCambiar={setArquitectoId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="materiales-revisor">
              <Form.Label>Revisor</Form.Label>
              <SelectorInforme valor={revisorId} opciones={revisores.data ?? []} alCambiar={setRevisorId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="materiales-contratista">
              <Form.Label>Contratista</Form.Label>
              <SelectorInforme valor={contratistaId} opciones={contratistas.data ?? []} alCambiar={setContratistaId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="materiales-proveedor">
              <Form.Label>Proveedor</Form.Label>
              <SelectorInforme valor={proveedorId} opciones={proveedores.data ?? []} alCambiar={setProveedorId} />
            </Form.Group>
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-corto">
            <Form.Group controlId="materiales-vista">
              <Form.Label>Vista</Form.Label>
              <Form.Select
                value={vista}
                onChange={(evento) => {
                  const siguiente: VistaInforme = evento.target.value === 'detallado' ? 'detallado' : 'agrupado'
                  setVista(siguiente)
                  setPagina(1)
                  if (siguiente === 'agrupado') {
                    setCampo((actual) => (actual === 'acta' ? 'subtotal' : actual))
                  }
                  setFiltro((actual) => (actual ? { ...actual, vista: siguiente } : actual))
                }}
              >
                <option value="detallado">Detallado</option>
                <option value="agrupado">Agrupado</option>
              </Form.Select>
            </Form.Group>
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-corto">
            <Form.Group controlId="materiales-estado">
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
        <TablaMateriales
          items={reporte.data.items}
          total={reporte.data.total}
          cantidad={reporte.data.cantidad}
          tamano={reporte.data.tamano}
          detallado={!reporte.data.agrupado}
          pagina={pagina}
          campo={campo}
          direccion={direccion}
          encabezado={encabezado}
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
