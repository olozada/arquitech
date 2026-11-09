import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Form, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import type { RegistroCatalogo } from '../catalogos/definiciones'
import { dinero, numero } from '../actas/formato'

type MaterialAgregado = { id: number; descripcion: string; precio: number }
type MaterialDisponible = { id: number; descripcion: string }

const mensajeSeleccion = 'Debe seleccionar materiales.'
const mensajePrecio = 'Debe seleccionar valores superiores a cero.'

export function MaterialesProveedor() {
  const consultas = useQueryClient()
  const [proveedorId, setProveedorId] = useState(0)
  const [agregadosMarcados, setAgregadosMarcados] = useState<Set<number>>(new Set())
  const [disponiblesMarcados, setDisponiblesMarcados] = useState<Set<number>>(new Set())
  const [precios, setPrecios] = useState<Record<number, string>>({})

  const proveedores = useQuery({
    queryKey: ['catalogo', 'proveedores'],
    queryFn: async () => (await cliente.get<RegistroCatalogo[]>('/api/proveedores')).data,
  })
  const agregados = useQuery({
    queryKey: ['proveedor-materiales', proveedorId, 'agregados'],
    enabled: proveedorId > 0,
    queryFn: async () =>
      (await cliente.get<MaterialAgregado[]>(`/api/proveedores/${proveedorId}/materiales`)).data,
  })
  const disponibles = useQuery({
    queryKey: ['proveedor-materiales', proveedorId, 'disponibles'],
    enabled: proveedorId > 0,
    queryFn: async () =>
      (await cliente.get<MaterialDisponible[]>(`/api/proveedores/${proveedorId}/materiales/disponibles`)).data,
  })

  const agregar = useMutation({
    mutationFn: async (materiales: { materialId: number; precio: number }[]) => {
      await cliente.post(`/api/proveedores/${proveedorId}/materiales`, materiales)
    },
    onSuccess: async () => {
      setDisponiblesMarcados(new Set())
      await refrescar()
      await avisarExito()
    },
    onError: (error) => avisarError(error),
  })

  const quitar = useMutation({
    mutationFn: async (ids: number[]) => {
      await cliente.delete(`/api/proveedores/${proveedorId}/materiales`, { data: ids })
    },
    onSuccess: async () => {
      setAgregadosMarcados(new Set())
      await refrescar()
      await avisarExito()
    },
    onError: (error) => avisarError(error),
  })

  function cambiarProveedor(valor: string) {
    setProveedorId(Number(valor))
    setAgregadosMarcados(new Set())
    setDisponiblesMarcados(new Set())
  }

  function refrescar() {
    return consultas.invalidateQueries({ queryKey: ['proveedor-materiales', proveedorId] })
  }

  function pedirAgregar() {
    const marcados = (disponibles.data ?? []).filter((material) => disponiblesMarcados.has(material.id))
    if (marcados.length === 0) {
      void avisarTexto(mensajeSeleccion)
      return
    }

    const materiales = marcados.map((material) => ({
      materialId: material.id,
      precio: numero(precios[material.id] ?? '') ?? 0,
    }))
    if (materiales.some((material) => material.precio <= 0)) {
      void avisarTexto(mensajePrecio)
      return
    }

    agregar.mutate(materiales)
  }

  function pedirQuitar() {
    if (agregadosMarcados.size === 0) {
      void avisarTexto(mensajeSeleccion)
      return
    }

    quitar.mutate([...agregadosMarcados])
  }

  const filasAgregadas = agregados.data ?? []
  const filasDisponibles = disponibles.data ?? []

  return (
    <section>
      <h1>Materiales por proveedor</h1>
      <div className="catalogo-panel">
        <Form.Group controlId="proveedor-materiales">
          <Form.Label>Proveedor</Form.Label>
          <Form.Select value={proveedorId || ''} onChange={(evento) => cambiarProveedor(evento.target.value)}>
            <option value="">Seleccione</option>
            {(proveedores.data ?? []).map((proveedor) => (
              <option key={proveedor.id} value={proveedor.id}>
                {String(proveedor.nombre ?? '')}
              </option>
            ))}
          </Form.Select>
        </Form.Group>
      </div>
      {proveedorId === 0 ? (
        <p>Seleccione un proveedor.</p>
      ) : (
        <div className="dos-columnas">
          <section className="catalogo-panel">
            <h2>Agregados</h2>
            <TablaMateriales
              vacio="No hay materiales agregados."
              cargando={agregados.isPending}
              error={agregados.isError}
              filas={filasAgregadas.map((material) => ({
                id: material.id,
                descripcion: material.descripcion,
                extra: dinero(material.precio, 2),
              }))}
              columnaExtra="Precio"
              marcados={agregadosMarcados}
              alMarcar={setAgregadosMarcados}
            />
            <Button type="button" variant="danger" disabled={quitar.isPending} onClick={pedirQuitar}>
              Quitar
            </Button>
          </section>
          <section className="catalogo-panel">
            <h2>Sin agregar</h2>
            <TablaMateriales
              vacio="No hay materiales disponibles."
              cargando={disponibles.isPending}
              error={disponibles.isError}
              filas={filasDisponibles.map((material) => ({
                id: material.id,
                descripcion: material.descripcion,
              }))}
              columnaExtra="Precio"
              marcados={disponiblesMarcados}
              alMarcar={setDisponiblesMarcados}
              precio={(id) => precios[id] ?? ''}
              alPrecio={(id, valor) => setPrecios((actual) => ({ ...actual, [id]: valor }))}
            />
            <Button type="button" disabled={agregar.isPending} onClick={pedirAgregar}>
              Agregar
            </Button>
          </section>
        </div>
      )}
    </section>
  )
}

type Fila = { id: number; descripcion: string; extra?: string }

function TablaMateriales({
  filas,
  vacio,
  cargando,
  error,
  columnaExtra,
  marcados,
  alMarcar,
  precio,
  alPrecio,
}: {
  filas: Fila[]
  vacio: string
  cargando: boolean
  error: boolean
  columnaExtra: string
  marcados: Set<number>
  alMarcar: (marcados: Set<number>) => void
  precio?: (id: number) => string
  alPrecio?: (id: number, valor: string) => void
}) {
  if (cargando) {
    return <p>Cargando…</p>
  }

  if (error) {
    return <p>No se pudieron cargar los materiales.</p>
  }

  if (filas.length === 0) {
    return <p>{vacio}</p>
  }

  const todos = filas.every((fila) => marcados.has(fila.id))

  return (
    <div className="table-responsive mb-3">
      <Table hover className="catalogo-tabla mb-0">
        <thead>
          <tr>
            <th>
              <Form.Check
                aria-label="Seleccionar todos"
                checked={todos}
                onChange={() => alMarcar(todos ? new Set() : new Set(filas.map((fila) => fila.id)))}
              />
            </th>
            <th>Código</th>
            <th>Descripción</th>
            <th>{columnaExtra}</th>
          </tr>
        </thead>
        <tbody>
          {filas.map((fila) => (
            <tr key={fila.id}>
              <td>
                <Form.Check
                  aria-label={`Seleccionar ${fila.descripcion}`}
                  checked={marcados.has(fila.id)}
                  onChange={() => alMarcar(alternar(marcados, fila.id))}
                />
              </td>
              <td>{fila.id}</td>
              <td>{fila.descripcion}</td>
              <td>
                {alPrecio ? (
                  <Form.Control
                    value={precio?.(fila.id) ?? ''}
                    inputMode="decimal"
                    aria-label={`Precio de ${fila.descripcion}`}
                    onChange={(evento) => alPrecio(fila.id, evento.target.value)}
                  />
                ) : (
                  fila.extra
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </Table>
    </div>
  )
}

function alternar(marcados: Set<number>, id: number) {
  const siguiente = new Set(marcados)
  if (siguiente.has(id)) {
    siguiente.delete(id)
  } else {
    siguiente.add(id)
  }

  return siguiente
}

function avisarExito() {
  return Swal.fire({ icon: 'success', title: '¡Finalizado!', confirmButtonColor: '#1abb9c' })
}

function avisarTexto(texto: string) {
  return Swal.fire({ icon: 'error', title: '¡Error!', text: texto, confirmButtonColor: '#1abb9c' })
}

function avisarError(error: unknown) {
  return avisarTexto(mensajeError(error))
}
