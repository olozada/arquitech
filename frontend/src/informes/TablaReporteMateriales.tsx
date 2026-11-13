import { Pagination, Table } from 'react-bootstrap'
import { dinero } from '../actas/formato'
import { TAMANO_PAGINA, type Campo, type Direccion, type ItemMaterial } from './reporteMateriales'

export function TablaMateriales({
  items,
  total,
  cantidad,
  tamano,
  detallado,
  pagina,
  campo,
  direccion,
  encabezado,
  alCambiarPagina,
  alOrdenar,
}: {
  items: ItemMaterial[]
  total: number
  cantidad: number
  tamano: number
  detallado: boolean
  pagina: number
  campo: Campo
  direccion: Direccion
  encabezado: string[]
  alCambiarPagina: (pagina: number) => void
  alOrdenar: (campo: Campo) => void
}) {
  const filas = items
  const tamanoPagina = tamano > 0 ? tamano : TAMANO_PAGINA
  const totalPaginas = Math.max(1, Math.ceil((cantidad || filas.length) / tamanoPagina))
  const paginaVisible = Math.min(pagina, totalPaginas)

  return (
    <div className="catalogo-panel">
      {encabezado.length > 0 && (
        <div className="reporte-encabezado">
          {encabezado.map((linea) => (
            <p key={linea}>{linea}</p>
          ))}
        </div>
      )}
      <div className="table-responsive">
        <Table hover className="catalogo-tabla reporte-items mb-0" style={{ width: detallado ? '62.5rem' : '58rem' }}>
          <colgroup>
            {detallado && <col className="reporte-col-acta" />}
            <col className="reporte-col-um" />
            <col className="reporte-col-material" />
            <col className="reporte-col-um" />
            <col className="reporte-col-cantidad" />
            <col className="reporte-col-precio" />
            <col className="reporte-col-estado" />
            <col className="reporte-col-subtotal" />
          </colgroup>
          <thead>
            <tr>
              {detallado && (
                <Encabezado campo="acta" activo={campo} direccion={direccion} alOrdenar={alOrdenar}>Acta</Encabezado>
              )}
              <Encabezado campo="codigo" activo={campo} direccion={direccion} alOrdenar={alOrdenar}>Código</Encabezado>
              <Encabezado campo="material" activo={campo} direccion={direccion} alOrdenar={alOrdenar}>Material</Encabezado>
              <Encabezado campo="unidad" activo={campo} direccion={direccion} alOrdenar={alOrdenar}>UM</Encabezado>
              <Encabezado campo="cantidad" activo={campo} direccion={direccion} alOrdenar={alOrdenar} numerico>Cantidad</Encabezado>
              <Encabezado campo="precio" activo={campo} direccion={direccion} alOrdenar={alOrdenar} numerico>Precio</Encabezado>
              <Encabezado campo="estado" activo={campo} direccion={direccion} alOrdenar={alOrdenar}>Estado</Encabezado>
              <Encabezado campo="subtotal" activo={campo} direccion={direccion} alOrdenar={alOrdenar} numerico>Subtotal</Encabezado>
            </tr>
          </thead>
          <tbody>
            {filas.map((item, indice) => (
              <tr key={`${item.numeroActa ?? ''}-${item.codigo}-${item.material}-${item.unidad}-${item.aprobado}-${indice}`}>
                {detallado && <td>{item.numeroActa}</td>}
                <td>{item.codigo}</td>
                <td title={item.material}>{item.material}</td>
                <td>{item.unidad}</td>
                <td className="reporte-num">{item.cantidad}</td>
                <td className="reporte-num">{dinero(item.precio)}</td>
                <td>{item.aprobado ? 'Aprobado' : 'Rechazado'}</td>
                <td className="reporte-num">{dinero(item.subtotal)}</td>
              </tr>
            ))}
            <tr className="reporte-total">
              <th colSpan={detallado ? 7 : 6}>TOTAL</th>
              <th className="reporte-total-valor">{dinero(total)}</th>
            </tr>
          </tbody>
        </Table>
      </div>
      {totalPaginas > 1 && (
        <div className="d-flex align-items-center gap-2 mt-3">
          <Pagination className="catalogo-paginacion mb-0">
            <Pagination.Prev disabled={paginaVisible === 1} onClick={() => alCambiarPagina(paginaVisible - 1)} />
            <Pagination.Next disabled={paginaVisible === totalPaginas} onClick={() => alCambiarPagina(paginaVisible + 1)} />
          </Pagination>
          <span>Página {paginaVisible} de {totalPaginas}</span>
        </div>
      )}
    </div>
  )
}

function Encabezado({
  campo,
  activo,
  direccion,
  alOrdenar,
  numerico,
  children,
}: {
  campo: Campo
  activo: Campo
  direccion: Direccion
  alOrdenar: (campo: Campo) => void
  numerico?: boolean
  children: string
}) {
  const seleccionado = activo === campo
  return (
    <th className={numerico ? 'reporte-num' : undefined} aria-sort={seleccionado ? (direccion === 'asc' ? 'ascending' : 'descending') : 'none'}>
      <button type="button" className="actas-orden" onClick={() => alOrdenar(campo)}>
        {children}
        <span aria-hidden="true">{seleccionado ? (direccion === 'asc' ? '▲' : '▼') : '↕'}</span>
      </button>
    </th>
  )
}
