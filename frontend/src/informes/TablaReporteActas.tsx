import { Fragment } from 'react'
import { Pagination, Table } from 'react-bootstrap'
import type { CampoOrdenInforme } from '../api/archivos'
import { dinero } from '../actas/formato'
import { campoVisible, TAMANO_PAGINA, type Direccion, type Informe } from './reporteActas'

export function TablaItems({
  informe,
  pagina,
  campo,
  direccion,
  romper,
  ocultarConstruccion,
  encabezado,
  alCambiarPagina,
  alOrdenar,
}: {
  informe: Informe
  pagina: number
  campo: CampoOrdenInforme
  direccion: Direccion
  romper: boolean
  ocultarConstruccion: boolean
  encabezado: string[]
  alCambiarPagina: (pagina: number) => void
  alOrdenar: (campo: CampoOrdenInforme) => void
}) {
  const mostrarActa = !informe.agrupado || romper
  const mostrarConstruccion = !ocultarConstruccion
  const filas = informe.items
  const tamano = informe.tamano > 0 ? informe.tamano : TAMANO_PAGINA
  const totalPaginas = Math.max(1, Math.ceil((informe.cantidad || filas.length) / tamano))
  const paginaVisible = Math.min(pagina, totalPaginas)
  const columnasAntesDelTotal = (mostrarActa ? 1 : 0) + (mostrarConstruccion ? 1 : 0) + 6
  const activo = campoVisible(campo, informe.agrupado, romper, ocultarConstruccion)
  const ancho = (mostrarActa ? 4.5 : 0) + (mostrarConstruccion ? 14 : 0) + 62

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
        <Table hover className="catalogo-tabla reporte-items mb-0" style={{ width: `${ancho}rem` }}>
          <colgroup>
            {mostrarActa && <col className="reporte-col-acta" />}
            {mostrarConstruccion && <col className="reporte-col-construccion" />}
            <col className="reporte-col-construccion" />
            <col className="reporte-col-concepto" />
            <col className="reporte-col-material" />
            <col className="reporte-col-um" />
            <col className="reporte-col-cantidad" />
            <col className="reporte-col-precio" />
            <col className="reporte-col-estado" />
            <col className="reporte-col-subtotal" />
          </colgroup>
          <thead>
            <tr>
              {mostrarActa && (
                <Encabezado campo="acta" activo={activo} direccion={direccion} alOrdenar={alOrdenar}>
                  Acta
                </Encabezado>
              )}
              {mostrarConstruccion && (
                <Encabezado campo="construccion" activo={activo} direccion={direccion} alOrdenar={alOrdenar}>
                  Construcción
                </Encabezado>
              )}
              <Encabezado campo="concepto" activo={activo} direccion={direccion} alOrdenar={alOrdenar}>
                Concepto
              </Encabezado>
              <Encabezado campo="material" activo={activo} direccion={direccion} alOrdenar={alOrdenar}>
                Material
              </Encabezado>
              <Encabezado campo="unidad" activo={activo} direccion={direccion} alOrdenar={alOrdenar}>
                UM
              </Encabezado>
              <Encabezado campo="cantidad" activo={activo} direccion={direccion} alOrdenar={alOrdenar} numerico>
                Cantidad
              </Encabezado>
              <Encabezado campo="precio" activo={activo} direccion={direccion} alOrdenar={alOrdenar} numerico>
                Precio
              </Encabezado>
              <Encabezado campo="estado" activo={activo} direccion={direccion} alOrdenar={alOrdenar}>
                Estado
              </Encabezado>
              <Encabezado campo="subtotal" activo={activo} direccion={direccion} alOrdenar={alOrdenar} numerico>
                Subtotal
              </Encabezado>
            </tr>
          </thead>
          <tbody>
            {filas.map((item, indice) => {
              const cierraActa = romper && item.totalActa != null
              const sumaActa = item.totalActa ?? 0
              return (
                <Fragment key={item.id ?? `${item.numeroActa}-${item.construccion}-${item.concepto}-${item.precio}-${indice}`}>
                  <tr>
                    {mostrarActa && <td>{item.numeroActa}</td>}
                    {mostrarConstruccion && <td title={item.construccion}>{item.construccion}</td>}
                    <td title={item.concepto}>{item.concepto}</td>
                    <td title={item.material}>{item.material}</td>
                    <td>{item.unidad}</td>
                    <td className="reporte-num">{item.cantidad}</td>
                    <td className="reporte-num">{dinero(item.precio)}</td>
                    <td>{item.aprobado ? 'Aprobado' : 'Rechazado'}</td>
                    <td className="reporte-num">{dinero(item.subtotal)}</td>
                  </tr>
                  {cierraActa && (
                    <tr className="reporte-corte">
                      <th colSpan={columnasAntesDelTotal}>Total acta {item.numeroActa}</th>
                      <th className="reporte-total-valor">{dinero(sumaActa)}</th>
                    </tr>
                  )}
                </Fragment>
              )
            })}
            <tr className="reporte-total">
              <th colSpan={columnasAntesDelTotal}>TOTAL</th>
              <th className="reporte-total-valor">{dinero(informe.total)}</th>
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
  campo: CampoOrdenInforme
  activo: CampoOrdenInforme | null
  direccion: Direccion
  alOrdenar: (campo: CampoOrdenInforme) => void
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
