import { useMemo } from 'react'
import { Dropdown, Form } from 'react-bootstrap'
import { AlertTriangle, MoreHorizontal, Ruler } from 'lucide-react'
import type { ItemPresupuesto } from '../api/presupuesto'
import { dinero } from '../actas/formato'
import { CeldaNumero } from './CeldaNumero'
import { ComboApu, type ApuOpcion } from './ComboApu'
import { resolverUnidad } from './unidades'

type Aviso = { itemId: number | null; tipo: string; mensaje: string }

export function FilaItem({
  item,
  editable,
  esPlantilla,
  resaltado,
  oculto = false,
  avisos,
  apus,
  destinoInicial,
  mostrarFactor,
  cantidadDe,
  alCambiar,
  alMedicion,
  alDuplicar,
  alMover,
  alConcepto,
  alEditarApu,
  unidades,
}: {
  item: ItemPresupuesto
  editable: boolean
  esPlantilla: boolean
  resaltado: boolean
  oculto?: boolean
  avisos: Aviso[]
  apus: ApuOpcion[]
  destinoInicial: number
  mostrarFactor: boolean
  cantidadDe: (codigo: string) => number | undefined
  alCambiar: (cambios: Partial<ItemPresupuesto>) => void
  alMedicion: () => void
  alDuplicar: () => void
  alMover: (destino: number) => void
  alConcepto: () => void
  alEditarApu: () => void
  unidades: Array<{ id: number; nombre: string }>
}) {
  const soloLecturaCantidad = item.mediciones.length > 0
  const chipMediciones = item.mediciones.length > 0
  const chipFormula = Boolean(item.formulaCantidad?.trim() || item.cantidadOrigenItemId != null)

  const idUnidad = useMemo(() => {
    if (item.unidadMedidaId != null && item.unidadMedidaId > 0) {
      return item.unidadMedidaId
    }
    if (!item.unidad.trim() || unidades.length === 0) {
      return null
    }
    return resolverUnidad(item.unidad, unidades)?.id ?? null
  }, [item.unidad, item.unidadMedidaId, unidades])

  const factorVisible = useMemo(() => {
    if (!mostrarFactor) return false
    if (item.factorConversionApu !== 1) return true
    if (item.avisoUnidad) return true
    if (item.unidad && item.unidadApu && item.unidad !== item.unidadApu) return true
    return false
  }, [item.avisoUnidad, item.factorConversionApu, item.unidad, item.unidadApu, mostrarFactor])

  function elegirApu(apu: ApuOpcion | null) {
    if (!apu) {
      alCambiar({ apuId: null, desdeApu: false })
      return
    }
    const cambios: Partial<ItemPresupuesto> = {
      apuId: apu.id,
      desdeApu: true,
    }
    if (!item.descripcion.trim()) {
      cambios.descripcion = apu.descripcion
    }
    if (item.unidadMedidaId == null) {
      if (apu.unidadMedidaId) {
        cambios.unidadMedidaId = apu.unidadMedidaId
        cambios.unidad = apu.unidad
      } else {
        const coincidencia = unidades.find((fila) => fila.nombre.toUpperCase() === apu.unidad.toUpperCase())
        if (coincidencia) {
          cambios.unidadMedidaId = coincidencia.id
          cambios.unidad = coincidencia.nombre
        }
      }
    }
    alCambiar(cambios)
  }

  return (
    <tr
      id={item.id >= 0 ? `item-${item.id}` : undefined}
      className={resaltado ? 'table-warning' : undefined}
      hidden={oculto}
    >
      <td className="presupuesto-col-codigo">
        <Form.Control size="sm" value={item.codigo} disabled={!editable} onChange={(evento) => alCambiar({ codigo: evento.target.value })} />
      </td>
      <td className="presupuesto-col-descripcion">
        <Form.Control
          size="sm"
          title={item.descripcion}
          value={item.descripcion}
          disabled={!editable}
          onChange={(evento) => alCambiar({ descripcion: evento.target.value })}
        />
      </td>
      <td>
        {editable ? (
          <Form.Select
            size="sm"
            value={idUnidad ?? ''}
            onChange={(evento) => {
              const id = Number(evento.target.value)
              const nombre = unidades.find((fila) => fila.id === id)?.nombre ?? ''
              alCambiar({ unidadMedidaId: id > 0 ? id : null, unidad: nombre })
            }}
          >
            <option value="">Elegir…</option>
            {unidades.map((fila) => (
              <option key={fila.id} value={fila.id}>
                {fila.nombre}
              </option>
            ))}
          </Form.Select>
        ) : (
          <span>{item.unidad}</span>
        )}
      </td>
      <td className="presupuesto-col-cantidad">
        <div className="d-flex align-items-center gap-1">
          <div className="flex-grow-1">
            <CeldaNumero
              col="cantidad"
              valor={item.cantidad}
              formula={item.formulaCantidad}
              disabled={!editable || soloLecturaCantidad}
              cantidadDe={cantidadDe}
              alCambiar={(valor, formula) =>
                alCambiar({
                  cantidad: valor,
                  formulaCantidad: formula,
                  cantidadOrigenItemId: formula ? null : item.cantidadOrigenItemId,
                })
              }
            />
          </div>
          {editable && !esPlantilla && (
            <button type="button" className="btn btn-sm btn-link p-0 presupuesto-icono" title="Mediciones" onClick={alMedicion}>
              <Ruler size={16} aria-hidden />
            </button>
          )}
          {chipMediciones && <span className="badge text-bg-secondary">Σ {item.mediciones.length}</span>}
          {chipFormula && <span className="badge text-bg-light border">=</span>}
        </div>
      </td>
      <td className="presupuesto-col-apu">
        {editable ? (
          <ComboApu valor={item.apuId ?? null} apus={apus} disabled={!editable} alElegir={elegirApu} />
        ) : (
          <span>{item.apu ?? 'Valor directo'}</span>
        )}
      </td>
      {mostrarFactor && (
        <td>
          {factorVisible ? (
            <CeldaNumero
              col="factor"
              valor={item.factorConversionApu}
              decimales={4}
              disabled={!editable}
              alCambiar={(valor) => alCambiar({ factorConversionApu: valor || 1 })}
            />
          ) : null}
        </td>
      )}
      <td className="text-end">
        {editable && !item.desdeApu ? (
          <CeldaNumero
            col="material"
            valor={item.valorMaterialUnitario}
            decimales={2}
            disabled={!editable}
            alCambiar={(valor) => alCambiar({ valorMaterialUnitario: valor })}
          />
        ) : (
          dinero(item.valorMaterialUnitario, 2)
        )}
      </td>
      <td className="text-end">
        {editable && !item.desdeApu ? (
          <CeldaNumero
            col="mo"
            valor={item.valorManoObraUnitario}
            decimales={2}
            disabled={!editable}
            alCambiar={(valor) => alCambiar({ valorManoObraUnitario: valor })}
          />
        ) : (
          dinero(item.valorManoObraUnitario, 2)
        )}
      </td>
      <td className="text-end">{dinero(item.cantidad * (item.valorMaterialUnitario + item.valorManoObraUnitario), 2)}</td>
      <td className="presupuesto-col-acciones text-nowrap">
        {avisos.length > 0 && (
          <span className="presupuesto-icono text-warning me-1" title={avisos.map((a) => a.mensaje).join('\n')}>
            <AlertTriangle size={16} aria-hidden />
          </span>
        )}
        {editable && item.id >= 0 && (
          <Dropdown align="end">
            <Dropdown.Toggle variant="link" size="sm" className="presupuesto-icono p-0 border-0 shadow-none">
              <MoreHorizontal size={18} aria-hidden />
            </Dropdown.Toggle>
            <Dropdown.Menu>
              <Dropdown.Item onClick={alDuplicar}>Duplicar</Dropdown.Item>
              <Dropdown.Item onClick={() => alMover(destinoInicial)}>Mover</Dropdown.Item>
              <Dropdown.Item onClick={alConcepto}>Concepto</Dropdown.Item>
              {item.apuId != null && item.apuId >= 0 && <Dropdown.Item onClick={alEditarApu}>Editar APU</Dropdown.Item>}
            </Dropdown.Menu>
          </Dropdown>
        )}
      </td>
    </tr>
  )
}