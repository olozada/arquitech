import { useEffect, useState } from 'react'
import axios from 'axios'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Col, Form, Modal, Row, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { HistorialAuditoria } from '../componentes/HistorialAuditoria'
import { useSesion } from '../auth/contextoSesion'
import { esAdministrador, esRevisor, perfilArquitecto, perfilDeNuevoItem } from '../auth/perfiles'
import { abrirPdf } from '../api/archivos'
import { dinero, numero, redondear } from '../actas/formato'
import type { RegistroCatalogo } from '../catalogos/definiciones'

type LineaBorrador = {
  clave: string
  id: number | null
  tipoUsuarioId: number
  tipoUsuario: string
  construccionId: number
  construccion: string
  conceptoId: number
  concepto: string
  unidadMedidaId: number | null
  unidadMedida: string
  insumoId: number | null
  insumo: string | null
  cantidad: number
  precio: number
  descripcion: string
  aprobado: boolean
  presupuestoItemId: number | null
}

type Borrador = {
  fechaInicial: string
  fechaFinal: string
  arquitectoId: number
  revisorId: number
  contratistaId: number
  observacion: string
  detalles: LineaBorrador[]
}

const CLAVE = 'arquitectos.borradorActa'
const mensajeFechas = 'La fecha final debe ser mayor o igual que la fecha inicial.'

function imprimir(numero: number) {
  const ventana = prepararVisor()
  void abrirPdf(numero, ventana).catch(async (error) => {
    ventana?.close()
    await avisar(mensajeError(error))
  })
}

function prepararVisor() {
  const ventana = window.open('', '_blank')
  if (!ventana) {
    return null
  }

  ventana.document.write(
    '<!DOCTYPE html><title>Acta</title><p style="font-family:sans-serif;padding:24px">Generando el acta…</p>',
  )
  ventana.document.close()
  return ventana
}

export function descartarBorradorActa() {
  localStorage.removeItem(CLAVE)
}

type ActaGuardada = {
  numeroActa: number
  fechaInicial: string
  fechaFinal: string
  arquitectoId: number
  revisorId: number
  contratistaId: number
  observacion: string | null
  detalles: Array<{
    id: number
    construccionId: number
    construccion: string
    conceptoId: number
    concepto: string
    unidadMedidaId: number | null
    unidadMedida: string
    insumoId: number | null
    insumo: string | null
    cantidad: number
    precio: number
    descripcion: string
    tipoUsuarioId: number
    tipoUsuario: string
    aprobado: boolean
    presupuestoItemId?: number | null
  }>
  cerrada: boolean
  rowVersion: string
}

export function TomaActas() {
  const { sesion } = useSesion()
  const administrador = esAdministrador(sesion?.tipoUsuario)
  const revisor = esRevisor(sesion?.tipoUsuario)
  const ubicacion = useLocation()
  const parametros = useParams()
  const navegar = useNavigate()
  const consultas = useQueryClient()
  const numeroRuta = Number(parametros.numero)
  const editando = Number.isInteger(numeroRuta) && numeroRuta > 0
  const marcaNueva = marcaDeActaNueva(ubicacion.state)
  const inicial = useState(() => (editando || marcaNueva ? borradorVacio() : leerBorrador()))[0]
  const [marcaAplicada, setMarcaAplicada] = useState(marcaNueva)
  const [fechaInicial, setFechaInicial] = useState(inicial.fechaInicial)
  const [fechaFinal, setFechaFinal] = useState(inicial.fechaFinal)
  const [arquitectoId, setArquitectoId] = useState(inicial.arquitectoId)
  const [revisorId, setRevisorId] = useState(inicial.revisorId)
  const [contratistaId, setContratistaId] = useState(inicial.contratistaId)
  const [observacion, setObservacion] = useState(inicial.observacion)
  const [detalles, setDetalles] = useState(inicial.detalles)
  const [numeroActa, setNumeroActa] = useState<number | null>(null)
  const [modalAbierto, setModalAbierto] = useState(false)
  const [lineaClave, setLineaClave] = useState<string | null>(null)
  const [linea, setLinea] = useState(lineaVacia)
  const [guardando, setGuardando] = useState(false)
  const [cerrada, setCerrada] = useState(false)
  const [version, setVersion] = useState('')
  const [conflicto, setConflicto] = useState(false)
  const [hidratada, setHidratada] = useState(!editando)
  const [pestana, setPestana] = useState<'acta' | 'historial'>('acta')

  const arquitectos = useCatalogo('arquitectos')
  const revisores = useCatalogo('revisores')
  const contratistas = useCatalogo('contratistas')
  const construcciones = useCatalogo('construcciones')
  const conceptos = useCatalogo('conceptos')
  const unidades = useCatalogo('unidades-medida')
  const insumos = useQuery({
    queryKey: ['insumos-acta'],
    queryFn: async () => {
      const { data } = await cliente.get<Array<{ id: number; codigo: string | null; descripcion: string; activo: boolean }>>('/api/insumos')
      return data
        .filter((insumo) => insumo.activo)
        .map((insumo) => ({
          id: insumo.id,
          descripcion: insumo.codigo ? `${insumo.codigo} ${insumo.descripcion}` : insumo.descripcion,
          activo: true,
        }))
    },
  })
  const obraLinea = Number(linea.construccionId)
  const itemsPresupuesto = useQuery({
    queryKey: ['presupuesto-items-acta', obraLinea],
    enabled: modalAbierto && obraLinea > 0,
    queryFn: async () =>
      (await cliente.get<ItemPresupuestoActa[]>(`/api/presupuestos/items-acta?construccionId=${obraLinea}`)).data,
  })
  const actaConsulta = useQuery({
    queryKey: ['acta', numeroRuta],
    enabled: editando,
    staleTime: Infinity,
    queryFn: async () => (await cliente.get<ActaGuardada>(`/api/actas/${numeroRuta}`)).data,
  })

  useEffect(() => {
    if (!editando || hidratada || !actaConsulta.data) {
      return
    }

    aplicarActa(actaConsulta.data)
    setHidratada(true)
  }, [editando, hidratada, actaConsulta.data])

  useEffect(() => {
    if (marcaNueva === marcaAplicada) {
      return
    }

    setMarcaAplicada(marcaNueva)
    limpiarFormulario()
    setNumeroActa(null)
    setModalAbierto(false)
    setLineaClave(null)
    setLinea(lineaVacia())
    descartarBorradorActa()
  }, [marcaNueva, marcaAplicada])

  useEffect(() => {
    if (editando || marcaNueva !== marcaAplicada) {
      return
    }

    const borrador: Borrador = {
      fechaInicial,
      fechaFinal,
      arquitectoId,
      revisorId,
      contratistaId,
      observacion,
      detalles,
    }
    localStorage.setItem(CLAVE, JSON.stringify(borrador))
  }, [editando, marcaNueva, marcaAplicada, fechaInicial, fechaFinal, arquitectoId, revisorId, contratistaId, observacion, detalles])

  const hayRechazo = detalles.some((item) => !item.aprobado)
  const todosRechazados = detalles.length > 0 && detalles.every((item) => !item.aprobado)
  const total = detalles.reduce((suma, item) => suma + (item.aprobado ? item.cantidad * item.precio : 0), 0)

  function abrirNuevaLinea() {
    setLineaClave(null)
    setLinea(lineaVacia())
    setModalAbierto(true)
  }

  function abrirLinea(item: LineaBorrador) {
    setLineaClave(item.clave)
    setLinea({
      construccionId: String(item.construccionId),
      conceptoId: String(item.conceptoId),
      unidadMedidaId: item.unidadMedidaId ? String(item.unidadMedidaId) : '',
      insumoId: item.insumoId ? String(item.insumoId) : '',
      cantidad: String(item.cantidad),
      precio: String(item.precio),
      descripcion: item.descripcion,
      presupuestoItemId: item.presupuestoItemId ? String(item.presupuestoItemId) : '',
    })
    setModalAbierto(true)
  }

  function guardarLinea() {
    const resultadoLinea = construirLinea(linea, construcciones.data, conceptos.data, unidades.data, insumos.data)
    if (!resultadoLinea.ok) {
      void avisar(resultadoLinea.error)
      return
    }

    const construida = resultadoLinea.linea
    const tipo = perfilDeNuevoItem(sesion?.tipoUsuario)

    setNumeroActa(null)
    setDetalles((actual) =>
      lineaClave
        ? actual.map((item) =>
            item.clave === lineaClave
              ? { ...construida, clave: lineaClave, id: item.id, tipoUsuarioId: item.tipoUsuarioId, tipoUsuario: item.tipoUsuario, aprobado: item.aprobado }
              : item,
          )
        : [...actual, { ...construida, clave: crypto.randomUUID(), id: null, tipoUsuarioId: tipo.id, tipoUsuario: tipo.nombre }],
    )
    setModalAbierto(false)
  }

  function revisarLinea(clave: string, aprobado: boolean) {
    setNumeroActa(null)
    setDetalles((actual) => actual.map((item) => (item.clave === clave ? { ...item, aprobado } : item)))
  }

  function eliminarLinea() {
    if (!lineaClave) {
      return
    }

    setNumeroActa(null)
    setDetalles((actual) => actual.filter((item) => item.clave !== lineaClave))
    setModalAbierto(false)
  }

  async function registrar() {
    const error = validarEncabezado(fechaInicial, fechaFinal, arquitectoId, revisorId, contratistaId)
    if (error) {
      await avisar(error)
      return
    }

    const confirmacion = await Swal.fire({
      icon: 'question',
      title: editando ? '¿Desea guardar los cambios del acta?' : '¿Desea registrar el acta?',
      showCancelButton: true,
      confirmButtonText: editando ? 'Guardar' : 'Registrar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#1abb9c',
    })
    if (!confirmacion.isConfirmed) {
      return
    }

    setGuardando(true)
    const cuerpo = {
      fechaInicial,
      fechaFinal,
      arquitectoId,
      revisorId,
      contratistaId,
      observacion: observacion.trim() || null,
      rowVersion: version || null,
      detalles: detalles.map((item) => ({
        id: item.id,
        tipoUsuarioId: item.tipoUsuarioId,
        construccionId: item.construccionId,
        conceptoId: item.conceptoId,
        unidadMedidaId: item.unidadMedidaId,
        insumoId: item.insumoId,
        cantidad: item.cantidad,
        precio: item.precio,
        descripcion: item.descripcion,
        aprobado: item.aprobado,
        presupuestoItemId: item.presupuestoItemId,
      })),
    }
    try {
      if (editando) {
        const { data } = await cliente.put<ActaGuardada>(`/api/actas/${numeroRuta}`, cuerpo)
        aplicarActa(data)
        consultas.setQueryData(['acta', numeroRuta], data)
        await consultas.invalidateQueries({ queryKey: ['actas'] })
        await Swal.fire({
          icon: 'success',
          title: '¡Finalizado!',
          text: `Acta N° ${data.numeroActa}`,
          confirmButtonColor: '#1abb9c',
        })
        navegar('/actas')
        return
      }

      const { data } = await cliente.post<{ numeroActa: number }>('/api/actas', cuerpo)
      limpiarFormulario()
      setNumeroActa(data.numeroActa)
      localStorage.removeItem(CLAVE)
      await consultas.invalidateQueries({ queryKey: ['actas'] })
      await Swal.fire({
        icon: 'success',
        title: '¡Finalizado!',
        text: `Acta N° ${data.numeroActa}`,
        confirmButtonColor: '#1abb9c',
      })
      navegar('/actas')
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 409) {
        setConflicto(true)
        return
      }

      await avisar(mensajeError(error))
    } finally {
      setGuardando(false)
    }
  }

  async function cancelar() {
    const confirmacion = await Swal.fire({
      icon: 'warning',
      title: '¿Desea cancelar el acta?',
      showCancelButton: true,
      confirmButtonText: 'Sí, cancelar',
      cancelButtonText: 'Volver',
      confirmButtonColor: '#d9534f',
    })
    if (!confirmacion.isConfirmed) {
      return
    }

    if (!editando) {
      limpiarFormulario()
      setNumeroActa(null)
      localStorage.removeItem(CLAVE)
    }

    navegar('/actas')
  }

  async function cambiarCierre(cerrar: boolean) {
    const confirmacion = await Swal.fire({
      icon: 'question',
      title: cerrar ? '¿Desea cerrar el acta?' : '¿Desea abrir el acta?',
      text: cerrar
        ? 'Al cerrarla no se podrán agregar, modificar ni eliminar ítems.'
        : 'Al abrirla los perfiles podrán modificar el acta de nuevo.',
      showCancelButton: true,
      confirmButtonText: cerrar ? 'Cerrar' : 'Abrir',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#1abb9c',
    })
    if (!confirmacion.isConfirmed) {
      return
    }

    setGuardando(true)
    try {
      const { data } = await cliente.put<ActaGuardada>(`/api/actas/${numeroRuta}/cierre`, {
        cerrada: cerrar,
        rowVersion: version || null,
      })
      aplicarActa(data)
      consultas.setQueryData(['acta', numeroRuta], data)
      await consultas.invalidateQueries({ queryKey: ['actas'] })
      await Swal.fire({
        icon: 'success',
        title: cerrar ? 'Acta cerrada' : 'Acta abierta',
        text: `Acta N° ${data.numeroActa}`,
        confirmButtonColor: '#1abb9c',
      })
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 409) {
        setConflicto(true)
        return
      }

      await avisar(mensajeError(error))
    } finally {
      setGuardando(false)
    }
  }

  async function recargarActa() {
    try {
      const { data } = await cliente.get<ActaGuardada>(`/api/actas/${numeroRuta}`)
      aplicarActa(data)
      consultas.setQueryData(['acta', numeroRuta], data)
      setConflicto(false)
    } catch (error) {
      await avisar(mensajeError(error))
    }
  }

  function aplicarActa(acta: ActaGuardada) {
    setFechaInicial(acta.fechaInicial.slice(0, 10))
    setFechaFinal(acta.fechaFinal.slice(0, 10))
    setArquitectoId(acta.arquitectoId)
    setRevisorId(acta.revisorId)
    setContratistaId(acta.contratistaId)
    setObservacion(acta.observacion ?? '')
    setDetalles(acta.detalles.map(lineaDesdeServidor))
    setNumeroActa(acta.numeroActa)
    setCerrada(acta.cerrada)
    setVersion(acta.rowVersion)
    setConflicto(false)
  }

  function limpiarFormulario() {
    setFechaInicial('')
    setFechaFinal('')
    setArquitectoId(0)
    setRevisorId(0)
    setContratistaId(0)
    setObservacion('')
    setDetalles([])
    setCerrada(false)
    setVersion('')
    setConflicto(false)
  }

  if (editando && actaConsulta.isError) {
    return (
      <section>
        <p className="mb-2">
          <Link to="/actas">Volver a actas</Link>
        </p>
        <p>{mensajeError(actaConsulta.error)}</p>
      </section>
    )
  }

  if (editando && !hidratada) {
    return (
      <section>
        <p>Cargando acta…</p>
      </section>
    )
  }

  return (
    <section>
      <p className="mb-2">
        <Link to="/actas">Volver a actas</Link>
      </p>
      <h1>{editando ? `Acta N° ${numeroRuta}` : 'Toma de actas – Perfil Arquitecto'}</h1>
      {conflicto && (
        <p className="alert alert-warning" role="status">
          Otra persona modificó esta acta
          <Button type="button" size="sm" className="ms-2" variant="outline-dark" onClick={() => void recargarActa()}>
            Recargar
          </Button>
        </p>
      )}
      {cerrada && (
        <p className="alert alert-warning" role="status">
          El acta ya está cerrada. No se pueden agregar, modificar ni eliminar ítems.
        </p>
      )}
      {editando && administrador && (
        <div className="nav nav-tabs mb-3">
          <button type="button" className={pestana === 'acta' ? 'nav-link active' : 'nav-link'} onClick={() => setPestana('acta')}>
            Acta
          </button>
          <button type="button" className={pestana === 'historial' ? 'nav-link active' : 'nav-link'} onClick={() => setPestana('historial')}>
            Historial
          </button>
        </div>
      )}
      {pestana === 'historial' && editando ? (
        <HistorialAuditoria entidad="Acta" entidadId={String(numeroRuta)} />
      ) : (
      <>
      <div className="catalogo-panel">
        <Row className="g-3">
          <Col md={6}>
            <Form.Group controlId="acta-fecha-inicial">
              <Form.Label>Fecha inicial</Form.Label>
              <Form.Control type="date" value={fechaInicial} disabled={cerrada || (editando && !administrador)} onChange={(evento) => setFechaInicial(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={6}>
            <Form.Group controlId="acta-fecha-final">
              <Form.Label>Fecha final</Form.Label>
              <Form.Control type="date" value={fechaFinal} disabled={cerrada || (editando && !administrador)} onChange={(evento) => setFechaFinal(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={4}>
            <Selector id="acta-arquitecto" etiqueta="Arquitecto" valor={arquitectoId} opciones={arquitectos.data} campo="nombre" deshabilitado={cerrada || (editando && !administrador)} alCambiar={setArquitectoId} />
          </Col>
          <Col md={4}>
            <Selector id="acta-revisor" etiqueta="Revisor" valor={revisorId} opciones={revisores.data} campo="nombre" deshabilitado={cerrada || (editando && !administrador)} alCambiar={setRevisorId} />
          </Col>
          <Col md={4}>
            <Selector id="acta-contratista" etiqueta="Contratista" valor={contratistaId} opciones={contratistas.data} campo="nombre" deshabilitado={cerrada || (editando && !administrador)} alCambiar={setContratistaId} />
          </Col>
        </Row>
        {!revisor && !cerrada && (
          <Button type="button" className="mt-3" onClick={abrirNuevaLinea}>
            Agregar ítems
          </Button>
        )}
      </div>

      <div className="catalogo-panel">
        {detalles.length === 0 ? (
          <p>No hay ítems.</p>
        ) : (
          <div className="table-responsive">
            <Table hover className="catalogo-tabla mb-0">
              <thead>
                <tr>
                  <th>{revisor ? 'Revisión' : 'Seleccionar'}</th>
                  <th>Perfil</th>
                  <th>Estado</th>
                  <th>Construcción</th>
                  <th>Concepto</th>
                  <th>Insumo</th>
                  <th>UM</th>
                  <th>Cantidad</th>
                  <th>Precio</th>
                  <th>Descripción</th>
                  <th>Subtotal</th>
                </tr>
              </thead>
              <tbody>
                {detalles.map((item) => (
                  <tr key={item.clave}>
                    <td>
                      {cerrada ? null : revisor ? (
                        <div className="d-flex gap-1">
                          <Button type="button" size="sm" variant={item.aprobado ? 'success' : 'outline-success'} onClick={() => revisarLinea(item.clave, true)}>
                            Aprobar
                          </Button>
                          <Button type="button" size="sm" variant={!item.aprobado ? 'danger' : 'outline-danger'} onClick={() => revisarLinea(item.clave, false)}>
                            Rechazar
                          </Button>
                        </div>
                      ) : (
                        (administrador || item.tipoUsuarioId === perfilDeNuevoItem(sesion?.tipoUsuario).id) && (
                          <Button type="button" size="sm" variant="outline-primary" onClick={() => abrirLinea(item)}>
                            Seleccionar
                          </Button>
                        )
                      )}
                    </td>
                    <td>{item.tipoUsuario}</td>
                    <td>{item.aprobado ? 'Aprobado' : 'Rechazado'}</td>
                    <td>{item.construccion}</td>
                    <td>{item.concepto}</td>
                    <td>{item.insumo ?? ''}</td>
                    <td>{item.unidadMedida}</td>
                    <td>{item.cantidad}</td>
                    <td>{dinero(item.precio, 2)}</td>
                    <td>{item.descripcion}</td>
                    <td>{dinero(item.aprobado ? item.cantidad * item.precio : 0, 2)}</td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </div>
        )}
        <p className="total-acta">Total: {dinero(total, 2)}</p>
        {todosRechazados && <p>Todos los ítems están rechazados. El valor del acta es 0.</p>}
        {hayRechazo && !todosRechazados && <p>Los ítems rechazados suman 0.</p>}
      </div>

      <div className="catalogo-panel">
        <Form.Group controlId="acta-comentario">
          <Form.Label>Comentario</Form.Label>
          <Form.Control as="textarea" rows={3} maxLength={1000} value={observacion} disabled={cerrada || (editando && !administrador)} onChange={(evento) => setObservacion(evento.target.value)} />
        </Form.Group>
        <div className="catalogo-acciones mt-3">
          {!cerrada && (
            <Button type="button" disabled={guardando} onClick={() => void registrar()}>
              {editando ? 'Guardar' : 'Registrar'}
            </Button>
          )}
          {editando && administrador && (
            <Button type="button" variant={cerrada ? 'outline-primary' : 'outline-danger'} disabled={guardando} onClick={() => void cambiarCierre(!cerrada)}>
              {cerrada ? 'Abrir acta' : 'Cerrar acta'}
            </Button>
          )}
          <Button type="button" variant="outline-secondary" onClick={() => void cancelar()}>
            Cancelar
          </Button>
          {(editando || numeroActa != null) && (
            <Button type="button" variant="outline-primary" onClick={() => imprimir(numeroActa ?? numeroRuta)}>
              Imprimir
            </Button>
          )}
        </div>
      </div>

      <Modal show={modalAbierto} onHide={() => setModalAbierto(false)} centered>
        <Modal.Header closeButton>
          <Modal.Title>{lineaClave ? 'Modificar ítem' : 'Agregar ítem'}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <Selector id="linea-construccion" etiqueta="Construcción" valor={Number(linea.construccionId)} opciones={construcciones.data} campo="descripcion" alCambiar={(id) => setLinea({ ...linea, construccionId: id ? String(id) : '', presupuestoItemId: '' })} />
          {(itemsPresupuesto.data?.length ?? 0) > 0 && (
            <Form.Group className="mb-3" controlId="linea-presupuesto">
              <Form.Label>Ítem presupuesto</Form.Label>
              <Form.Select
                value={linea.presupuestoItemId}
                onChange={(evento) => {
                  const id = Number(evento.target.value)
                  const item = itemsPresupuesto.data?.find((fila) => fila.id === id)
                  if (!item) {
                    setLinea({ ...linea, presupuestoItemId: '' })
                    return
                  }
                  setLinea({
                    ...linea,
                    presupuestoItemId: String(item.id),
                    conceptoId: item.conceptoId ? String(item.conceptoId) : linea.conceptoId,
                    unidadMedidaId: item.unidadMedidaId ? String(item.unidadMedidaId) : linea.unidadMedidaId,
                    descripcion: item.descripcion,
                    precio: item.precio == null ? linea.precio : String(item.precio),
                  })
                }}
              >
                <option value="">Sin ligar</option>
                {itemsPresupuesto.data?.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.codigo} · {item.descripcion}
                  </option>
                ))}
              </Form.Select>
            </Form.Group>
          )}
          <Selector id="linea-concepto" etiqueta="Concepto" valor={Number(linea.conceptoId)} opciones={conceptos.data} campo="descripcion" alCambiar={(id) => setLinea({ ...linea, conceptoId: id ? String(id) : '' })} />
          <Selector id="linea-um" etiqueta="Unidad de medida" valor={Number(linea.unidadMedidaId)} opciones={unidades.data} campo="nombre" alCambiar={(id) => setLinea({ ...linea, unidadMedidaId: id ? String(id) : '' })} />
          <Selector id="linea-material" etiqueta="Insumo" valor={Number(linea.insumoId)} opciones={insumos.data} campo="descripcion" alCambiar={(id) => setLinea({ ...linea, insumoId: id ? String(id) : '' })} />
          <Form.Group className="mb-3" controlId="linea-precio">
            <Form.Label>Precio</Form.Label>
            <Form.Control value={linea.precio} inputMode="decimal" onChange={(evento) => setLinea({ ...linea, precio: evento.target.value })} />
          </Form.Group>
          <Form.Group className="mb-3" controlId="linea-cantidad">
            <Form.Label>Cantidad</Form.Label>
            <Form.Control value={linea.cantidad} inputMode="decimal" onChange={(evento) => setLinea({ ...linea, cantidad: evento.target.value })} />
          </Form.Group>
          <Form.Group controlId="linea-descripcion">
            <Form.Label>Descripción</Form.Label>
            <Form.Control value={linea.descripcion} maxLength={500} onChange={(evento) => setLinea({ ...linea, descripcion: evento.target.value })} />
          </Form.Group>
        </Modal.Body>
        <Modal.Footer>
          {lineaClave == null ? (
            <Button type="button" onClick={guardarLinea}>
              Agregar
            </Button>
          ) : (
            <>
              <Button type="button" onClick={guardarLinea}>
                Modificar
              </Button>
              <Button type="button" variant="danger" onClick={eliminarLinea}>
                Eliminar
              </Button>
            </>
          )}
          <Button type="button" variant="outline-secondary" onClick={() => setModalAbierto(false)}>
            Cancelar
          </Button>
        </Modal.Footer>
      </Modal>
      </>
      )}
    </section>
  )
}

type LineaFormulario = {
  construccionId: string
  conceptoId: string
  unidadMedidaId: string
  insumoId: string
  cantidad: string
  precio: string
  descripcion: string
  presupuestoItemId: string
}

function lineaVacia(): LineaFormulario {
  return {
    construccionId: '',
    conceptoId: '',
    unidadMedidaId: '',
    insumoId: '',
    cantidad: '',
    precio: '',
    descripcion: '',
    presupuestoItemId: '',
  }
}

function useCatalogo(recurso: string) {
  return useQuery({
    queryKey: ['catalogo', recurso],
    queryFn: async () => (await cliente.get<RegistroCatalogo[]>(`/api/${recurso}`)).data,
  })
}

function Selector({
  id,
  etiqueta,
  valor,
  opciones,
  campo,
  deshabilitado = false,
  alCambiar,
}: {
  id: string
  etiqueta: string
  valor: number
  opciones: RegistroCatalogo[] | undefined
  campo: 'nombre' | 'descripcion'
  deshabilitado?: boolean
  alCambiar: (id: number) => void
}) {
  return (
    <Form.Group className="mb-3" controlId={id}>
      <Form.Label>{etiqueta}</Form.Label>
      <Form.Select value={valor || ''} disabled={deshabilitado} onChange={(evento) => alCambiar(Number(evento.target.value))}>
        <option value="">Seleccione</option>
        {(opciones ?? []).map((opcion) => (
          <option key={opcion.id} value={opcion.id}>
            {String(opcion[campo] ?? '')}
          </option>
        ))}
      </Form.Select>
    </Form.Group>
  )
}

function construirLinea(
  formulario: LineaFormulario,
  construcciones: RegistroCatalogo[] | undefined,
  conceptos: RegistroCatalogo[] | undefined,
  unidades: RegistroCatalogo[] | undefined,
  insumos: RegistroCatalogo[] | undefined,
): { ok: true; linea: Omit<LineaBorrador, 'clave'> } | { ok: false; error: string } {
  const construccion = buscar(construcciones, Number(formulario.construccionId))
  const concepto = buscar(conceptos, Number(formulario.conceptoId))
  const unidad = buscar(unidades, Number(formulario.unidadMedidaId))
  const insumo = formulario.insumoId ? buscar(insumos, Number(formulario.insumoId)) : null
  const cantidad = numero(formulario.cantidad)
  const precio = numero(formulario.precio)
  const descripcion = formulario.descripcion.trim()
  const faltantes: string[] = []
  if (!construccion) {
    faltantes.push('la construcción')
  }
  if (!concepto) {
    faltantes.push('el concepto')
  }
  if (formulario.unidadMedidaId && !unidad) {
    faltantes.push('la unidad de medida')
  }
  if (formulario.insumoId && !insumo) {
    faltantes.push('el insumo')
  }
  if (cantidad == null || cantidad <= 0) {
    faltantes.push('la cantidad')
  }
  if (precio == null || precio < 0) {
    faltantes.push('el precio')
  }
  if (faltantes.length > 0 || !construccion || !concepto || cantidad == null || precio == null) {
    return { ok: false, error: `Debe indicar ${enumerar(faltantes)}.` }
  }

  return {
    ok: true,
    linea: {
        id: null,
        tipoUsuarioId: perfilArquitecto,
        tipoUsuario: 'Arquitecto',
        construccionId: construccion.id,
      construccion: String(construccion.descripcion ?? ''),
      conceptoId: concepto.id,
      concepto: String(concepto.descripcion ?? ''),
      unidadMedidaId: unidad?.id ?? null,
      unidadMedida: unidad ? String(unidad.nombre ?? '') : '',
      insumoId: insumo?.id ?? null,
      insumo: insumo ? String(insumo.descripcion ?? '') : null,
      cantidad: redondear(cantidad, 4),
      precio: redondear(precio, 2),
      descripcion,
      aprobado: true,
      presupuestoItemId: Number(formulario.presupuestoItemId) > 0 ? Number(formulario.presupuestoItemId) : null,
    },
  }
}

function buscar(opciones: RegistroCatalogo[] | undefined, id: number) {
  return (opciones ?? []).find((opcion) => opcion.id === id)
}

function validarEncabezado(
  fechaInicial: string,
  fechaFinal: string,
  arquitectoId: number,
  revisorId: number,
  contratistaId: number,
) {
  const faltantes: string[] = []
  if (!fechaInicial) {
    faltantes.push('la fecha inicial')
  }
  if (!fechaFinal) {
    faltantes.push('la fecha final')
  }
  if (arquitectoId <= 0) {
    faltantes.push('el arquitecto')
  }
  if (revisorId <= 0) {
    faltantes.push('el revisor')
  }
  if (contratistaId <= 0) {
    faltantes.push('el contratista')
  }
  if (faltantes.length > 0) {
    return `Debe seleccionar ${enumerar(faltantes)}.`
  }

  if (fechaFinal < fechaInicial) {
    return mensajeFechas
  }

  return null
}

function enumerar(partes: string[]) {
  if (partes.length === 1) {
    return partes[0]
  }

  return `${partes.slice(0, -1).join(', ')} y ${partes[partes.length - 1]}`
}

function lineaDesdeServidor(item: ActaGuardada['detalles'][number]): LineaBorrador {
  return {
    clave: `srv-${item.id}`,
    id: item.id,
    tipoUsuarioId: item.tipoUsuarioId,
    tipoUsuario: item.tipoUsuario,
    construccionId: item.construccionId,
    construccion: item.construccion,
    conceptoId: item.conceptoId,
    concepto: item.concepto,
    unidadMedidaId: item.unidadMedidaId,
    unidadMedida: item.unidadMedida,
    insumoId: item.insumoId,
    insumo: item.insumo,
    cantidad: item.cantidad,
    precio: item.precio,
    descripcion: item.descripcion,
    aprobado: item.aprobado !== false,
    presupuestoItemId: item.presupuestoItemId ?? null,
  }
}

type ItemPresupuestoActa = {
  id: number
  codigo: string
  descripcion: string
  unidad: string
  conceptoId: number | null
  unidadMedidaId: number | null
  precio: number | null
}

function borradorVacio(): Borrador {
  return {
    fechaInicial: '',
    fechaFinal: '',
    arquitectoId: 0,
    revisorId: 0,
    contratistaId: 0,
    observacion: '',
    detalles: [],
  }
}

function marcaDeActaNueva(estado: unknown) {
  if (!estado || typeof estado !== 'object' || !('nueva' in estado)) {
    return 0
  }

  const marca = (estado as { nueva?: unknown }).nueva
  return typeof marca === 'number' ? marca : 0
}

function leerBorrador(): Borrador {
  const vacio = borradorVacio()
  try {
    const texto = localStorage.getItem(CLAVE)
    if (!texto) {
      return vacio
    }

    const valor = JSON.parse(texto) as Partial<Borrador>
    const detalles = Array.isArray(valor.detalles)
      ? valor.detalles.map((item) => ({
          ...item,
          id: typeof item.id === 'number' ? item.id : null,
          tipoUsuarioId: typeof item.tipoUsuarioId === 'number' ? item.tipoUsuarioId : perfilArquitecto,
          tipoUsuario: typeof item.tipoUsuario === 'string' ? item.tipoUsuario : 'Arquitecto',
          aprobado: item.aprobado !== false,
          presupuestoItemId: typeof item.presupuestoItemId === 'number' ? item.presupuestoItemId : null,
        }))
      : []
    return { ...vacio, ...valor, detalles }
  } catch {
    return vacio
  }
}

function avisar(texto: string) {
  return Swal.fire({ icon: 'error', title: '¡Error!', text: texto, confirmButtonColor: '#1abb9c' })
}
