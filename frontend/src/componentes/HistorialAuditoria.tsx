import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Row } from 'react-bootstrap'
import { cliente } from '../api/cliente'

export type RegistroAuditoria = {
  id: number
  fecha: string
  usuarioId: number | null
  usuario: string | null
  entidad: string
  entidadId: string
  accion: string
  cambiosJson: string | null
  ip: string | null
}

type PaginaAuditoria = {
  registros: RegistroAuditoria[]
  total: number
  pagina: number
  tamano: number
}

type Filtro = {
  entidad: string
  entidadId: string
  desde: string
  hasta: string
  usuarioId: string
}

export function HistorialAuditoria({
  entidad,
  entidadId,
  conFiltros = false,
}: {
  entidad?: string
  entidadId?: string
  conFiltros?: boolean
}) {
  const [pagina, setPagina] = useState(1)
  const [filtro, setFiltro] = useState<Filtro>({
    entidad: entidad ?? '',
    entidadId: entidadId ?? '',
    desde: '',
    hasta: '',
    usuarioId: '',
  })
  const [aplicado, setAplicado] = useState(filtro)
  const consulta = useQuery({
    queryKey: ['auditoria', aplicado, pagina],
    queryFn: async () =>
      (await cliente.get<PaginaAuditoria>('/api/auditoria', {
        params: {
          entidad: aplicado.entidad || undefined,
          entidadId: aplicado.entidadId || undefined,
          desde: aplicado.desde || undefined,
          hasta: aplicado.hasta || undefined,
          usuarioId: aplicado.usuarioId || undefined,
          pagina,
        },
      })).data,
  })

  const totalPaginas = Math.max(1, Math.ceil((consulta.data?.total ?? 0) / (consulta.data?.tamano || 1)))

  return (
    <section>
      {conFiltros && (
        <Form
          className="catalogo-panel"
          onSubmit={(evento) => {
            evento.preventDefault()
            setPagina(1)
            setAplicado(filtro)
          }}
        >
          <Row className="g-3">
            <Col md={3}>
              <Form.Label htmlFor="aud-entidad">Entidad</Form.Label>
              <Form.Control id="aud-entidad" value={filtro.entidad} onChange={(evento) => setFiltro({ ...filtro, entidad: evento.target.value })} />
            </Col>
            <Col md={2}>
              <Form.Label htmlFor="aud-id">Id</Form.Label>
              <Form.Control id="aud-id" value={filtro.entidadId} onChange={(evento) => setFiltro({ ...filtro, entidadId: evento.target.value })} />
            </Col>
            <Col md={2}>
              <Form.Label htmlFor="aud-desde">Desde</Form.Label>
              <Form.Control id="aud-desde" type="date" value={filtro.desde} onChange={(evento) => setFiltro({ ...filtro, desde: evento.target.value })} />
            </Col>
            <Col md={2}>
              <Form.Label htmlFor="aud-hasta">Hasta</Form.Label>
              <Form.Control id="aud-hasta" type="date" value={filtro.hasta} onChange={(evento) => setFiltro({ ...filtro, hasta: evento.target.value })} />
            </Col>
            <Col md={2}>
              <Form.Label htmlFor="aud-usuario">Usuario</Form.Label>
              <Form.Control id="aud-usuario" inputMode="numeric" value={filtro.usuarioId} onChange={(evento) => setFiltro({ ...filtro, usuarioId: evento.target.value })} />
            </Col>
            <Col md={1} className="d-flex align-items-end">
              <Button type="submit">Ver</Button>
            </Col>
          </Row>
        </Form>
      )}
      {consulta.isPending && <p>Cargando historial…</p>}
      {consulta.isError && <p>No se pudo cargar el historial.</p>}
      {consulta.data && consulta.data.registros.length === 0 && <p>No hay movimientos.</p>}
      {consulta.data && consulta.data.registros.length > 0 && (
        <ol className="historial-linea">
          {consulta.data.registros.map((registro) => (
            <li key={registro.id}>
              <div className="d-flex justify-content-between gap-3 flex-wrap">
                <strong>{registro.accion}</strong>
                <time dateTime={registro.fecha}>{new Date(registro.fecha).toLocaleString('es-CO')}</time>
              </div>
              <div>
                {registro.entidad} {registro.entidadId}
                {' · '}
                {registro.usuario ?? 'Sin usuario'}
              </div>
              <Cambios json={registro.cambiosJson} />
            </li>
          ))}
        </ol>
      )}
      {consulta.data && consulta.data.total > consulta.data.tamano && (
        <div className="d-flex align-items-center gap-2 mt-3">
          <Button type="button" size="sm" variant="outline-secondary" disabled={pagina <= 1} onClick={() => setPagina((actual) => actual - 1)}>
            Anterior
          </Button>
          <span>Página {consulta.data.pagina} de {totalPaginas}</span>
          <Button
            type="button"
            size="sm"
            variant="outline-secondary"
            disabled={pagina * consulta.data.tamano >= consulta.data.total}
            onClick={() => setPagina((actual) => actual + 1)}
          >
            Siguiente
          </Button>
        </div>
      )}
    </section>
  )
}

function Cambios({ json }: { json: string | null }) {
  const lineas = leerCambios(json)
  if (lineas.length === 0) {
    return null
  }

  return (
    <ul className="mb-0">
      {lineas.map((linea) => (
        <li key={linea}>{linea}</li>
      ))}
    </ul>
  )
}

function leerCambios(json: string | null) {
  if (!json) {
    return []
  }

  try {
    const datos = JSON.parse(json) as Record<string, [unknown, unknown]>
    return Object.entries(datos).map(([campo, par]) => `${campo}: ${texto(par?.[0])} → ${texto(par?.[1])}`)
  } catch {
    return [json]
  }
}

function texto(valor: unknown) {
  if (valor == null || valor === '') {
    return '—'
  }

  return String(valor)
}
