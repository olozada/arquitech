import { Button, Form } from 'react-bootstrap'
import type { ItemPresupuesto } from '../api/presupuesto'
import { dinero, redondear } from '../actas/formato'

type Aviso = { itemId: number | null; tipo: string; mensaje: string }
type ApuOpcion = { id: number; codigo: string | null; descripcion: string }
type Candidato = { id: number; codigo: string }

export function FilaItem({
  item,
  editable,
  esPlantilla,
  resaltado,
  oculto = false,
  avisos,
  apus,
  candidatos,
  destinoInicial,
  alCambiar,
  alMedicion,
  alDuplicar,
  alMover,
  alConcepto,
  alEditarApu,
}: {
  item: ItemPresupuesto
  editable: boolean
  esPlantilla: boolean
  resaltado: boolean
  oculto?: boolean
  avisos: Aviso[]
  apus: ApuOpcion[]
  candidatos: Candidato[]
  destinoInicial: number
  alCambiar: (cambios: Partial<ItemPresupuesto>) => void
  alMedicion: () => void
  alDuplicar: () => void
  alMover: (destino: number) => void
  alConcepto: () => void
  alEditarApu: () => void
}) {
  return (
    <tr
      id={item.id >= 0 ? `item-${item.id}` : undefined}
      className={resaltado ? 'table-warning' : undefined}
      hidden={oculto}
    >
      <td>
        <Form.Control size="sm" value={item.codigo} disabled={!editable} onChange={(evento) => alCambiar({ codigo: evento.target.value })} />
      </td>
      <td>
        <Form.Control size="sm" value={item.descripcion} disabled={!editable} onChange={(evento) => alCambiar({ descripcion: evento.target.value })} />
        {item.avisoUnidad && <small className="text-danger">La unidad no coincide con el APU. Indique un factor de conversión.</small>}
      </td>
      <td>
        <Form.Control size="sm" value={item.unidad} disabled={!editable} onChange={(evento) => alCambiar({ unidad: evento.target.value })} />
      </td>
      <td>
        <Form.Control size="sm" value={item.cantidad} disabled={!editable || item.mediciones.length > 0} onChange={(evento) => alCambiar({ cantidad: Number(evento.target.value) || 0 })} />
        {editable && !esPlantilla && (
          <>
            <Button type="button" variant="link" size="sm" className="p-0" onClick={alMedicion}>
              Mediciones
            </Button>
            <Form.Select
              size="sm"
              value={item.cantidadOrigenItemId ?? ''}
              onChange={(evento) => alCambiar({ cantidadOrigenItemId: Number(evento.target.value) || null })}
            >
              <option value="">Digitar</option>
              {candidatos.filter((candidato) => candidato.id !== item.id).map((candidato) => (
                <option key={candidato.id} value={candidato.id}>
                  = {candidato.codigo} × {item.cantidadOrigenFactor}
                </option>
              ))}
            </Form.Select>
          </>
        )}
      </td>
      <td>
        <Form.Select size="sm" value={item.apuId ?? ''} disabled={!editable} onChange={(evento) => alCambiar({ apuId: Number(evento.target.value) || null, desdeApu: Number(evento.target.value) > 0 })}>
          <option value="">Valor directo</option>
          {apus.map((apu) => (
            <option key={apu.id} value={apu.id}>
              {(apu.codigo ? `${apu.codigo} ` : '') + apu.descripcion}
            </option>
          ))}
        </Form.Select>
      </td>
      <td>
        <Form.Control size="sm" value={item.factorConversionApu} disabled={!editable} onChange={(evento) => alCambiar({ factorConversionApu: Number(evento.target.value) || 1 })} />
      </td>
      <td className="text-end">
        {editable && !item.desdeApu ? (
          <Form.Control size="sm" className="text-end" value={redondear(item.valorMaterialUnitario, 2)} onChange={(evento) => alCambiar({ valorMaterialUnitario: Number(evento.target.value) || 0 })} />
        ) : (
          dinero(item.valorMaterialUnitario, 2)
        )}
      </td>
      <td className="text-end">
        {editable && !item.desdeApu ? (
          <Form.Control size="sm" className="text-end" value={redondear(item.valorManoObraUnitario, 2)} onChange={(evento) => alCambiar({ valorManoObraUnitario: Number(evento.target.value) || 0 })} />
        ) : (
          dinero(item.valorManoObraUnitario, 2)
        )}
      </td>
      <td className="text-end">{dinero(item.cantidad * (item.valorMaterialUnitario + item.valorManoObraUnitario), 2)}</td>
      <td>
        {editable && item.id >= 0 && (
          <>
            <Button type="button" size="sm" variant="link" onClick={alDuplicar}>
              Duplicar
            </Button>
            <Button type="button" size="sm" variant="link" onClick={() => alMover(destinoInicial)}>
              Mover
            </Button>
            <Button type="button" size="sm" variant="link" onClick={alConcepto}>
              Concepto
            </Button>
            {item.apuId != null && item.apuId >= 0 && (
              <Button type="button" size="sm" variant="link" onClick={alEditarApu}>
                Editar APU
              </Button>
            )}
          </>
        )}
        {avisos.map((aviso) => (
          <div key={aviso.tipo} className="text-danger small" title={aviso.mensaje}>
            {aviso.mensaje}
          </div>
        ))}
      </td>
    </tr>
  )
}
