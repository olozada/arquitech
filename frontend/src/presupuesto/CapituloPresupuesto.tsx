import { Button, Table } from 'react-bootstrap'
import { cliente } from '../api/cliente'
import type { ItemPresupuesto, PresupuestoDetalle } from '../api/presupuesto'
import { dinero } from '../actas/formato'
import { totalCapitulo } from './costosPresupuesto'
import { FilaItem } from './FilaItem'

type Aviso = { itemId: number | null; tipo: string; mensaje: string }
type ApuOpcion = { id: number; codigo: string | null; descripcion: string }

export function CapituloPresupuesto({
  indiceCapitulo,
  borrador,
  editable,
  resaltado,
  filtroAvisos,
  avisos,
  apus,
  alActualizar,
  alCambiar,
  alAgregarItem,
  alBuscarApu,
  alCrearApu,
  alMedicion,
  alDuplicar,
  alMover,
  alConcepto,
  alEditarApu,
}: {
  indiceCapitulo: number
  borrador: PresupuestoDetalle
  editable: boolean
  resaltado: number | null
  filtroAvisos: boolean
  avisos: Aviso[]
  apus: ApuOpcion[]
  alActualizar: (presupuesto: PresupuestoDetalle) => void
  alCambiar: (item: number, cambios: Partial<ItemPresupuesto>) => void
  alAgregarItem: () => void
  alBuscarApu: () => void
  alCrearApu: () => void
  alMedicion: (item: number) => void
  alDuplicar: (itemId: number) => void
  alMover: (itemId: number, destino: number) => void
  alConcepto: (item: number) => void
  alEditarApu: (apuId: number) => void
}) {
  const capitulo = borrador.capitulos[indiceCapitulo]
  const candidatos = borrador.capitulos.flatMap((grupo) => grupo.items).filter((candidato) => candidato.id > 0)
  const destinoInicial = borrador.capitulos.find((candidato) => candidato.id !== capitulo.id && candidato.id > 0)?.id ?? 0

  return (
    <details className="catalogo-panel mb-3" open>
      <summary
        draggable={editable}
        onDragStart={(evento) => evento.dataTransfer.setData('text/plain', String(indiceCapitulo))}
        onDragOver={(evento) => evento.preventDefault()}
        onDrop={(evento) => {
          evento.preventDefault()
          const origen = Number(evento.dataTransfer.getData('text/plain'))
          if (!Number.isInteger(origen) || origen === indiceCapitulo) {
            return
          }
          const capitulos = [...borrador.capitulos]
          const [movido] = capitulos.splice(origen, 1)
          capitulos.splice(indiceCapitulo, 0, movido)
          void cliente.put<PresupuestoDetalle>(`/api/presupuestos/${borrador.id}/orden`, {
            rowVersion: borrador.rowVersion,
            capitulos: capitulos.filter((capituloOrden) => capituloOrden.id > 0).map((capituloOrden, indice) => ({
              id: capituloOrden.id,
              orden: indice + 1,
              items: capituloOrden.items.map((fila) => fila.id).filter((itemId) => itemId > 0),
            })),
          }).then(({ data }) => alActualizar(data))
        }}
      >
        <strong>
          {capitulo.numero}. {capitulo.nombre}
        </strong>{' '}
        <span className="text-secondary">{dinero(totalCapitulo(capitulo), 2)}</span>
      </summary>
      <Table responsive size="sm" className="presupuesto-hoja mt-2 mb-0">
        <thead>
          <tr>
            <th>Código</th>
            <th>Descripción</th>
            <th>Unidad</th>
            <th>Cantidad</th>
            <th>APU</th>
            <th>Factor</th>
            <th className="text-end">Material</th>
            <th className="text-end">MO</th>
            <th className="text-end">Total</th>
          </tr>
        </thead>
        <tbody>
          {capitulo.items.map((item, indiceItem) => (
              <FilaItem
                key={item.id || `${indiceCapitulo}-${indiceItem}`}
                item={item}
                editable={editable}
                esPlantilla={borrador.esPlantilla}
                resaltado={resaltado === item.id}
                oculto={filtroAvisos && !avisos.some((aviso) => aviso.itemId === item.id)}
                avisos={avisos.filter((aviso) => aviso.itemId === item.id)}
                apus={apus}
                candidatos={candidatos}
                destinoInicial={destinoInicial}
                alCambiar={(cambios) => alCambiar(indiceItem, cambios)}
                alMedicion={() => alMedicion(indiceItem)}
                alDuplicar={() => alDuplicar(item.id)}
                alMover={(destino) => alMover(item.id, destino)}
                alConcepto={() => alConcepto(indiceItem)}
                alEditarApu={() => alEditarApu(item.apuId ?? 0)}
              />
          ))}
        </tbody>
      </Table>
      {editable && !borrador.esPlantilla && (
        <div className="d-flex gap-2">
          <Button type="button" variant="link" onClick={alAgregarItem}>
            Ítem manual
          </Button>
          <Button type="button" variant="link" onClick={alBuscarApu}>
            Desde APU
          </Button>
          <Button type="button" variant="link" onClick={alCrearApu}>
            Crear APU
          </Button>
        </div>
      )}
    </details>
  )
}
