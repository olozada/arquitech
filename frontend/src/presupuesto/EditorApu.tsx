import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Form, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { dinero as formatoDinero, redondear } from '../actas/formato'

function dinero(valor: number) {
  return formatoDinero(valor, 2)
}
import { calcularApu } from './calculadoraApu'
import { SelectorOrigenLinea, type OrigenLinea } from './SelectorOrigenLinea'

type Linea = {
  clave: number
  insumoId: number | null
  cuadrillaId: number | null
  subApuId: number | null
  descripcion: string
  unidad: string
  cantidad: number
  precioManual: number | null
  modo: string
  factor: number
  precio: number
}

type Grupo = { clave: number; grupo: string; divisor: number; lineas: Linea[] }

type ApuServidor = {
  id: number
  codigo: string | null
  descripcion: string
  unidad: string
  unidadMedidaId: number | null
  esSubanalisis: boolean
  divisorGeneral: number
  conceptoId: number | null
  activo: boolean
  costoTotal: number
  grupos: Array<{
    id: number
    grupo: string
    divisor: number
    subtotal: number
    lineas: Array<Omit<Linea, 'clave'> & { id: number }>
  }>
}

type Uso = { tipo: string; id: number; nombre: string; aprobado: boolean }
type Unidad = { id: number; nombre: string; activo: boolean }
type Concepto = { id: number; descripcion: string; activo: boolean }

const gruposPosibles = ['Material', 'ManoObra', 'Equipo', 'Transporte', 'Otros']
const modos = ['Desperdicio', 'Rendimiento', 'Divisor', 'PorcentajeManoObra']
let clave = 1
const nuevaClave = () => clave++

function desdeServidor(apu: ApuServidor): Grupo[] {
  return apu.grupos.map((grupo) => ({
    clave: nuevaClave(),
    grupo: grupo.grupo,
    divisor: grupo.divisor,
    lineas: grupo.lineas.map((linea) => ({ ...linea, clave: nuevaClave() })),
  }))
}

function vacio(unidad: string): { descripcion: string; unidad: string; grupos: Grupo[] } {
  return {
    descripcion: 'Nuevo análisis',
    unidad,
    grupos: [
      { clave: nuevaClave(), grupo: 'Material', divisor: 1, lineas: [] },
      { clave: nuevaClave(), grupo: 'ManoObra', divisor: 1, lineas: [] },
    ],
  }
}

export function EditorApu({
  apuId,
  unidadInicial,
  embebido,
  alGuardar,
  alCancelar,
}: {
  apuId: number | null
  unidadInicial?: string
  embebido?: boolean
  alGuardar?: (apu: ApuServidor) => void
  alCancelar?: () => void
}) {
  const consulta = useQuery({
    queryKey: ['apu', apuId],
    enabled: apuId != null && apuId > 0,
    queryFn: async () => (await cliente.get<ApuServidor>(`/api/apu/${apuId}`)).data,
  })
  const usos = useQuery({
    queryKey: ['apu-usos', apuId],
    enabled: apuId != null && apuId > 0 && !embebido,
    queryFn: async () => (await cliente.get<Uso[]>(`/api/apu/${apuId}/usos`)).data,
  })
  const unidades = useQuery({
    queryKey: ['unidades-medida'],
    queryFn: async () => (await cliente.get<Unidad[]>('/api/unidades-medida')).data,
  })
  const conceptos = useQuery({
    queryKey: ['conceptos'],
    queryFn: async () => (await cliente.get<Concepto[]>('/api/conceptos')).data,
  })
  const [grupos, setGrupos] = useState<Grupo[]>(() => (apuId ? [] : vacio(unidadInicial ?? '').grupos))
  const [codigo, setCodigo] = useState<string | null>(null)
  const [descripcion, setDescripcion] = useState(() => (apuId ? '' : 'Nuevo análisis'))
  const [unidadMedidaId, setUnidadMedidaId] = useState<number | null>(null)
  const [esSubanalisis, setEsSubanalisis] = useState(false)
  const [divisorGeneral, setDivisorGeneral] = useState(1)
  const [conceptoId, setConceptoId] = useState<number | null>(null)
  const [activo, setActivo] = useState(true)
  const [origen, setOrigen] = useState<number | null>(null)
  const [listo, setListo] = useState(apuId == null)
  const consultas = useQueryClient()
  const navegar = useNavigate()

  useEffect(() => {
    if (!consulta.data || listo) {
      return
    }
    setCodigo(consulta.data.codigo)
    setDescripcion(consulta.data.descripcion)
    setUnidadMedidaId(consulta.data.unidadMedidaId)
    setEsSubanalisis(consulta.data.esSubanalisis)
    setDivisorGeneral(consulta.data.divisorGeneral || 1)
    setConceptoId(consulta.data.conceptoId)
    setActivo(consulta.data.activo)
    setGrupos(desdeServidor(consulta.data))
    setListo(true)
  }, [consulta.data, listo])

  useEffect(() => {
    if (unidadMedidaId != null || !unidadInicial || !unidades.data) {
      return
    }
    const coincidencia = unidades.data.find((item) => item.nombre.toLowerCase() === unidadInicial.toLowerCase())
    if (coincidencia) {
      setUnidadMedidaId(coincidencia.id)
    }
  }, [unidadInicial, unidadMedidaId, unidades.data])

  const gruposActuales = grupos
  const unidad = (unidades.data ?? []).find((item) => item.id === unidadMedidaId)?.nombre ?? consulta.data?.unidad ?? unidadInicial ?? ''
  const calculo = useMemo(
    () =>
      calcularApu(
        gruposActuales.map((grupo) => ({
          tipo: grupo.grupo,
          divisor: grupo.divisor,
          lineas: grupo.lineas.map((linea) => ({
            modo: linea.modo,
            cantidad: linea.cantidad,
            factor: linea.factor,
            precio: linea.precioManual ?? linea.precio,
            herramientaMenor: linea.modo === 'PorcentajeManoObra',
          })),
        })),
        divisorGeneral,
      ),
    [gruposActuales, divisorGeneral],
  )

  if (apuId && !consulta.data) {
    return <p>Cargando APU…</p>
  }

  function actualizarLinea(grupoClave: number, lineaClave: number, cambios: Partial<Linea>) {
    setGrupos(
      gruposActuales.map((grupo) =>
        grupo.clave === grupoClave
          ? { ...grupo, lineas: grupo.lineas.map((linea) => (linea.clave === lineaClave ? { ...linea, ...cambios } : linea)) }
          : grupo,
      ),
    )
  }

  async function guardar() {
    const cuerpo = {
      codigo,
      descripcion,
      unidad,
      unidadMedidaId,
      esSubanalisis,
      divisorGeneral: divisorGeneral || 1,
      conceptoId,
      activo,
      grupos: gruposActuales.map((grupo) => ({
        grupo: grupo.grupo,
        divisor: grupo.divisor || 1,
        lineas: grupo.lineas.map((linea) => ({
          insumoId: linea.insumoId,
          cuadrillaId: linea.cuadrillaId,
          subApuId: linea.subApuId,
          descripcion: linea.descripcion,
          unidad: linea.unidad,
          cantidad: linea.cantidad,
          precioManual: linea.insumoId || linea.cuadrillaId || linea.subApuId ? null : linea.precioManual,
          modo: linea.modo,
          factor: linea.factor,
        })),
      })),
    }
    const tieneValor = calculo.detalle.some((grupo) => grupo.lineas.some((valor) => valor > 0))
    if (apuId == null && !tieneValor) {
      await Swal.fire({ icon: 'warning', text: 'Agregue al menos una línea con valor mayor que cero.' })
      return
    }
    try {
      const { data } = apuId
        ? await cliente.put<ApuServidor>(`/api/apu/${apuId}`, cuerpo)
        : await cliente.post<ApuServidor>('/api/apu', cuerpo)
      if (Math.abs(data.costoTotal - calculo.total) > 1) {
        await Swal.fire({ icon: 'info', text: `El servidor calculó ${dinero(data.costoTotal)} y la vista previa ${dinero(calculo.total)}.` })
      }
      await consultas.invalidateQueries({ queryKey: ['apu', data.id] })
      await consultas.invalidateQueries({ queryKey: ['apus'] })
      alGuardar?.(data)
      if (!embebido && apuId == null) {
        navegar(`/presupuesto/apu/${data.id}`)
      }
    } catch (error) {
      const texto = mensajeError(error)
      await Swal.fire({
        icon: 'error',
        text: texto.toLowerCase().includes('circular') ? 'Este sub-análisis crearía una referencia circular' : texto,
      })
    }
  }

  async function duplicar() {
    if (!apuId) {
      return
    }
    const { data } = await cliente.post<ApuServidor>(`/api/apu/${apuId}/duplicar`)
    navegar(`/presupuesto/apu/${data.id}`)
  }

  const usadoEnAprobado = (usos.data ?? []).some((uso) => uso.aprobado)

  return (
    <section>
      {!embebido && <Link to="/presupuesto/apu">Volver</Link>}
      <div className="d-flex justify-content-between align-items-center mt-2 mb-3">
        <h1 className="h4 mb-0">{descripcion || 'Análisis de precios'}</h1>
        <div className="d-flex gap-2">
          {alCancelar && <Button type="button" variant="outline-secondary" onClick={alCancelar}>Cancelar</Button>}
          {apuId != null && !embebido && <Button type="button" variant="outline-secondary" onClick={() => void duplicar()}>Duplicar</Button>}
          <Button type="button" onClick={() => void guardar()}>Guardar</Button>
        </div>
      </div>
      <p>Vista previa {dinero(calculo.total)}{consulta.data ? ` · servidor ${dinero(consulta.data.costoTotal)}` : ''}</p>
      <div className="row g-2 mb-3">
        <div className="col-md-2"><Form.Label>Código</Form.Label><Form.Control value={codigo ?? ''} onChange={(evento) => setCodigo(evento.target.value)} /></div>
        <div className="col-md-4"><Form.Label>Descripción</Form.Label><Form.Control value={descripcion} onChange={(evento) => setDescripcion(evento.target.value)} /></div>
        <div className="col-md-2">
          <Form.Label>Unidad</Form.Label>
          <Form.Select value={unidadMedidaId ?? ''} onChange={(evento) => setUnidadMedidaId(Number(evento.target.value) || null)}>
            <option value="">Seleccione</option>
            {(unidades.data ?? []).filter((item) => item.activo !== false).map((item) => <option key={item.id} value={item.id}>{item.nombre}</option>)}
          </Form.Select>
        </div>
        <div className="col-md-2">
          <Form.Label>Concepto</Form.Label>
          <Form.Select value={conceptoId ?? ''} onChange={(evento) => setConceptoId(Number(evento.target.value) || null)}>
            <option value="">Sin concepto</option>
            {(conceptos.data ?? []).map((item) => <option key={item.id} value={item.id}>{item.descripcion}</option>)}
          </Form.Select>
        </div>
        <div className="col-md-2"><Form.Label>Divisor general</Form.Label><Form.Control value={divisorGeneral} onChange={(evento) => setDivisorGeneral(Number(evento.target.value) || 0)} /></div>
      </div>
      <Form.Check className="mb-2" label="Es sub-análisis" checked={esSubanalisis} onChange={(evento) => setEsSubanalisis(evento.target.checked)} />
      <Form.Check className="mb-3" label="Activo" checked={activo} onChange={(evento) => setActivo(evento.target.checked)} />
      {gruposActuales.map((grupo, indiceGrupo) => (
        <div key={grupo.clave} className="catalogo-panel mb-3">
          <div className="d-flex justify-content-between">
            <h2 className="h6">{grupo.grupo} · {dinero(calculo.detalle[indiceGrupo]?.subtotal ?? 0)}</h2>
            <div className="d-flex gap-2 align-items-center">
              <Form.Label className="mb-0">Divisor</Form.Label>
              <Form.Control style={{ width: '6rem' }} value={grupo.divisor} onChange={(evento) => setGrupos(gruposActuales.map((item) => item.clave === grupo.clave ? { ...item, divisor: Number(evento.target.value) || 0 } : item))} />
              <Button type="button" size="sm" variant="outline-danger" onClick={() => void quitarGrupo(grupo.clave)}>Quitar grupo</Button>
            </div>
          </div>
          <Table size="sm">
            <thead>
              <tr>
                <th>Descripción</th>
                <th>Unidad</th>
                <th>Cantidad</th>
                <th>Modo</th>
                <th>Factor</th>
                <th className="text-end">Precio</th>
                <th className="text-end">Valor</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {grupo.lineas.map((linea, indiceLinea) => (
                <tr key={linea.clave}>
                  <td><Form.Control size="sm" value={linea.descripcion} onChange={(evento) => actualizarLinea(grupo.clave, linea.clave, { descripcion: evento.target.value })} /></td>
                  <td>{linea.unidad}</td>
                  <td><Form.Control size="sm" value={linea.cantidad} onChange={(evento) => actualizarLinea(grupo.clave, linea.clave, { cantidad: Number(evento.target.value) || 0 })} /></td>
                  <td>
                    <Form.Select size="sm" value={linea.modo} onChange={(evento) => actualizarLinea(grupo.clave, linea.clave, { modo: evento.target.value })}>
                      {modos.filter((modo) => modo !== 'PorcentajeManoObra' || grupo.grupo === 'ManoObra').map((modo) => <option key={modo}>{modo}</option>)}
                    </Form.Select>
                    <div className="small text-secondary">{ayuda(linea, grupo.grupo)}</div>
                  </td>
                  <td>
                    {linea.modo === 'Rendimiento' && linea.cuadrillaId ? (
                      <Form.Control
                        size="sm"
                        value={linea.factor === 0 ? '' : 1 / linea.factor}
                        onChange={(evento) => {
                          const rendimiento = Number(evento.target.value) || 0
                          actualizarLinea(grupo.clave, linea.clave, { factor: rendimiento === 0 ? 0 : 1 / rendimiento })
                        }}
                      />
                    ) : (
                      <Form.Control size="sm" value={linea.factor} onChange={(evento) => actualizarLinea(grupo.clave, linea.clave, { factor: Number(evento.target.value) || 0 })} />
                    )}
                  </td>
                  <td className="text-end">
                    {linea.insumoId || linea.cuadrillaId || linea.subApuId ? dinero(linea.precio) : (
                      <Form.Control size="sm" className="text-end" value={redondear(linea.precioManual ?? 0, 2)} onChange={(evento) => actualizarLinea(grupo.clave, linea.clave, { precioManual: Number(evento.target.value) || 0, precio: Number(evento.target.value) || 0 })} />
                    )}
                  </td>
                  <td className="text-end">{dinero(calculo.detalle[indiceGrupo]?.lineas[indiceLinea] ?? 0)}</td>
                  <td className="text-nowrap">
                    <Button type="button" size="sm" variant="link" onClick={() => mover(grupo.clave, indiceLinea, -1)}>↑</Button>
                    <Button type="button" size="sm" variant="link" onClick={() => mover(grupo.clave, indiceLinea, 1)}>↓</Button>
                    <Button type="button" size="sm" variant="link" className="text-danger" onClick={() => setGrupos(gruposActuales.map((item) => item.clave === grupo.clave ? { ...item, lineas: item.lineas.filter((fila) => fila.clave !== linea.clave) } : item))}>×</Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
          <Button type="button" size="sm" variant="outline-primary" onClick={() => setOrigen(grupo.clave)}>+ Línea</Button>
        </div>
      ))}
      <Form.Select className="mb-3" style={{ maxWidth: '16rem' }} value="" onChange={(evento) => agregarGrupo(evento.target.value)}>
        <option value="">+ Grupo</option>
        {gruposPosibles.filter((tipo) => !gruposActuales.some((grupo) => grupo.grupo === tipo)).map((tipo) => <option key={tipo}>{tipo}</option>)}
      </Form.Select>
      {!embebido && apuId != null && (
        <div className="catalogo-panel">
          <h2 className="h6">Se usa en</h2>
          {usadoEnAprobado && <p className="text-warning">Los presupuestos aprobados no cambian; los borradores se recalculan al pulsar Recalcular.</p>}
          <ul>
            {(usos.data ?? []).map((uso) => (
              <li key={`${uso.tipo}-${uso.id}-${uso.nombre}`}>
                {uso.tipo === 'Presupuesto' ? <Link to={`/presupuestos/${uso.id}`}>{uso.nombre}</Link> : <Link to={`/presupuesto/apu/${uso.id}`}>{uso.nombre}</Link>}
              </li>
            ))}
          </ul>
        </div>
      )}
      {origen != null && (
        <SelectorOrigenLinea
          grupo={gruposActuales.find((grupo) => grupo.clave === origen)?.grupo ?? 'Material'}
          apuId={apuId}
          alCerrar={() => setOrigen(null)}
          alElegir={(elegido) => {
            agregarLinea(origen, elegido)
            setOrigen(null)
          }}
        />
      )}
    </section>
  )

  function agregarGrupo(tipo: string) {
    if (!tipo) {
      return
    }
    setGrupos([...gruposActuales, { clave: nuevaClave(), grupo: tipo, divisor: 1, lineas: [] }])
  }

  async function quitarGrupo(grupoClave: number) {
    const confirmacion = await Swal.fire({ icon: 'warning', title: '¿Eliminar el grupo?', showCancelButton: true, confirmButtonColor: '#1abb9c' })
    if (confirmacion.isConfirmed) {
      setGrupos(gruposActuales.filter((grupo) => grupo.clave !== grupoClave))
    }
  }

  function agregarLinea(grupoClave: number, elegido: OrigenLinea) {
    const linea: Linea = {
      clave: nuevaClave(),
      insumoId: elegido.insumoId ?? null,
      cuadrillaId: elegido.cuadrillaId ?? null,
      subApuId: elegido.subApuId ?? null,
      descripcion: elegido.descripcion,
      unidad: elegido.unidad,
      cantidad: 1,
      precioManual: elegido.precioManual,
      modo: 'Desperdicio',
      factor: 0,
      precio: elegido.precio,
    }
    setGrupos(gruposActuales.map((grupo) => grupo.clave === grupoClave ? { ...grupo, lineas: [...grupo.lineas, linea] } : grupo))
  }

  function mover(grupoClave: number, indice: number, delta: number) {
    setGrupos(gruposActuales.map((grupo) => {
      if (grupo.clave !== grupoClave) {
        return grupo
      }
      const destino = indice + delta
      if (destino < 0 || destino >= grupo.lineas.length) {
        return grupo
      }
      const lineas = [...grupo.lineas]
      const [linea] = lineas.splice(indice, 1)
      lineas.splice(destino, 0, linea)
      return { ...grupo, lineas }
    }))
  }
}

function ayuda(linea: Linea, grupo: string) {
  if (linea.modo === 'PorcentajeManoObra') {
    return grupo === 'ManoObra' ? 'porcentaje sobre la mano de obra del grupo (0,10)' : ''
  }
  if (linea.modo === 'Rendimiento' && linea.cuadrillaId) {
    return `rendimiento (unidades por día). Factor guardado ${linea.factor.toLocaleString('es-CO', { maximumFractionDigits: 4 })}`
  }
  if (linea.modo === 'Rendimiento') {
    return 'el factor multiplica cantidad × precio'
  }
  if (linea.modo === 'Divisor') {
    return 'usos por unidad'
  }
  return '% desperdicio (0,05 = 5 %)'
}
