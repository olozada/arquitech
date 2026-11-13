import { useQuery } from '@tanstack/react-query'
import { Button, Form, Table } from 'react-bootstrap'
import { cliente } from '../api/cliente'
import type { ItemPresupuesto } from '../api/presupuesto'
import { parcial } from './mediciones'

export function SelectorConcepto({
  texto,
  alBuscar,
  alElegir,
}: {
  texto: string
  alBuscar: (texto: string) => void
  alElegir: (conceptoId: number) => void
}) {
  const conceptos = useQuery({
    queryKey: ['conceptos'],
    queryFn: async () => (await cliente.get<Array<{ id: number; descripcion: string }>>('/api/conceptos')).data,
  })
  const filtro = texto.trim().toLowerCase()
  return (
    <>
      <Form.Control className="mb-2" placeholder="Buscar concepto" value={texto} onChange={(evento) => alBuscar(evento.target.value)} />
      {(conceptos.data ?? []).filter((concepto) => concepto.descripcion.toLowerCase().includes(filtro)).slice(0, 20).map((concepto) => (
        <Button key={concepto.id} type="button" variant="link" className="d-block text-start" onClick={() => alElegir(concepto.id)}>
          {concepto.descripcion}
        </Button>
      ))}
    </>
  )
}

export function Mediciones({
  item,
  editable,
  alCambiar,
}: {
  item: ItemPresupuesto
  editable: boolean
  alCambiar: (mediciones: ItemPresupuesto['mediciones']) => void
}) {
  return (
    <>
      <Table size="sm">
        <thead>
          <tr>
            <th>Ubicación</th>
            <th>Veces</th>
            <th>Largo</th>
            <th>Ancho</th>
            <th>Alto</th>
            <th>Parcial</th>
          </tr>
        </thead>
        <tbody>
          {item.mediciones.map((medicion, indice) => (
            <tr key={indice}>
              <td>
                <Form.Control size="sm" value={medicion.ubicacion} disabled={!editable} onChange={(evento) => editar(indice, { ubicacion: evento.target.value })} />
              </td>
              {(['veces', 'largo', 'ancho', 'alto'] as const).map((campo) => (
                <td key={campo}>
                  <Form.Control
                    size="sm"
                    value={medicion[campo] ?? ''}
                    disabled={!editable}
                    onChange={(evento) => {
                      const texto = evento.target.value
                      const numero = Number(texto)
                      if (campo === 'veces') {
                        editar(indice, { veces: texto === '' || !Number.isFinite(numero) ? 1 : numero })
                        return
                      }
                      editar(indice, { [campo]: texto === '' ? null : numero })
                    }}
                  />
                </td>
              ))}
              <td>{parcial(medicion.veces, medicion.largo, medicion.ancho, medicion.alto).toLocaleString('es-CO')}</td>
            </tr>
          ))}
        </tbody>
      </Table>
      {editable && (
        <Button type="button" variant="link" onClick={() => alCambiar([...item.mediciones, { id: 0, ubicacion: '', veces: 1, largo: null, ancho: null, alto: null, parcial: 1 }])}>
          Agregar medida
        </Button>
      )}
    </>
  )

  function editar(indice: number, cambios: Partial<ItemPresupuesto['mediciones'][number]>) {
    alCambiar(item.mediciones.map((medicion, posicion) => (posicion === indice ? { ...medicion, ...cambios } : medicion)))
  }
}
