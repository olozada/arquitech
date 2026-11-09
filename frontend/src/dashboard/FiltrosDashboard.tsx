import { Button, Col, Form, Row } from 'react-bootstrap'
import { useQuery } from '@tanstack/react-query'
import { cliente } from '../api/cliente'
import type { RegistroCatalogo } from '../catalogos/definiciones'
import { atajo, type FiltroDashboard } from './filtroUrl'

type Propiedades = {
  filtro: FiltroDashboard
  alCambiar: (filtro: FiltroDashboard) => void
  exportando: boolean
  alExportar: () => void
}

export function FiltrosDashboard({ filtro, alCambiar, exportando, alExportar }: Propiedades) {
  const construcciones = useCatalogo('construcciones')
  const contratistas = useCatalogo('contratistas')
  const arquitectos = useCatalogo('arquitectos')

  function cambiar(parcial: Partial<FiltroDashboard>) {
    alCambiar({ ...filtro, ...parcial })
  }

  function aplicarAtajo(nombre: 'mes' | 'trimestre' | 'anio' | 'doce') {
    const fechas = atajo(nombre)
    alCambiar({ ...filtro, desde: fechas.desde, hasta: fechas.hasta })
  }

  return (
    <section className="catalogo-panel">
      <Row className="g-2 align-items-end">
        <Col md={6} lg={2}>
          <Form.Label>Desde</Form.Label>
          <Form.Control type="date" value={filtro.desde} onChange={(evento) => cambiar({ desde: evento.target.value })} />
        </Col>
        <Col md={6} lg={2}>
          <Form.Label>Hasta</Form.Label>
          <Form.Control type="date" value={filtro.hasta} onChange={(evento) => cambiar({ hasta: evento.target.value })} />
        </Col>
        <Col md={4} lg={2}>
          <Form.Label>Construcción</Form.Label>
          <Selector
            valor={filtro.construccionId}
            opciones={construcciones.data ?? []}
            alCambiar={(construccionId) => cambiar({ construccionId })}
          />
        </Col>
        <Col md={4} lg={2}>
          <Form.Label>Contratista</Form.Label>
          <Selector
            valor={filtro.contratistaId}
            opciones={contratistas.data ?? []}
            alCambiar={(contratistaId) => cambiar({ contratistaId })}
          />
        </Col>
        <Col md={4} lg={2}>
          <Form.Label>Arquitecto</Form.Label>
          <Selector
            valor={filtro.arquitectoId}
            opciones={arquitectos.data ?? []}
            alCambiar={(arquitectoId) => cambiar({ arquitectoId })}
          />
        </Col>
        <Col lg={2} className="d-grid">
          <Button type="button" onClick={alExportar} disabled={exportando}>
            {exportando ? 'Exportando…' : 'Exportar'}
          </Button>
        </Col>
      </Row>
      <div className="catalogo-acciones mt-3">
        <Button type="button" variant="outline-secondary" size="sm" onClick={() => aplicarAtajo('mes')}>
          Mes actual
        </Button>
        <Button type="button" variant="outline-secondary" size="sm" onClick={() => aplicarAtajo('trimestre')}>
          Trimestre
        </Button>
        <Button type="button" variant="outline-secondary" size="sm" onClick={() => aplicarAtajo('anio')}>
          Año
        </Button>
        <Button type="button" variant="outline-secondary" size="sm" onClick={() => aplicarAtajo('doce')}>
          12 meses
        </Button>
      </div>
    </section>
  )
}

function Selector({
  valor,
  opciones,
  alCambiar,
}: {
  valor: number
  opciones: RegistroCatalogo[]
  alCambiar: (id: number) => void
}) {
  return (
    <Form.Select value={valor} onChange={(evento) => alCambiar(Number(evento.target.value))}>
      <option value={0}>Todos</option>
      {opciones.map((opcion) => (
        <option key={opcion.id} value={opcion.id}>
          {String(opcion.nombre ?? opcion.descripcion ?? opcion.id)}
        </option>
      ))}
    </Form.Select>
  )
}

function useCatalogo(recurso: string) {
  return useQuery({
    queryKey: ['catalogo', recurso],
    queryFn: async () => (await cliente.get<RegistroCatalogo[]>(`/api/${recurso}`)).data,
  })
}
