import { Fragment, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Pagination, Row, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeDeError, mensajeError } from '../api/cliente'
import { abrirInformeItems, descargarExcel } from '../api/archivos'
import type { CampoOrdenInforme } from '../api/archivos'
import { dinero, fecha, fechaConsulta, hoyIso, inicioDeMesIso } from '../actas/formato'
import type { RegistroCatalogo } from '../catalogos/definiciones'
import type { EstadoActa } from '../dashboard/filtroUrl'

type VistaInforme = 'detallado' | 'agrupado'

type ItemInforme = {
  id: number | null
  numeroActa: number | null
  construccion: string
  concepto: string
  material: string
  descripcion: string
  unidad: string
  cantidad: number
  precio: number
  subtotal: number
  aprobado: boolean
}

type Informe = {
  agrupado: boolean
  items: ItemInforme[]
  total: number
}

type Filtro = {
  usarFechas: boolean
  desde: string
  hasta: string
  construccionId: number
  contratistaId: number
  arquitectoId: number
  revisorId: number
  estado: EstadoActa
  vista: VistaInforme
  romperPorActa: boolean
}

const TAMANO_PAGINA = 20

type Direccion = 'asc' | 'desc'

export function ReporteActas() {
  const [usarFechas, setUsarFechas] = useState(false)
  const [desde, setDesde] = useState(inicioDeMesIso)
  const [hasta, setHasta] = useState(hoyIso)
  const [construccionId, setConstruccionId] = useState(0)
  const [contratistaId, setContratistaId] = useState(0)
  const [arquitectoId, setArquitectoId] = useState(0)
  const [revisorId, setRevisorId] = useState(0)
  const [estado, setEstado] = useState<EstadoActa>('')
  const [vista, setVista] = useState<VistaInforme>('detallado')
  const [romperPorActa, setRomperPorActa] = useState(false)
  const [campo, setCampo] = useState<CampoOrdenInforme>('subtotal')
  const [direccion, setDireccion] = useState<Direccion>('desc')
  const [pagina, setPagina] = useState(1)
  const [filtro, setFiltro] = useState<Filtro | null>(null)
  const [exportando, setExportando] = useState(false)
  const [imprimiendo, setImprimiendo] = useState(false)
  const informeDisponible = construccionId > 0 || contratistaId > 0 || arquitectoId > 0 || revisorId > 0

  const construcciones = useCatalogo('construcciones')
  const contratistas = useCatalogo('contratistas')
  const arquitectos = useCatalogo('arquitectos')
  const revisores = useCatalogo('revisores')
  const reporte = useQuery({
    queryKey: ['reporte-actas', filtro],
    enabled: filtro != null,
    queryFn: async () => {
      const { data } = await cliente.get<Informe>('/api/actas/items', {
        params: { ...parametros(filtro!), vista: filtro!.vista, romperPorActa: filtro!.romperPorActa || undefined },
      })
      return data
    },
  })

  function criterioActual() {
    return { usarFechas, desde, hasta, construccionId, contratistaId, arquitectoId, revisorId, estado, vista, romperPorActa }
  }

  async function consultar(evento: FormEvent) {
    evento.preventDefault()
    if (fechasInvalidas(usarFechas, desde, hasta)) {
      await avisar('La fecha final debe ser mayor o igual que la fecha inicial.')
      return
    }

    setPagina(1)
    setFiltro(criterioActual())
  }

  async function exportar() {
    if (fechasInvalidas(usarFechas, desde, hasta)) {
      await avisar('La fecha final debe ser mayor o igual que la fecha inicial.')
      return
    }

    setExportando(true)
    try {
      await descargarExcel(criterioActual())
    } catch (error) {
      await avisar(await mensajeDeError(error))
    } finally {
      setExportando(false)
    }
  }

  async function imprimir() {
    if (fechasInvalidas(usarFechas, desde, hasta)) {
      await avisar('La fecha final debe ser mayor o igual que la fecha inicial.')
      return
    }

    setImprimiendo(true)
    try {
      await abrirInformeItems(criterioActual(), vista, ordenImpresion(vista, campo, direccion, romperPorActa, construccionId > 0))
    } catch (error) {
      await avisar(await mensajeDeError(error))
    } finally {
      setImprimiendo(false)
    }
  }

  return (
    <section>
      <h1>Reporte de actas</h1>
      <p><Link to="/informes/materiales">Ver por materiales</Link></p>
      <Form className="catalogo-panel reporte-filtros" onSubmit={(evento) => void consultar(evento)}>
        <Row className="g-3 align-items-end">
          <Col xs={12} sm="auto">
            <Form.Check
              id="reporte-usar-fechas"
              className="reporte-filtro-check"
              type="checkbox"
              label="Filtrar por fecha"
              checked={usarFechas}
              onChange={(evento) => {
                const activo = evento.target.checked
                setUsarFechas(activo)
                if (activo && fechasInvalidas(true, desde, hasta)) return
                setPagina(1)
                setFiltro((actual) => (actual ? { ...actual, usarFechas: activo, desde, hasta } : actual))
              }}
            />
          </Col>
          <Col xs={12} sm="auto">
            <Form.Check
              id="reporte-romper-acta"
              className="reporte-filtro-check"
              type="checkbox"
              label="Agrupar"
              checked={romperPorActa}
              onChange={(evento) => {
                const activo = evento.target.checked
                setRomperPorActa(activo)
                setPagina(1)
                setFiltro((actual) => (actual ? { ...actual, romperPorActa: activo } : actual))
              }}
            />
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-fecha">
            <Form.Group controlId="reporte-desde">
              <Form.Label>Desde</Form.Label>
              <Form.Control
                type="date"
                value={desde}
                disabled={!usarFechas}
                onChange={(evento) => {
                  if (evento.currentTarget.disabled) return
                  setDesde(evento.currentTarget.value)
                }}
              />
            </Form.Group>
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-fecha">
            <Form.Group controlId="reporte-hasta">
              <Form.Label>Hasta</Form.Label>
              <Form.Control
                type="date"
                value={hasta}
                disabled={!usarFechas}
                onChange={(evento) => {
                  if (evento.currentTarget.disabled) return
                  setHasta(evento.currentTarget.value)
                }}
              />
            </Form.Group>
          </Col>
        </Row>
        <Row className="g-3 align-items-end mt-1">
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="reporte-construccion">
              <Form.Label>Construcción</Form.Label>
              <Selector valor={construccionId} opciones={construcciones.data ?? []} alCambiar={setConstruccionId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="reporte-arquitecto">
              <Form.Label>Arquitecto</Form.Label>
              <Selector valor={arquitectoId} opciones={arquitectos.data ?? []} alCambiar={setArquitectoId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="reporte-revisor">
              <Form.Label>Revisor</Form.Label>
              <Selector valor={revisorId} opciones={revisores.data ?? []} alCambiar={setRevisorId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="reporte-contratista">
              <Form.Label>Contratista</Form.Label>
              <Selector valor={contratistaId} opciones={contratistas.data ?? []} alCambiar={setContratistaId} />
            </Form.Group>
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-corto">
            <Form.Group controlId="reporte-vista">
              <Form.Label>Vista</Form.Label>
              <Form.Select
                value={vista}
                onChange={(evento) => {
                  const siguiente: VistaInforme = evento.target.value === 'agrupado' ? 'agrupado' : 'detallado'
                  setVista(siguiente)
                  setPagina(1)
                  setFiltro((actual) => (actual ? { ...actual, vista: siguiente } : actual))
                }}
              >
                <option value="detallado">Detallado</option>
                <option value="agrupado">Agrupado</option>
              </Form.Select>
            </Form.Group>
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-corto">
            <Form.Group controlId="reporte-estado">
              <Form.Label>Estado</Form.Label>
              <Form.Select
                value={estado}
                onChange={(evento) => setEstado(evento.target.value === 'abierta' || evento.target.value === 'cerrada' ? evento.target.value : '')}
              >
                <option value="">Todos</option>
                <option value="abierta">Abierta</option>
                <option value="cerrada">Cerrada</option>
              </Form.Select>
            </Form.Group>
          </Col>
        </Row>
        <div className="catalogo-acciones mt-3">
          <Button type="submit">Consultar</Button>
          <Button type="button" variant="outline-primary" disabled={exportando} onClick={() => void exportar()}>
            Exportar Excel
          </Button>
          {informeDisponible && (
            <Button type="button" variant="outline-primary" disabled={imprimiendo} onClick={() => void imprimir()}>
              {imprimiendo ? 'Imprimiendo…' : 'Imprimir'}
            </Button>
          )}
        </div>
      </Form>

      {reporte.isError && <p>{mensajeError(reporte.error)}</p>}
      {reporte.data && reporte.data.items.length === 0 && <p>No hay registros.</p>}
      {reporte.data && reporte.data.items.length > 0 && (
        <TablaItems
          informe={reporte.data}
          pagina={pagina}
          campo={campo}
          direccion={direccion}
          romper={filtro?.romperPorActa ?? false}
          ocultarConstruccion={(filtro?.construccionId ?? 0) > 0}
          encabezado={filtro ? lineasEncabezado(filtro, construcciones.data ?? [], arquitectos.data ?? [], revisores.data ?? [], contratistas.data ?? []) : []}
          alCambiarPagina={setPagina}
          alOrdenar={(siguiente) => {
            setPagina(1)
            if (campo === siguiente) {
              setDireccion((actual) => (actual === 'asc' ? 'desc' : 'asc'))
              return
            }

            setCampo(siguiente)
            setDireccion('asc')
          }}
        />
      )}
    </section>
  )
}

function TablaItems({
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
  const ordenados = useMemo(
    () => ordenarItems(informe.items, campoVisible(campo, informe.agrupado, romper, ocultarConstruccion), direccion, romper),
    [informe.items, informe.agrupado, campo, direccion, romper, ocultarConstruccion],
  )
  const totalPaginas = Math.max(1, Math.ceil(ordenados.length / TAMANO_PAGINA))
  const paginaVisible = Math.min(pagina, totalPaginas)
  const inicio = (paginaVisible - 1) * TAMANO_PAGINA
  const filas = ordenados.slice(inicio, inicio + TAMANO_PAGINA)
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
              const siguiente = ordenados[inicio + indice + 1]
              const cierraActa = romper && siguiente?.numeroActa !== item.numeroActa
              const sumaActa = cierraActa ? sumaPorActa(ordenados, item.numeroActa) : 0
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

function campoVisible(campo: CampoOrdenInforme, agrupado: boolean, romper: boolean, ocultarConstruccion = false) {
  if ((agrupado && !romper && campo === 'acta') || (ocultarConstruccion && campo === 'construccion')) {
    return null
  }

  return campo
}

function ordenImpresion(
  vista: VistaInforme,
  campo: CampoOrdenInforme,
  direccion: Direccion,
  romper: boolean,
  ocultarConstruccion: boolean,
) {
  return campoVisible(campo, vista === 'agrupado', romper, ocultarConstruccion) ? { campo, direccion } : undefined
}

function lineasEncabezado(
  filtro: Filtro,
  construcciones: RegistroCatalogo[],
  arquitectos: RegistroCatalogo[],
  revisores: RegistroCatalogo[],
  contratistas: RegistroCatalogo[],
) {
  const lineas: string[] = []
  const desde = fechaConsulta(filtro.usarFechas, filtro.desde)
  const hasta = fechaConsulta(filtro.usarFechas, filtro.hasta)
  if (desde && hasta) {
    lineas.push(`Del ${fecha(desde)} al ${fecha(hasta)}`)
  } else if (desde) {
    lineas.push(`Desde ${fecha(desde)}`)
  } else if (hasta) {
    lineas.push(`Hasta ${fecha(hasta)}`)
  }

  const texto = (id: number, opciones: RegistroCatalogo[], etiqueta: string) => {
    const opcion = opciones.find((item) => item.id === id)
    const nombre = opcion ? String(opcion.nombre ?? opcion.descripcion ?? '') : ''
    if (nombre) lineas.push(`${etiqueta}: ${nombre}`)
  }

  texto(filtro.construccionId, construcciones, 'Construcción')
  texto(filtro.arquitectoId, arquitectos, 'Arquitecto')
  texto(filtro.revisorId, revisores, 'Revisor')
  texto(filtro.contratistaId, contratistas, 'Contratista')
  if (filtro.estado === 'abierta') lineas.push('Estado: Abierta')
  if (filtro.estado === 'cerrada') lineas.push('Estado: Cerrada')
  return lineas
}

function sumaPorActa(items: ItemInforme[], numeroActa: number | null) {
  return items.reduce((suma, item) => (item.numeroActa === numeroActa ? suma + item.subtotal : suma), 0)
}

function ordenarItems(items: ItemInforme[], campo: CampoOrdenInforme | null, direccion: Direccion, romper = false) {
  if (!campo && !romper) {
    return items
  }

  const factor = direccion === 'asc' ? 1 : -1
  return [...items].sort((a, b) => {
    if (romper && campo !== 'acta') {
      const porActa = (b.numeroActa ?? 0) - (a.numeroActa ?? 0)
      if (porActa !== 0) return porActa
    }

    return campo ? factor * compararItem(a, b, campo) : 0
  })
}

function compararItem(a: ItemInforme, b: ItemInforme, campo: CampoOrdenInforme) {
  if (campo === 'acta') {
    return (a.numeroActa ?? 0) - (b.numeroActa ?? 0)
  }

  if (campo === 'cantidad' || campo === 'precio' || campo === 'subtotal') {
    return a[campo] - b[campo]
  }

  if (campo === 'estado') {
    return textoEstado(a).localeCompare(textoEstado(b), 'es')
  }

  return normalizar(a[campo]).localeCompare(normalizar(b[campo]), 'es')
}

function textoEstado(item: ItemInforme) {
  return item.aprobado ? 'aprobado' : 'rechazado'
}

function normalizar(valor: string) {
  return valor
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLocaleLowerCase('es')
}

function avisar(texto: string) {
  return Swal.fire({ icon: 'error', title: '¡Error!', text: texto, confirmButtonColor: '#1abb9c' })
}

function Selector({
  valor,
  opciones,
  alCambiar,
}: {
  valor: number
  opciones: RegistroCatalogo[]
  alCambiar: (id: number) => void
}) {
  return (
    <Form.Select value={valor} onChange={(evento) => alCambiar(Number(evento.target.value))}>
      <option value={0}>Todos</option>
      {opciones.map((opcion) => (
        <option key={opcion.id} value={opcion.id}>
          {String(opcion.nombre ?? opcion.descripcion ?? opcion.id)}
        </option>
      ))}
    </Form.Select>
  )
}

function useCatalogo(recurso: string) {
  return useQuery({
    queryKey: ['catalogo', recurso],
    queryFn: async () => (await cliente.get<RegistroCatalogo[]>(`/api/${recurso}`)).data,
  })
}

function fechasInvalidas(usarFechas: boolean, desde: string, hasta: string) {
  return usarFechas && Boolean(fechaConsulta(true, desde)) && Boolean(fechaConsulta(true, hasta)) && hasta < desde
}

function parametros(filtro: Filtro) {
  return {
    desde: fechaConsulta(filtro.usarFechas, filtro.desde),
    hasta: fechaConsulta(filtro.usarFechas, filtro.hasta),
    construccionId: filtro.construccionId > 0 ? filtro.construccionId : undefined,
    contratistaId: filtro.contratistaId > 0 ? filtro.contratistaId : undefined,
    arquitectoId: filtro.arquitectoId > 0 ? filtro.arquitectoId : undefined,
    revisorId: filtro.revisorId > 0 ? filtro.revisorId : undefined,
    estado: filtro.estado || undefined,
  }
}
