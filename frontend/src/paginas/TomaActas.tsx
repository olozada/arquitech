import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Modal, Row, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import { abrirPdf } from '../api/archivos'
import { dinero, numero, redondear } from '../actas/formato'
import type { RegistroCatalogo } from '../catalogos/definiciones'

type LineaBorrador = {
  clave: string
  construccionId: number
  construccion: string
  conceptoId: number
  concepto: string
  unidadMedidaId: number
  unidadMedida: string
  materialId: number | null
  material: string | null
  cantidad: number
  precio: number
  descripcion: string
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
const mensajeEncabezado = 'Debe seleccionar toda la información.'
const mensajeDetalle = 'Debe ingresar toda la información.'
const mensajeFechas = 'La fecha final debe ser mayor o igual que la fecha inicial.'

export function TomaActas() {
  const inicial = leerBorrador()
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

  const arquitectos = useCatalogo('arquitectos')
  const revisores = useCatalogo('revisores')
  const contratistas = useCatalogo('contratistas')
  const construcciones = useCatalogo('construcciones')
  const conceptos = useCatalogo('conceptos')
  const unidades = useCatalogo('unidades-medida')
  const materiales = useCatalogo('materiales')

  useEffect(() => {
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
  }, [fechaInicial, fechaFinal, arquitectoId, revisorId, contratistaId, observacion, detalles])

  const total = detalles.reduce((suma, item) => suma + item.cantidad * item.precio, 0)

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
      unidadMedidaId: String(item.unidadMedidaId),
      materialId: item.materialId ? String(item.materialId) : '',
      cantidad: String(item.cantidad),
      precio: String(item.precio),
      descripcion: item.descripcion,
    })
    setModalAbierto(true)
  }

  function guardarLinea() {
    const construida = construirLinea(linea, construcciones.data, conceptos.data, unidades.data, materiales.data)
    if (!construida) {
      void avisar(mensajeDetalle)
      return
    }

    setNumeroActa(null)
    setDetalles((actual) =>
      lineaClave
        ? actual.map((item) => (item.clave === lineaClave ? { ...construida, clave: lineaClave } : item))
        : [...actual, { ...construida, clave: crypto.randomUUID() }],
    )
    setModalAbierto(false)
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
    const error = validarEncabezado(fechaInicial, fechaFinal, arquitectoId, revisorId, contratistaId, detalles.length)
    if (error) {
      await avisar(error)
      return
    }

    const confirmacion = await Swal.fire({
      icon: 'question',
      title: '¿Desea registrar el acta?',
      showCancelButton: true,
      confirmButtonText: 'Registrar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#1abb9c',
    })
    if (!confirmacion.isConfirmed) {
      return
    }

    const ventana = window.open('about:blank', '_blank')
    setGuardando(true)
    try {
      const { data } = await cliente.post<{ numeroActa: number }>('/api/actas', {
        fechaInicial,
        fechaFinal,
        arquitectoId,
        revisorId,
        contratistaId,
        observacion: observacion.trim() || null,
        detalles: detalles.map((item) => ({
          construccionId: item.construccionId,
          conceptoId: item.conceptoId,
          unidadMedidaId: item.unidadMedidaId,
          materialId: item.materialId,
          cantidad: item.cantidad,
          precio: item.precio,
          descripcion: item.descripcion,
        })),
      })
      limpiarFormulario()
      setNumeroActa(data.numeroActa)
      localStorage.removeItem(CLAVE)
      await Swal.fire({
        icon: 'success',
        title: '¡Finalizado!',
        text: `Acta N° ${data.numeroActa}`,
        confirmButtonColor: '#1abb9c',
      })
      await abrirPdf(data.numeroActa, ventana)
    } catch (error) {
      ventana?.close()
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

    limpiarFormulario()
    setNumeroActa(null)
    localStorage.removeItem(CLAVE)
  }

  function limpiarFormulario() {
    setFechaInicial('')
    setFechaFinal('')
    setArquitectoId(0)
    setRevisorId(0)
    setContratistaId(0)
    setObservacion('')
    setDetalles([])
  }

  return (
    <section>
      <h1>Toma de actas – Perfil Arquitecto</h1>
      <div className="catalogo-panel">
        <Row className="g-3">
          <Col md={6}>
            <Form.Group controlId="acta-fecha-inicial">
              <Form.Label>Fecha inicial</Form.Label>
              <Form.Control type="date" value={fechaInicial} onChange={(evento) => setFechaInicial(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={6}>
            <Form.Group controlId="acta-fecha-final">
              <Form.Label>Fecha final</Form.Label>
              <Form.Control type="date" value={fechaFinal} onChange={(evento) => setFechaFinal(evento.target.value)} />
            </Form.Group>
          </Col>
          <Col md={4}>
            <Selector id="acta-arquitecto" etiqueta="Arquitecto" valor={arquitectoId} opciones={arquitectos.data} campo="nombre" alCambiar={setArquitectoId} />
          </Col>
          <Col md={4}>
            <Selector id="acta-revisor" etiqueta="Revisor" valor={revisorId} opciones={revisores.data} campo="nombre" alCambiar={setRevisorId} />
          </Col>
          <Col md={4}>
            <Selector id="acta-contratista" etiqueta="Contratista" valor={contratistaId} opciones={contratistas.data} campo="nombre" alCambiar={setContratistaId} />
          </Col>
        </Row>
        <Button type="button" className="mt-3" onClick={abrirNuevaLinea}>
          Agregar ítems
        </Button>
      </div>

      <div className="catalogo-panel">
        {detalles.length === 0 ? (
          <p>No hay ítems.</p>
        ) : (
          <div className="table-responsive">
            <Table hover className="catalogo-tabla mb-0">
              <thead>
                <tr>
                  <th>Seleccionar</th>
                  <th>Construcción</th>
                  <th>Concepto</th>
                  <th>Material</th>
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
                      <Button type="button" size="sm" variant="outline-primary" onClick={() => abrirLinea(item)}>
                        Seleccionar
                      </Button>
                    </td>
                    <td>{item.construccion}</td>
                    <td>{item.concepto}</td>
                    <td>{item.material ?? ''}</td>
                    <td>{item.unidadMedida}</td>
                    <td>{item.cantidad}</td>
                    <td>{dinero(item.precio, 2)}</td>
                    <td>{item.descripcion}</td>
                    <td>{dinero(item.cantidad * item.precio, 2)}</td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </div>
        )}
        <p className="total-acta">Total: {dinero(total, 2)}</p>
      </div>

      <div className="catalogo-panel">
        <Form.Group controlId="acta-comentario">
          <Form.Label>Comentario</Form.Label>
          <Form.Control as="textarea" rows={3} maxLength={1000} value={observacion} onChange={(evento) => setObservacion(evento.target.value)} />
        </Form.Group>
        <div className="catalogo-acciones mt-3">
          <Button type="button" disabled={guardando} onClick={() => void registrar()}>
            Registrar
          </Button>
          <Button type="button" variant="outline-secondary" onClick={() => void cancelar()}>
            Cancelar
          </Button>
          {numeroActa != null && (
            <Button type="button" variant="outline-primary" onClick={() => void abrirPdf(numeroActa)}>
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
          <Selector id="linea-construccion" etiqueta="Construcción" valor={Number(linea.construccionId)} opciones={construcciones.data} campo="descripcion" alCambiar={(id) => setLinea({ ...linea, construccionId: id ? String(id) : '' })} />
          <Selector id="linea-concepto" etiqueta="Concepto" valor={Number(linea.conceptoId)} opciones={conceptos.data} campo="descripcion" alCambiar={(id) => setLinea({ ...linea, conceptoId: id ? String(id) : '' })} />
          <Selector id="linea-um" etiqueta="Unidad de medida" valor={Number(linea.unidadMedidaId)} opciones={unidades.data} campo="nombre" alCambiar={(id) => setLinea({ ...linea, unidadMedidaId: id ? String(id) : '' })} />
          <Selector id="linea-material" etiqueta="Material" valor={Number(linea.materialId)} opciones={materiales.data} campo="descripcion" alCambiar={(id) => setLinea({ ...linea, materialId: id ? String(id) : '' })} />
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
    </section>
  )
}

type LineaFormulario = {
  construccionId: string
  conceptoId: string
  unidadMedidaId: string
  materialId: string
  cantidad: string
  precio: string
  descripcion: string
}

function lineaVacia(): LineaFormulario {
  return {
    construccionId: '',
    conceptoId: '',
    unidadMedidaId: '',
    materialId: '',
    cantidad: '',
    precio: '',
    descripcion: '',
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
  alCambiar,
}: {
  id: string
  etiqueta: string
  valor: number
  opciones: RegistroCatalogo[] | undefined
  campo: 'nombre' | 'descripcion'
  alCambiar: (id: number) => void
}) {
  return (
    <Form.Group className="mb-3" controlId={id}>
      <Form.Label>{etiqueta}</Form.Label>
      <Form.Select value={valor || ''} onChange={(evento) => alCambiar(Number(evento.target.value))}>
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
  materiales: RegistroCatalogo[] | undefined,
): Omit<LineaBorrador, 'clave'> | null {
  const construccion = buscar(construcciones, Number(formulario.construccionId))
  const concepto = buscar(conceptos, Number(formulario.conceptoId))
  const unidad = buscar(unidades, Number(formulario.unidadMedidaId))
  const material = formulario.materialId ? buscar(materiales, Number(formulario.materialId)) : null
  const cantidad = numero(formulario.cantidad)
  const precio = numero(formulario.precio)
  const descripcion = formulario.descripcion.trim()
  if (!construccion || !concepto || !unidad || !descripcion || cantidad == null || cantidad <= 0 || precio == null || precio < 0) {
    return null
  }

  if (formulario.materialId && !material) {
    return null
  }

  return {
    construccionId: construccion.id,
    construccion: String(construccion.descripcion ?? ''),
    conceptoId: concepto.id,
    concepto: String(concepto.descripcion ?? ''),
    unidadMedidaId: unidad.id,
    unidadMedida: String(unidad.nombre ?? ''),
    materialId: material?.id ?? null,
    material: material ? String(material.descripcion ?? '') : null,
    cantidad: redondear(cantidad, 4),
    precio: redondear(precio, 2),
    descripcion,
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
  cantidadLineas: number,
) {
  if (!fechaInicial || !fechaFinal || arquitectoId <= 0 || revisorId <= 0 || contratistaId <= 0) {
    return mensajeEncabezado
  }

  if (fechaFinal < fechaInicial) {
    return mensajeFechas
  }

  if (cantidadLineas === 0) {
    return mensajeDetalle
  }

  return null
}

function leerBorrador(): Borrador {
  const vacio: Borrador = {
    fechaInicial: '',
    fechaFinal: '',
    arquitectoId: 0,
    revisorId: 0,
    contratistaId: 0,
    observacion: '',
    detalles: [],
  }
  try {
    const texto = localStorage.getItem(CLAVE)
    if (!texto) {
      return vacio
    }

    const valor = JSON.parse(texto) as Partial<Borrador>
    return { ...vacio, ...valor, detalles: Array.isArray(valor.detalles) ? valor.detalles : [] }
  } catch {
    return vacio
  }
}

function avisar(texto: string) {
  return Swal.fire({ icon: 'error', title: '¡Error!', text: texto, confirmButtonColor: '#1abb9c' })
}
