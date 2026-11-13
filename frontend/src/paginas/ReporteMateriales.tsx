import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Pagination, Row, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeDeError, mensajeError } from '../api/cliente'
import { dinero, fecha, fechaConsulta, hoyIso, inicioDeMesIso } from '../actas/formato'
import type { RegistroCatalogo } from '../catalogos/definiciones'
import type { EstadoActa } from '../dashboard/filtroUrl'

type VistaInforme = 'detallado' | 'agrupado'

type ItemMaterial = {
  numeroActa: number | null
  codigo: string
  material: string
  unidad: string
  cantidad: number
  precio: number
  subtotal: number
  aprobado: boolean
}

type InformeMateriales = {
  agrupado: boolean
  items: ItemMaterial[]
  total: number
  cantidad: number
  pagina: number
  tamano: number
}

type Filtro = {
  usarFechas: boolean
  desde: string
  hasta: string
  construccionId: number
  contratistaId: number
  arquitectoId: number
  revisorId: number
  proveedorId: number
  estado: EstadoActa
  vista: VistaInforme
}

type Campo = 'acta' | 'codigo' | 'material' | 'unidad' | 'cantidad' | 'precio' | 'estado' | 'subtotal'
type Direccion = 'asc' | 'desc'

const TAMANO_PAGINA = 20

export function ReporteMateriales() {
  const [usarFechas, setUsarFechas] = useState(false)
  const [desde, setDesde] = useState(inicioDeMesIso)
  const [hasta, setHasta] = useState(hoyIso)
  const [construccionId, setConstruccionId] = useState(0)
  const [contratistaId, setContratistaId] = useState(0)
  const [arquitectoId, setArquitectoId] = useState(0)
  const [revisorId, setRevisorId] = useState(0)
  const [proveedorId, setProveedorId] = useState(0)
  const [estado, setEstado] = useState<EstadoActa>('')
  const [vista, setVista] = useState<VistaInforme>('agrupado')
  const [campo, setCampo] = useState<Campo>('subtotal')
  const [direccion, setDireccion] = useState<Direccion>('desc')
  const [pagina, setPagina] = useState(1)
  const [filtro, setFiltro] = useState<Filtro | null>(null)
  const [exportando, setExportando] = useState(false)
  const [imprimiendo, setImprimiendo] = useState(false)
  const informeDisponible = construccionId > 0 || contratistaId > 0 || arquitectoId > 0 || revisorId > 0 || proveedorId > 0

  const construcciones = useCatalogo('construcciones')
  const contratistas = useCatalogo('contratistas')
  const arquitectos = useCatalogo('arquitectos')
  const revisores = useCatalogo('revisores')
  const proveedores = useCatalogo('proveedores')
  const reporte = useQuery({
    queryKey: ['reporte-materiales', filtro, pagina, campo, direccion],
    enabled: filtro != null,
    queryFn: async () => {
      const { data } = await cliente.get<InformeMateriales>('/api/actas/materiales', {
        params: { ...parametros(filtro!), orden: campo, direccion, pagina, tamano: TAMANO_PAGINA },
      })
      return data
    },
  })

  function criterioActual(): Filtro {
    return { usarFechas, desde, hasta, construccionId, contratistaId, arquitectoId, revisorId, proveedorId, estado, vista }
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
      const { data } = await cliente.get<Blob>('/api/actas/materiales/excel', {
        responseType: 'blob',
        params: parametros(criterioActual()),
      })
      const url = URL.createObjectURL(data)
      const enlace = document.createElement('a')
      enlace.href = url
      enlace.download = 'ReporteMateriales.xlsx'
      enlace.click()
      URL.revokeObjectURL(url)
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
      const { data } = await cliente.get<Blob>('/api/actas/materiales/informe', {
        responseType: 'blob',
        params: { ...parametros(criterioActual()), orden: campo, direccion },
      })
      const url = URL.createObjectURL(new Blob([data], { type: 'application/pdf' }))
      window.open(url, '_blank', 'noopener')
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
    } catch (error) {
      await avisar(await mensajeDeError(error))
    } finally {
      setImprimiendo(false)
    }
  }

  const encabezado = filtro
    ? lineasEncabezado(filtro, construcciones.data ?? [], arquitectos.data ?? [], revisores.data ?? [], contratistas.data ?? [], proveedores.data ?? [])
    : []

  return (
    <section>
      <h1>Reporte por materiales</h1>
      <p><Link to="/informes/actas">Ver por actas</Link></p>
      <Form className="catalogo-panel reporte-filtros" onSubmit={(evento) => void consultar(evento)}>
        <Row className="g-3 align-items-end">
          <Col xs={12} sm="auto">
            <Form.Check
              id="materiales-usar-fechas"
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
          <Col xs={6} sm="auto" className="reporte-campo-fecha">
            <Form.Group controlId="materiales-desde">
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
            <Form.Group controlId="materiales-hasta">
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
            <Form.Group controlId="materiales-construccion">
              <Form.Label>Construcción</Form.Label>
              <Selector valor={construccionId} opciones={construcciones.data ?? []} alCambiar={setConstruccionId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="materiales-arquitecto">
              <Form.Label>Arquitecto</Form.Label>
              <Selector valor={arquitectoId} opciones={arquitectos.data ?? []} alCambiar={setArquitectoId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="materiales-revisor">
              <Form.Label>Revisor</Form.Label>
              <Selector valor={revisorId} opciones={revisores.data ?? []} alCambiar={setRevisorId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="materiales-contratista">
              <Form.Label>Contratista</Form.Label>
              <Selector valor={contratistaId} opciones={contratistas.data ?? []} alCambiar={setContratistaId} />
            </Form.Group>
          </Col>
          <Col xs={12} sm={6} lg>
            <Form.Group controlId="materiales-proveedor">
              <Form.Label>Proveedor</Form.Label>
              <Selector valor={proveedorId} opciones={proveedores.data ?? []} alCambiar={setProveedorId} />
            </Form.Group>
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-corto">
            <Form.Group controlId="materiales-vista">
              <Form.Label>Vista</Form.Label>
              <Form.Select
                value={vista}
                onChange={(evento) => {
                  const siguiente: VistaInforme = evento.target.value === 'detallado' ? 'detallado' : 'agrupado'
                  setVista(siguiente)
                  setPagina(1)
                  if (siguiente === 'agrupado') {
                    setCampo((actual) => (actual === 'acta' ? 'subtotal' : actual))
                  }
                  setFiltro((actual) => (actual ? { ...actual, vista: siguiente } : actual))
                }}
              >
                <option value="detallado">Detallado</option>
                <option value="agrupado">Agrupado</option>
              </Form.Select>
            </Form.Group>
          </Col>
          <Col xs={6} sm="auto" className="reporte-campo-corto">
            <Form.Group controlId="materiales-estado">
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
        <TablaMateriales
          items={reporte.data.items}
          total={reporte.data.total}
          cantidad={reporte.data.cantidad}
          tamano={reporte.data.tamano}
          detallado={!reporte.data.agrupado}
          pagina={pagina}
          campo={campo}
          direccion={direccion}
          encabezado={encabezado}
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

function TablaMateriales({
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

function lineasEncabezado(
  filtro: Filtro,
  construcciones: RegistroCatalogo[],
  arquitectos: RegistroCatalogo[],
  revisores: RegistroCatalogo[],
  contratistas: RegistroCatalogo[],
  proveedores: RegistroCatalogo[],
) {
  const lineas: string[] = []
  const desde = fechaConsulta(filtro.usarFechas, filtro.desde)
  const hasta = fechaConsulta(filtro.usarFechas, filtro.hasta)
  if (desde && hasta) lineas.push(`Del ${fecha(desde)} al ${fecha(hasta)}`)
  else if (desde) lineas.push(`Desde ${fecha(desde)}`)
  else if (hasta) lineas.push(`Hasta ${fecha(hasta)}`)

  const texto = (id: number, opciones: RegistroCatalogo[], etiqueta: string) => {
    const opcion = opciones.find((item) => item.id === id)
    const nombre = opcion ? String(opcion.nombre ?? opcion.descripcion ?? '') : ''
    if (nombre) lineas.push(`${etiqueta}: ${nombre}`)
  }

  texto(filtro.construccionId, construcciones, 'Construcción')
  texto(filtro.arquitectoId, arquitectos, 'Arquitecto')
  texto(filtro.revisorId, revisores, 'Revisor')
  texto(filtro.contratistaId, contratistas, 'Contratista')
  texto(filtro.proveedorId, proveedores, 'Proveedor')
  if (filtro.estado === 'abierta') lineas.push('Estado: Abierta')
  if (filtro.estado === 'cerrada') lineas.push('Estado: Cerrada')
  return lineas
}

function Selector({ valor, opciones, alCambiar }: { valor: number; opciones: RegistroCatalogo[]; alCambiar: (id: number) => void }) {
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
    proveedorId: filtro.proveedorId > 0 ? filtro.proveedorId : undefined,
    estado: filtro.estado || undefined,
    vista: filtro.vista,
  }
}

function avisar(texto: string) {
  return Swal.fire({ icon: 'error', title: '¡Error!', text: texto, confirmButtonColor: '#1abb9c' })
}
