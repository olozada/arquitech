import { Link } from 'react-router-dom'
import { Button, Form, Table } from 'react-bootstrap'
import { redondear } from '../actas/formato'
import { ayuda, dinero, gruposPosibles, modos } from './modeloApu'
import { SelectorOrigenLinea } from './SelectorOrigenLinea'
import type { ModeloEditorApu } from './useEditorApu'

export function VistaEditorApu(modelo: ModeloEditorApu) {
  const {
    embebido, descripcion, alCancelar, apuId, duplicar, guardar, calculo, consulta,
    codigo, setCodigo, setDescripcion, unidadMedidaId, setUnidadMedidaId, unidades,
    conceptoId, setConceptoId, conceptos, divisorGeneral, setDivisorGeneral,
    esSubanalisis, setEsSubanalisis, activo, setActivo, gruposActuales, setGrupos,
    quitarGrupo, actualizarLinea, mover, setOrigen, agregarGrupo, usadoEnAprobado,
    usos, origen, agregarLinea,
  } = modelo
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
}
