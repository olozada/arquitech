import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button, Form, Modal, Nav } from 'react-bootstrap'
import { cliente } from '../api/cliente'
import { dinero as formatoDinero } from '../actas/formato'

function dinero(valor: number) {
  return formatoDinero(valor, 2)
}

export type OrigenLinea = {
  insumoId?: number | null
  cuadrillaId?: number | null
  subApuId?: number | null
  descripcion: string
  unidad: string
  precio: number
  precioManual: number | null
}

const tiposPorGrupo: Record<string, string[]> = {
  Material: ['Material', 'Laboratorio', 'Otro'],
  Equipo: ['Equipo', 'Herramienta'],
  Transporte: ['Transporte'],
  ManoObra: ['ManoObraDestajo'],
}

type Insumo = {
  id: number
  codigo: string | null
  descripcion: string
  tipo: string
  unidadUso: string
  precioUnitario: number
  activo: boolean
}

type Cuadrilla = { id: number; nombre: string; costoDia: number; activo: boolean }
type Apu = { id: number; descripcion: string; unidad: string; esSubanalisis: boolean; costoTotal: number; activo: boolean }

export function SelectorOrigenLinea({
  grupo,
  apuId,
  alElegir,
  alCerrar,
}: {
  grupo: string
  apuId: number | null
  alElegir: (origen: OrigenLinea) => void
  alCerrar: () => void
}) {
  const [pestana, setPestana] = useState<'insumo' | 'cuadrilla' | 'subapu' | 'manual'>('insumo')
  const [texto, setTexto] = useState('')
  const insumos = useQuery({
    queryKey: ['insumos-origen'],
    queryFn: async () => (await cliente.get<Insumo[]>('/api/insumos', { params: { incluirInactivos: false } })).data,
  })
  const cuadrillas = useQuery({
    queryKey: ['cuadrillas-origen'],
    queryFn: async () => (await cliente.get<Cuadrilla[]>('/api/cuadrillas')).data,
  })
  const apus = useQuery({
    queryKey: ['apus-origen'],
    queryFn: async () => (await cliente.get<Apu[]>('/api/apu')).data,
  })
  const filtro = texto.trim().toLowerCase()
  const permitidos = tiposPorGrupo[grupo] ?? []
  const insumosVisibles = (insumos.data ?? []).filter(
    (insumo) =>
      permitidos.includes(insumo.tipo) &&
      `${insumo.codigo ?? ''} ${insumo.descripcion}`.toLowerCase().includes(filtro),
  )
  const subanalisis = (apus.data ?? []).filter(
    (apu) => apu.esSubanalisis && apu.activo && apu.id !== apuId && apu.descripcion.toLowerCase().includes(filtro),
  )

  return (
    <Modal show centered size="lg" onHide={alCerrar}>
      <Modal.Header closeButton>
        <Modal.Title>Origen de la línea</Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <Nav variant="tabs" activeKey={pestana} onSelect={(clave) => setPestana((clave as typeof pestana) || 'insumo')} className="mb-3">
          <Nav.Item><Nav.Link eventKey="insumo">Insumo</Nav.Link></Nav.Item>
          <Nav.Item><Nav.Link eventKey="cuadrilla">Cuadrilla</Nav.Link></Nav.Item>
          <Nav.Item><Nav.Link eventKey="subapu">Sub-APU</Nav.Link></Nav.Item>
          <Nav.Item><Nav.Link eventKey="manual">Precio manual</Nav.Link></Nav.Item>
        </Nav>
        {pestana !== 'manual' && <Form.Control className="mb-2" placeholder="Buscar" value={texto} onChange={(evento) => setTexto(evento.target.value)} />}
        {pestana === 'insumo' && insumosVisibles.slice(0, 30).map((insumo) => (
          <Button key={insumo.id} type="button" variant="link" className="d-block text-start" onClick={() => alElegir({ insumoId: insumo.id, descripcion: insumo.descripcion, unidad: insumo.unidadUso, precio: insumo.precioUnitario, precioManual: null })}>
            {insumo.codigo} {insumo.descripcion} · {dinero(insumo.precioUnitario)}
          </Button>
        ))}
        {pestana === 'cuadrilla' && (cuadrillas.data ?? []).filter((cuadrilla) => cuadrilla.nombre.toLowerCase().includes(filtro)).map((cuadrilla) => (
          <Button key={cuadrilla.id} type="button" variant="link" className="d-block text-start" onClick={() => alElegir({ cuadrillaId: cuadrilla.id, descripcion: cuadrilla.nombre, unidad: 'DIA', precio: cuadrilla.costoDia, precioManual: null })}>
            {cuadrilla.nombre} · {dinero(cuadrilla.costoDia)} / día
          </Button>
        ))}
        {pestana === 'subapu' && subanalisis.slice(0, 30).map((apu) => (
          <Button key={apu.id} type="button" variant="link" className="d-block text-start" onClick={() => alElegir({ subApuId: apu.id, descripcion: apu.descripcion, unidad: apu.unidad, precio: apu.costoTotal, precioManual: null })}>
            {apu.descripcion} · {dinero(apu.costoTotal)}
          </Button>
        ))}
        {pestana === 'manual' && (
          <Button type="button" onClick={() => alElegir({ descripcion: '', unidad: '', precio: 0, precioManual: 0 })}>
            Usar precio manual
          </Button>
        )}
      </Modal.Body>
    </Modal>
  )
}
