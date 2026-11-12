import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Form, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { dinero as formatoDinero } from '../actas/formato'

function dinero(valor: number) {
  return formatoDinero(valor, 2)
}
import { CatalogoCrud } from '../componentes/CatalogoCrud'
import type { ConfiguracionCatalogo } from '../catalogos/definiciones'

type Parametros = { diasMes: number; incluirDiasNoHabiles: boolean; diasHabilesAnio: number; factorPrestacional: number }
type Prestacion = { id: number; concepto: string; porcentaje: number; orden: number }
type Cargo = { id: number; nombre: string; salarioMensual: number; jornal: number }
type Integrante = { cargoId: number; cargo: string; cantidad: number }
type Cuadrilla = { id: number; nombre: string; descripcion: string | null; costoDia: number; integrantes: Integrante[] }

const cargos: ConfiguracionCatalogo = {
  titulo: 'Cargos',
  recurso: 'cargos',
  ruta: '/presupuesto/mano-obra',
  campos: [
    { nombre: 'nombre', etiqueta: 'Nombre', requerido: true, maximo: 80, mensajeRequerido: 'Debe ingresar el nombre.', mensajeMaximo: 'Máximo 80 caracteres.' },
    { nombre: 'salarioMensual', etiqueta: 'Salario mensual', requerido: true, maximo: 20, mensajeRequerido: 'Debe ingresar el salario.', mensajeMaximo: 'Máximo 20 caracteres.', numero: true },
  ],
  columnas: [
    { clave: 'nombre', titulo: 'Cargo' },
    { clave: 'salarioMensual', titulo: 'Salario' },
    { clave: 'jornal', titulo: 'Jornal con prestaciones' },
  ],
}

export function ManoObraPresupuesto() {
  const consultas = useQueryClient()
  const parametros = useQuery({ queryKey: ['mo-parametros'], queryFn: async () => (await cliente.get<Parametros>('/api/mano-obra/parametros')).data })
  const prestaciones = useQuery({ queryKey: ['mo-prestaciones'], queryFn: async () => (await cliente.get<Prestacion[]>('/api/mano-obra/prestaciones')).data })
  const listaCargos = useQuery({ queryKey: ['mo-cargos'], queryFn: async () => (await cliente.get<Cargo[]>('/api/cargos')).data })
  const cuadrillas = useQuery({ queryKey: ['mo-cuadrillas'], queryFn: async () => (await cliente.get<Cuadrilla[]>('/api/cuadrillas')).data })
  const [filas, setFilas] = useState<Prestacion[] | null>(null)
  const [incluir, setIncluir] = useState<boolean | null>(null)
  const [cuadrilla, setCuadrilla] = useState<Cuadrilla | null>(null)
  const actuales = filas ?? prestaciones.data ?? []
  const factorBase = actuales.reduce((suma, fila) => suma + Number(fila.porcentaje), 0)
  const dias = parametros.data?.diasHabilesAnio ?? 296
  const conDias = incluir ?? parametros.data?.incluirDiasNoHabiles ?? false
  const factor = factorBase + (conDias && dias > 0 ? 365 / dias - 1 : 0)

  async function guardarPrestaciones() {
    try {
      await cliente.put('/api/mano-obra/prestaciones', actuales.map((fila, indice) => ({ concepto: fila.concepto, porcentaje: Number(fila.porcentaje), orden: indice + 1 })))
      if (parametros.data) {
        await cliente.put('/api/mano-obra/parametros', { ...parametros.data, incluirDiasNoHabiles: conDias })
      }
      setFilas(null)
      setIncluir(null)
      await consultas.invalidateQueries({ queryKey: ['mo-prestaciones'] })
      await consultas.invalidateQueries({ queryKey: ['mo-parametros'] })
      await consultas.invalidateQueries({ queryKey: ['mo-cargos'] })
      await consultas.invalidateQueries({ queryKey: ['mo-cuadrillas'] })
    } catch (error) {
      await Swal.fire({ icon: 'error', text: mensajeError(error) })
    }
  }

  async function guardarCuadrilla() {
    if (!cuadrilla) {
      return
    }
    try {
      const cuerpo = {
        nombre: cuadrilla.nombre,
        descripcion: cuadrilla.descripcion,
        activo: true,
        integrantes: cuadrilla.integrantes.map((item) => ({ cargoId: item.cargoId, cantidad: item.cantidad })),
      }
      if (cuadrilla.id > 0) {
        await cliente.put(`/api/cuadrillas/${cuadrilla.id}`, cuerpo)
      } else {
        await cliente.post('/api/cuadrillas', cuerpo)
      }
      setCuadrilla(null)
      await consultas.invalidateQueries({ queryKey: ['mo-cuadrillas'] })
    } catch (error) {
      await Swal.fire({ icon: 'error', text: mensajeError(error) })
    }
  }

  const costoVista = cuadrilla
    ? cuadrilla.integrantes.reduce((suma, item) => {
        const cargo = (listaCargos.data ?? []).find((candidato) => candidato.id === item.cargoId)
        return suma + (cargo?.jornal ?? 0) * Number(item.cantidad)
      }, 0)
    : 0

  return (
    <section>
      <h1>Mano de obra</h1>
      <p className="alert alert-info">Cambiar salarios o prestaciones afecta todos los APU. Los presupuestos aprobados no cambian; los borradores se actualizan con Recalcular.</p>
      <div className="catalogo-panel mb-3">
        <h2 className="h5">Prestaciones</h2>
        <Table size="sm">
          <thead>
            <tr>
              <th>Concepto</th>
              <th>%</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {actuales.map((fila, indice) => (
              <tr key={fila.id || indice}>
                <td><Form.Control value={fila.concepto} onChange={(evento) => cambiar(indice, { concepto: evento.target.value })} /></td>
                <td><Form.Control value={fila.porcentaje} onChange={(evento) => cambiar(indice, { porcentaje: Number(evento.target.value) || 0 })} /></td>
                <td>
                  <Button type="button" size="sm" variant="link" onClick={() => mover(indice, -1)}>↑</Button>
                  <Button type="button" size="sm" variant="link" onClick={() => mover(indice, 1)}>↓</Button>
                  <Button type="button" size="sm" variant="link" className="text-danger" onClick={() => setFilas(actuales.filter((_, posicion) => posicion !== indice))}>Quitar</Button>
                </td>
              </tr>
            ))}
          </tbody>
        </Table>
        <Button type="button" variant="link" onClick={() => setFilas([...actuales, { id: 0, concepto: '', porcentaje: 0, orden: actuales.length + 1 }])}>Agregar</Button>
        <p>Factor prestacional {factor.toLocaleString('es-CO', { maximumFractionDigits: 4 })} {parametros.data ? `(antes ${parametros.data.factorPrestacional.toLocaleString('es-CO', { maximumFractionDigits: 4 })})` : ''}</p>
        <Form.Check label="Incluir días no hábiles" checked={conDias} onChange={(evento) => setIncluir(evento.target.checked)} />
        <Button type="button" className="mt-2" onClick={() => void guardarPrestaciones()}>Guardar</Button>
      </div>
      <CatalogoCrud configuracion={cargos} />
      <div className="catalogo-panel mt-3">
        <div className="d-flex justify-content-between">
          <h2 className="h5">Cuadrillas</h2>
          <Button type="button" size="sm" onClick={() => setCuadrilla({ id: 0, nombre: '', descripcion: '', costoDia: 0, integrantes: [] })}>Nueva cuadrilla</Button>
        </div>
        <Table size="sm">
          <tbody>
            {(cuadrillas.data ?? []).map((item) => (
              <tr key={item.id}>
                <td><Button type="button" variant="link" onClick={() => setCuadrilla(structuredClone(item))}>{item.nombre}</Button></td>
                <td className="text-end">{dinero(item.costoDia)} / día</td>
              </tr>
            ))}
          </tbody>
        </Table>
        {cuadrilla && (
          <div className="mt-3">
            <Form.Control className="mb-2" placeholder="Nombre" value={cuadrilla.nombre} onChange={(evento) => setCuadrilla({ ...cuadrilla, nombre: evento.target.value })} />
            <Form.Control className="mb-2" placeholder="Descripción" value={cuadrilla.descripcion ?? ''} onChange={(evento) => setCuadrilla({ ...cuadrilla, descripcion: evento.target.value })} />
            {cuadrilla.integrantes.map((item, indice) => (
              <div key={indice} className="d-flex gap-2 mb-2">
                <Form.Select value={item.cargoId} onChange={(evento) => {
                  const cargoId = Number(evento.target.value)
                  const cargo = (listaCargos.data ?? []).find((candidato) => candidato.id === cargoId)
                  const integrantes = cuadrilla.integrantes.map((fila, posicion) => posicion === indice ? { ...fila, cargoId, cargo: cargo?.nombre ?? '' } : fila)
                  setCuadrilla({ ...cuadrilla, integrantes })
                }}>
                  <option value={0}>Cargo</option>
                  {(listaCargos.data ?? []).map((cargo) => <option key={cargo.id} value={cargo.id}>{cargo.nombre}</option>)}
                </Form.Select>
                <Form.Control value={item.cantidad} onChange={(evento) => {
                  const integrantes = cuadrilla.integrantes.map((fila, posicion) => posicion === indice ? { ...fila, cantidad: Number(evento.target.value) || 0 } : fila)
                  setCuadrilla({ ...cuadrilla, integrantes })
                }} />
                <Button type="button" variant="outline-danger" onClick={() => setCuadrilla({ ...cuadrilla, integrantes: cuadrilla.integrantes.filter((_, posicion) => posicion !== indice) })}>×</Button>
              </div>
            ))}
            <Button type="button" variant="link" onClick={() => setCuadrilla({ ...cuadrilla, integrantes: [...cuadrilla.integrantes, { cargoId: 0, cargo: '', cantidad: 1 }] })}>Agregar integrante</Button>
            <p>Costo por día {dinero(costoVista)}</p>
            <Button type="button" onClick={() => void guardarCuadrilla()}>Guardar cuadrilla</Button>
          </div>
        )}
      </div>
    </section>
  )

  function cambiar(indice: number, cambios: Partial<Prestacion>) {
    setFilas(actuales.map((fila, posicion) => posicion === indice ? { ...fila, ...cambios } : fila))
  }

  function mover(indice: number, delta: number) {
    const destino = indice + delta
    if (destino < 0 || destino >= actuales.length) {
      return
    }
    const copia = [...actuales]
    const [fila] = copia.splice(indice, 1)
    copia.splice(destino, 0, fila)
    setFilas(copia)
  }
}
