import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import type { Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery } from '@tanstack/react-query'
import { Button, Col, Form, Row } from 'react-bootstrap'
import { z } from 'zod'
import Swal from 'sweetalert2'
import { cliente, mensajeError } from '../api/cliente'
import type { ConfiguracionPresupuesto, PlantillaResumen, PresupuestoResumen } from '../api/presupuesto'
import type { RegistroCatalogo } from '../catalogos/definiciones'
import { dinero as formatoDinero } from '../actas/formato'

function dinero(valor: number) {
  return formatoDinero(valor, 2)
}

const esquema = z.object({
  construccionId: z.coerce.number().int().positive('Seleccione la construcción'),
  nombre: z.string().trim().min(1, 'El nombre es obligatorio'),
  fecha: z.string().min(1, 'La fecha es obligatoria'),
  mesesProyecto: z.coerce.number().int().min(0),
  areaConstruidaM2: z.coerce.number().positive('El área construida es obligatoria'),
  pctImprevistos: z.coerce.number().min(0).max(1),
  pctIncremento: z.coerce.number().min(0).max(1),
  pctAdministracion: z.coerce.number().min(0).max(1),
  pctUtilidad: z.coerce.number().min(0).max(1),
  pctIvaUtilidad: z.coerce.number().min(0).max(1),
})

type Datos = z.infer<typeof esquema>

export function AsistentePresupuesto({
  obras,
  alCrear,
  alImportar,
}: {
  obras: RegistroCatalogo[]
  alCrear: (id: number) => void
  alImportar: (construccionId: number) => void
}) {
  const configuracion = useQuery({
    queryKey: ['presupuesto-configuracion'],
    queryFn: async () => (await cliente.get<ConfiguracionPresupuesto>('/api/presupuestos/configuracion')).data,
  })
  const plantillas = useQuery({
    queryKey: ['plantillas'],
    queryFn: async () => (await cliente.get<PlantillaResumen[]>('/api/presupuestos/plantillas')).data,
  })
  const presupuestos = useQuery({
    queryKey: ['presupuestos', 'copia'],
    queryFn: async () => (await cliente.get<PresupuestoResumen[]>('/api/presupuestos')).data,
  })
  const [paso, setPaso] = useState<1 | 2>(1)
  const [origen, setOrigen] = useState<'vacio' | 'plantilla' | 'copia'>('vacio')
  const [origenId, setOrigenId] = useState(0)
  const [conservar, setConservar] = useState(false)
  const hoy = new Date().toISOString().slice(0, 10)
  const formulario = useForm<Datos>({
    resolver: zodResolver(esquema) as Resolver<Datos>,
    defaultValues: {
      construccionId: 0,
      nombre: 'Presupuesto',
      fecha: hoy,
      mesesProyecto: 0,
      areaConstruidaM2: 0,
      pctImprevistos: 0.05,
      pctIncremento: 0.07,
      pctAdministracion: 0,
      pctUtilidad: 0,
      pctIvaUtilidad: 0.19,
    },
  })
  useEffect(() => {
    if (!configuracion.data) {
      return
    }
    formulario.setValue('pctImprevistos', configuracion.data.pctImprevistos)
    formulario.setValue('pctIncremento', configuracion.data.pctIncremento)
    formulario.setValue('pctAdministracion', configuracion.data.pctAdministracion)
    formulario.setValue('pctUtilidad', configuracion.data.pctUtilidad)
    formulario.setValue('pctIvaUtilidad', configuracion.data.pctIvaUtilidad)
  }, [configuracion.data, formulario])
  const obraId = Number(formulario.watch('construccionId'))
  const aprobada = (presupuestos.data ?? []).find((item) => item.construccionId === obraId && item.estado === 'Aprobado')

  async function crear() {
    const datos = formulario.getValues()
    try {
      const { data } = await cliente.post<{ id: number }>('/api/presupuestos', {
        ...datos,
        origen,
        origenPresupuestoId: origen === 'vacio' ? null : origenId,
        conservarCantidades: conservar,
      })
      alCrear(data.id)
    } catch (error) {
      await Swal.fire({ icon: 'error', title: 'No se pudo crear', text: mensajeError(error) })
    }
  }

  if (paso === 1) {
    return (
      <Form
        onSubmit={formulario.handleSubmit(() => {
          const obra = obras.find((item) => item.id === obraId)
          if (obra && !formulario.getValues('nombre').startsWith('Presupuesto ')) {
            formulario.setValue('nombre', `Presupuesto ${obra.descripcion}`)
          }
          setPaso(2)
        })}
      >
        <Form.Group className="mb-2">
          <Form.Label>Construcción</Form.Label>
          <Form.Select {...formulario.register('construccionId')}>
            <option value={0}>Seleccione</option>
            {obras.filter((obra) => obra.activo).map((obra) => (
              <option key={obra.id} value={obra.id}>
                {obra.descripcion}
              </option>
            ))}
          </Form.Select>
          <Form.Text className="text-danger">{formulario.formState.errors.construccionId?.message}</Form.Text>
        </Form.Group>
        {aprobada && (
          <p className="text-warning">Se creará la versión {aprobada.version + 1} en borrador; la aprobada sigue vigente hasta que apruebe la nueva.</p>
        )}
        <Form.Group className="mb-2">
          <Form.Label>Nombre</Form.Label>
          <Form.Control {...formulario.register('nombre')} />
        </Form.Group>
        <Row>
          <Col>
            <Form.Label>Fecha</Form.Label>
            <Form.Control type="date" {...formulario.register('fecha')} />
          </Col>
          <Col>
            <Form.Label>Meses</Form.Label>
            <Form.Control {...formulario.register('mesesProyecto')} />
          </Col>
          <Col>
            <Form.Label>Área m²</Form.Label>
            <Form.Control {...formulario.register('areaConstruidaM2')} />
            <Form.Text className="text-danger">{formulario.formState.errors.areaConstruidaM2?.message}</Form.Text>
          </Col>
        </Row>
        <Row className="mt-2">
          {([
            ['pctImprevistos', 'Imprevistos'],
            ['pctIncremento', 'Incremento'],
            ['pctAdministracion', 'Administración'],
            ['pctUtilidad', 'Utilidad'],
            ['pctIvaUtilidad', 'IVA utilidad'],
          ] as const).map(([campo, etiqueta]) => (
            <Col key={campo}>
              <Form.Label>{etiqueta}</Form.Label>
              <Form.Control {...formulario.register(campo)} />
            </Col>
          ))}
        </Row>
        <Button type="submit" className="mt-3">
          Continuar
        </Button>
      </Form>
    )
  }

  return (
    <>
      <div className="d-grid gap-2">
        <Button type="button" variant={origen === 'vacio' ? 'primary' : 'outline-primary'} onClick={() => setOrigen('vacio')}>
          En blanco
        </Button>
        <Button type="button" variant={origen === 'plantilla' ? 'primary' : 'outline-primary'} onClick={() => setOrigen('plantilla')}>
          Desde plantilla
        </Button>
        {origen === 'plantilla' && (
          <Form.Select value={origenId || ''} onChange={(evento) => setOrigenId(Number(evento.target.value))}>
            <option value="">Seleccione</option>
            {(plantillas.data ?? []).map((plantilla) => (
              <option key={plantilla.id} value={plantilla.id}>
                {plantilla.nombre} · {plantilla.capitulos} capítulos · {plantilla.items} ítems
              </option>
            ))}
          </Form.Select>
        )}
        <Button type="button" variant={origen === 'copia' ? 'primary' : 'outline-primary'} onClick={() => setOrigen('copia')}>
          Copiar de otra obra
        </Button>
        {origen === 'copia' && (
          <>
            <Form.Select value={origenId || ''} onChange={(evento) => setOrigenId(Number(evento.target.value))}>
              <option value="">Seleccione</option>
              {(presupuestos.data ?? []).map((presupuesto) => (
                <option key={presupuesto.id} value={presupuesto.id}>
                  {presupuesto.construccion} · v{presupuesto.version} · {dinero(presupuesto.total)}
                </option>
              ))}
            </Form.Select>
            <Form.Check label="Conservar cantidades" checked={conservar} onChange={(evento) => setConservar(evento.target.checked)} />
          </>
        )}
        <Button type="button" variant="outline-secondary" onClick={() => alImportar(obraId)}>
          Importar Excel
        </Button>
      </div>
      <div className="d-flex gap-2 mt-3">
        <Button type="button" variant="link" onClick={() => setPaso(1)}>
          Atrás
        </Button>
        <Button type="button" disabled={origen !== 'vacio' && origenId <= 0} onClick={() => void crear()}>
          Crear
        </Button>
      </div>
    </>
  )
}
