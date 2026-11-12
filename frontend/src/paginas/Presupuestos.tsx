import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button, Form, Modal, Nav, Table } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import type { InformeImportacion, PlantillaResumen } from '../api/presupuesto'
import { AsistentePresupuesto } from './AsistentePresupuesto'
import { usePresupuestos } from '../api/presupuesto'
import { esAdministrador } from '../auth/perfiles'
import { useSesion } from '../auth/contextoSesion'
import { dinero as formatoDinero } from '../actas/formato'

function dinero(valor: number) {
  return formatoDinero(valor, 2)
}
import type { RegistroCatalogo } from '../catalogos/definiciones'

export function Presupuestos() {
  const { sesion } = useSesion()
  const administrador = esAdministrador(sesion?.tipoUsuario)
  const navegar = useNavigate()
  const [construccionId, setConstruccionId] = useState(0)
  const [abierto, setAbierto] = useState<'nuevo' | 'importar' | null>(null)
  const [obraImportar, setObraImportar] = useState(0)
  const [pestana, setPestana] = useState<'obras' | 'plantillas'>('obras')
  const plantillas = useQuery({
    queryKey: ['plantillas'],
    queryFn: async () => (await cliente.get<PlantillaResumen[]>('/api/presupuestos/plantillas')).data,
  })
  const consulta = usePresupuestos(construccionId)
  const obras = useQuery({
    queryKey: ['construcciones'],
    queryFn: async () => (await cliente.get<RegistroCatalogo[]>('/api/construcciones')).data,
  })

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h1 className="mb-0">Presupuestos</h1>
        {administrador && (
          <div className="d-flex gap-2">
            <Button type="button" variant="outline-primary" onClick={() => setAbierto('importar')}>
              Importar Excel
            </Button>
            <Button type="button" onClick={() => setAbierto('nuevo')}>
              Nuevo
            </Button>
          </div>
        )}
      </div>
      <Nav variant="tabs" activeKey={pestana} onSelect={(clave) => setPestana(clave === 'plantillas' ? 'plantillas' : 'obras')} className="mb-3">
        <Nav.Item>
          <Nav.Link eventKey="obras">Presupuestos</Nav.Link>
        </Nav.Item>
        <Nav.Item>
          <Nav.Link eventKey="plantillas">Plantillas</Nav.Link>
        </Nav.Item>
      </Nav>
      {pestana === 'plantillas' ? (
        <div className="catalogo-panel">
          <Table hover className="mb-0">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Capítulos</th>
                <th>Ítems</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {(plantillas.data ?? []).map((plantilla) => (
                <tr key={plantilla.id}>
                  <td>
                    <Link to={`/presupuestos/${plantilla.id}`}>{plantilla.nombre}</Link>
                  </td>
                  <td>{plantilla.capitulos}</td>
                  <td>{plantilla.items}</td>
                  <td className="text-end">
                    <Button
                      type="button"
                      size="sm"
                      variant="outline-danger"
                      onClick={async () => {
                        await cliente.delete(`/api/presupuestos/${plantilla.id}`)
                        await plantillas.refetch()
                      }}
                    >
                      Eliminar
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </div>
      ) : (
        <>
      <Form.Group className="mb-3" style={{ maxWidth: '24rem' }}>
        <Form.Label>Construcción</Form.Label>
        <Form.Select value={construccionId || ''} onChange={(evento) => setConstruccionId(Number(evento.target.value))}>
          <option value="">Todas</option>
          {(obras.data ?? []).map((obra) => (
            <option key={obra.id} value={obra.id}>
              {obra.descripcion}
            </option>
          ))}
        </Form.Select>
      </Form.Group>
      <div className="catalogo-panel">
        <Table responsive hover className="mb-0">
          <thead>
            <tr>
              <th>Construcción</th>
              <th>Nombre</th>
              <th>Versión</th>
              <th>Estado</th>
              <th className="text-end">Total</th>
              <th className="text-end">Valor/m²</th>
              <th className="text-end">Ejecución</th>
            </tr>
          </thead>
          <tbody>
            {(consulta.data ?? []).map((presupuesto) => (
              <tr key={presupuesto.id}>
                <td>{presupuesto.construccion}</td>
                <td>
                  <Link to={`/presupuestos/${presupuesto.id}`}>{presupuesto.nombre}</Link>
                </td>
                <td>{presupuesto.version}</td>
                <td>{presupuesto.estado}</td>
                <td className="text-end">{dinero(presupuesto.total)}</td>
                <td className="text-end">{presupuesto.valorPorM2 == null ? '—' : dinero(presupuesto.valorPorM2)}</td>
                <td className="text-end">{(presupuesto.avanceFinanciero * 100).toLocaleString('es-CO', { maximumFractionDigits: 1 })} %</td>
              </tr>
            ))}
            {consulta.data?.length === 0 && (
              <tr>
                <td colSpan={7}>No hay presupuestos para el filtro.</td>
              </tr>
            )}
          </tbody>
        </Table>
      </div>
        </>
      )}
      <Modal show={abierto !== null} onHide={() => setAbierto(null)} centered>
        <Modal.Header closeButton>
          <Modal.Title>{abierto === 'importar' ? 'Importar Excel' : 'Nuevo presupuesto'}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {abierto === 'nuevo' ? (
            <AsistentePresupuesto
              obras={obras.data ?? []}
              alCrear={(id) => navegar(`/presupuestos/${id}`)}
              alImportar={(construccionId) => {
                setObraImportar(construccionId)
                setAbierto('importar')
              }}
            />
          ) : (
            <FormularioImportar obras={obras.data ?? []} inicial={obraImportar} alImportar={(informe) => void mostrarInforme(informe, navegar)} />
          )}
        </Modal.Body>
      </Modal>
    </section>
  )
}

function FormularioImportar({
  obras,
  inicial,
  alImportar,
}: {
  obras: RegistroCatalogo[]
  inicial: number
  alImportar: (informe: InformeImportacion) => void
}) {
  const [construccionId, setConstruccionId] = useState(inicial)
  const [archivo, setArchivo] = useState<File | null>(null)
  const [enviando, setEnviando] = useState(false)

  async function importar() {
    if (!archivo || construccionId <= 0) {
      return
    }
    const datos = new FormData()
    datos.append('archivo', archivo)
    datos.append('construccionId', String(construccionId))
    setEnviando(true)
    try {
      const { data } = await cliente.post<InformeImportacion>('/api/presupuestos/importar', datos)
      alImportar(data)
    } catch (error) {
      await Swal.fire({ icon: 'error', title: 'No se pudo importar', text: mensajeError(error) })
    } finally {
      setEnviando(false)
    }
  }

  return (
    <>
      <Obra obras={obras} valor={construccionId} alCambiar={setConstruccionId} />
      <Form.Group className="mb-3">
        <Form.Label>Libro Excel</Form.Label>
        <Form.Control
          type="file"
          accept=".xlsx"
          onChange={(evento) => setArchivo((evento.target as HTMLInputElement).files?.[0] ?? null)}
        />
      </Form.Group>
      <Button type="button" disabled={!archivo || construccionId <= 0 || enviando} onClick={() => void importar()}>
        Importar
      </Button>
    </>
  )
}

function Obra({ obras, valor, alCambiar }: { obras: RegistroCatalogo[]; valor: number; alCambiar: (id: number) => void }) {
  return (
    <Form.Group className="mb-3">
      <Form.Label>Construcción</Form.Label>
      <Form.Select value={valor || ''} onChange={(evento) => alCambiar(Number(evento.target.value))}>
        <option value="">Seleccione</option>
        {obras.map((obra) => (
          <option key={obra.id} value={obra.id}>
            {obra.descripcion}
          </option>
        ))}
      </Form.Select>
    </Form.Group>
  )
}

async function mostrarInforme(informe: InformeImportacion, navegar: (ruta: string) => void) {
  const texto = [
    `APU reproducidos: ${informe.apusReproducidos} de ${informe.apusRevisados}.`,
    `Unidades que no coinciden: ${informe.unidadesDistintas}.`,
    `Códigos duplicados: ${informe.codigosDuplicados}.`,
    `Ítems sin APU: ${informe.itemsSinApu}.`,
    `Total importado: ${dinero(informe.total)}.`,
  ].join('\n')
  await Swal.fire({ icon: 'info', title: 'Informe de importación', text: texto })
  navegar(`/presupuestos/${informe.presupuestoId}`)
}
