import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button, Form, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { dinero as formatoDinero } from '../actas/formato'

function dinero(valor: number) {
  return formatoDinero(valor, 2)
}
import { EditorApu } from '../presupuesto/EditorApu'

type Resumen = {
  id: number
  codigo: string | null
  descripcion: string
  unidad: string
  esSubanalisis: boolean
  costoTotal: number
  usos: number
}

export function Apus() {
  const { id } = useParams()
  return id ? <EditorApu apuId={Number(id)} /> : <ListadoApu />
}

function ListadoApu() {
  const [texto, setTexto] = useState('')
  const [clase, setClase] = useState<'todos' | 'sub' | 'normales'>('todos')
  const navegar = useNavigate()
  const lista = useQuery({
    queryKey: ['apus', texto],
    queryFn: async () => (await cliente.get<Resumen[]>('/api/apu', { params: { texto: texto || undefined } })).data,
  })
  const filas = (lista.data ?? []).filter((apu) => {
    if (clase === 'sub') {
      return apu.esSubanalisis
    }
    if (clase === 'normales') {
      return !apu.esSubanalisis
    }
    return true
  })

  return (
    <section>
      <div className="d-flex justify-content-between mb-3">
        <h1 className="mb-0">Análisis de precios</h1>
        <Button type="button" onClick={() => void crear(navegar)}>
          + Nuevo APU
        </Button>
      </div>
      <div className="d-flex gap-2 mb-3">
        <Form.Control placeholder="Buscar" value={texto} onChange={(evento) => setTexto(evento.target.value)} />
        <Form.Select style={{ maxWidth: '14rem' }} value={clase} onChange={(evento) => setClase(evento.target.value as typeof clase)}>
          <option value="todos">Todos</option>
          <option value="sub">Sub-análisis</option>
          <option value="normales">Normales</option>
        </Form.Select>
      </div>
      <div className="catalogo-panel">
        <Table hover size="sm" className="mb-0">
          <thead>
            <tr>
              <th>Código</th>
              <th>Descripción</th>
              <th>Unidad</th>
              <th className="text-end">Usos</th>
              <th className="text-end">Costo</th>
            </tr>
          </thead>
          <tbody>
            {filas.map((apu) => (
              <tr key={apu.id}>
                <td>{apu.codigo}</td>
                <td>
                  <Link to={`/presupuesto/apu/${apu.id}`}>{apu.descripcion}</Link>
                </td>
                <td>{apu.unidad}</td>
                <td className="text-end">{apu.usos}</td>
                <td className="text-end">{dinero(apu.costoTotal)}</td>
              </tr>
            ))}
          </tbody>
        </Table>
      </div>
    </section>
  )
}

async function crear(navegar: (ruta: string) => void) {
  try {
    const { data } = await cliente.post<{ id: number }>('/api/apu', {
      descripcion: 'Nuevo análisis',
      unidad: '',
      divisorGeneral: 1,
      activo: true,
      grupos: [
        { grupo: 'Material', divisor: 1, lineas: [] },
        { grupo: 'ManoObra', divisor: 1, lineas: [] },
      ],
    })
    navegar(`/presupuesto/apu/${data.id}`)
  } catch (error) {
    await Swal.fire({ icon: 'error', text: mensajeError(error) })
  }
}
