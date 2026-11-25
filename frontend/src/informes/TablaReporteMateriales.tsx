import { Fragment } from 'react'
import { Pagination, Table } from 'react-bootstrap'
import { dinero } from '../actas/formato'
import { filasMateriales, TAMANO_PAGINA, type Campo, type Direccion, type ItemMaterial } from './reporteMateriales'

export function TablaMateriales({
  items,
  total,
  cantidad,
  tamano,
  detallado,
  columna = 'Material',
  subtotales,
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
  columna?: string
  subtotales: boolean
  pagina: number
  campo: Campo
  direccion: Direccion
  encabezado: string[]
  alCambiarPagina: (pagina: number) => void
  alOrdenar: (campo: Campo) => void
}) {
  const filasBase = items
  const porConstruccion = filasBase.some((item) => (item.construccion ?? '').trim().length > 0)
  const secciones = porConstruccion ? seccionesPorConstruccion(filasBase) : [{ nombre: '', items: filasBase }]
  const columnas = (detallado ? 1 : 0) + 7
  const tamanoPagina = tamano > 0 ? tamano : TAMANO_PAGINA
  const totalPaginas = Math.max(1, Math.ceil((cantidad || items.length) / tamanoPagina))
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
        <Table hover className="catalogo-tabla reporte-items reporte-ancho mb-0">
          <colgroup>
            {detallado && <col className="reporte-col-acta" />}
            <col className="reporte-col-codigo" />
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
              <Encabezado campo="material" activo={campo} direccion={direccion} alOrdenar={alOrdenar}>{columna}</Encabezado>
              <Encabezado campo="unidad" activo={campo} direccion={direccion} alOrdenar={alOrdenar}>UM</Encabezado>
              <Encabezado campo="cantidad" activo={campo} direccion={direccion} alOrdenar={alOrdenar} numerico>Cantidad</Encabezado>
              <Encabezado campo="precio" activo={campo} direccion={direccion} alOrdenar={alOrdenar} numerico>Precio</Encabezado>
              <Encabezado campo="estado" activo={campo} direccion={direccion} alOrdenar={alOrdenar}>Estado</Encabezado>
              <Encabezado campo="subtotal" activo={campo} direccion={direccion} alOrdenar={alOrdenar} numerico>Subtotal</Encabezado>
            </tr>
          </thead>
          <tbody>
            {secciones.map((seccion) => {
              const filas = filasMateriales(seccion.items, detallado && subtotales)
              const totalSeccion = seccion.items.reduce((suma, item) => suma + item.subtotal, 0)
              return (
                <Fragment key={seccion.nombre || 'sin-construccion'}>
                  {porConstruccion && (
                    <tr className="reporte-corte">
                      <th colSpan={columnas}>Construcción: {seccion.nombre}</th>
                    </tr>
                  )}
                  {filas.map((fila) => fila.tipo === 'linea' ? (
                    <tr key={`${seccion.nombre}-${fila.item.numeroActa ?? ''}-${fila.item.codigo}-${fila.item.material}-${fila.item.unidad}-${fila.item.aprobado}-${fila.indice}`}>
                      {detallado && <td>{fila.item.numeroActa}</td>}
                      <td>{fila.item.codigo}</td>
                      <td title={fila.item.material}>{fila.item.material}</td>
                      <td>{fila.item.unidad}</td>
                      <td className="reporte-num">{fila.item.cantidad}</td>
                      <td className="reporte-num">{dinero(fila.item.precio)}</td>
                      <td>{fila.item.aprobado ? 'Aprobado' : 'Rechazado'}</td>
                      <td className="reporte-num">{dinero(fila.item.subtotal)}</td>
                    </tr>
                  ) : (
                    <tr key={`${seccion.nombre}-subtotal-${fila.clave}`} className="reporte-corte">
                      {detallado && <th />}
                      <th />
                      <th title={`Subtotal ${fila.material}`}>Subtotal {fila.material}</th>
                      <th />
                      <th className="reporte-num">{fila.cantidad}</th>
                      <th />
                      <th />
                      <th className="reporte-total-valor">{dinero(fila.subtotal)}</th>
                    </tr>
                  ))}
                  {porConstruccion && (
                    <tr className="reporte-corte">
                      <th colSpan={columnas - 1}>Total {seccion.nombre}</th>
                      <th className="reporte-total-valor">{dinero(totalSeccion)}</th>
                    </tr>
                  )}
                </Fragment>
              )
            })}
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

function seccionesPorConstruccion(items: ItemMaterial[]) {
  const grupos = new Map<string, ItemMaterial[]>()
  const orden: string[] = []
  for (const item of items) {
    const nombre = item.construccion?.trim() || 'Sin construcción'
    const actual = grupos.get(nombre)
    if (actual) {
      actual.push(item)
    } else {
      grupos.set(nombre, [item])
      orden.push(nombre)
    }
  }

  return orden.map((nombre) => ({ nombre, items: grupos.get(nombre) ?? [] }))
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
