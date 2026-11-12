import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button, Form, Pagination, Table } from 'react-bootstrap'
import { useForm } from 'react-hook-form'
import type { Resolver } from 'react-hook-form'
import Swal from 'sweetalert2'
import { z } from 'zod'
import { cliente, mensajeError } from '../api/cliente'
import type { CampoCatalogo, ConfiguracionCatalogo, RegistroCatalogo } from '../catalogos/definiciones'

const TAMANO_PAGINA = 10

type Valores = Record<string, string>

type Propiedades = {
  configuracion: ConfiguracionCatalogo
}

const sinRegistros: RegistroCatalogo[] = []

export function CatalogoCrud({ configuracion }: Propiedades) {
  const consultas = useQueryClient()
  const [seleccionId, setSeleccionId] = useState<number | null>(null)
  const [pagina, setPagina] = useState(1)
  const [busqueda, setBusqueda] = useState('')
  const esquema = useMemo(() => crearEsquema(configuracion.campos), [configuracion])
  const vacios = useMemo(() => valoresVacios(configuracion.campos), [configuracion])
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<Valores>({
    resolver: zodResolver(esquema) as Resolver<Valores>,
    defaultValues: vacios,
  })

  const lista = useQuery({
    queryKey: ['catalogo', configuracion.recurso],
    queryFn: async () => {
      const { data } = await cliente.get<RegistroCatalogo[]>(`/api/${configuracion.recurso}`)
      return data
    },
  })

  const guardar = useMutation({
    mutationFn: async (valores: Valores) => {
      const cuerpo = aSolicitud(configuracion.campos, valores)
      if (seleccionId == null) {
        const { data } = await cliente.post<RegistroCatalogo>(`/api/${configuracion.recurso}`, cuerpo)
        return data
      }

      const { data } = await cliente.put<RegistroCatalogo>(
        `/api/${configuracion.recurso}/${seleccionId}`,
        cuerpo,
      )
      return data
    },
    onSuccess: async (registro) => {
      await consultas.invalidateQueries({ queryKey: ['catalogo', configuracion.recurso] })
      const registros =
        consultas.getQueryData<RegistroCatalogo[]>(['catalogo', configuracion.recurso]) ?? []
      const visibles = filtrarRegistros(registros, configuracion.columnas, busqueda)
      const indiceVisible = visibles.findIndex((item) => item.id === registro.id)
      if (indiceVisible < 0) {
        setBusqueda('')
        const indice = registros.findIndex((item) => item.id === registro.id)
        setPagina(indice < 0 ? 1 : Math.floor(indice / TAMANO_PAGINA) + 1)
      } else {
        setPagina(Math.floor(indiceVisible / TAMANO_PAGINA) + 1)
      }
      setSeleccionId(null)
      reset(vacios)
      await Swal.fire({ icon: 'success', title: '¡Finalizado!', confirmButtonColor: '#1abb9c' })
    },
    onError: async (error) => {
      await Swal.fire({
        icon: 'error',
        title: '¡Error!',
        text: mensajeError(error),
        confirmButtonColor: '#1abb9c',
      })
    },
  })

  const quitar = useMutation({
    mutationFn: async (id: number) => {
      await cliente.delete(`/api/${configuracion.recurso}/${id}`)
    },
    onSuccess: async () => {
      await consultas.invalidateQueries({ queryKey: ['catalogo', configuracion.recurso] })
      setSeleccionId(null)
      reset(vacios)
      await Swal.fire({ icon: 'success', title: '¡Finalizado!', confirmButtonColor: '#1abb9c' })
    },
    onError: async (error) => {
      await Swal.fire({
        icon: 'error',
        title: '¡Error!',
        text: mensajeError(error),
        confirmButtonColor: '#1abb9c',
      })
    },
  })

  const registros = lista.data ?? sinRegistros
  const visibles = useMemo(
    () => filtrarRegistros(registros, configuracion.columnas, busqueda),
    [registros, configuracion.columnas, busqueda],
  )
  const totalPaginas = Math.max(1, Math.ceil(visibles.length / TAMANO_PAGINA))
  const paginaVisible = Math.min(pagina, totalPaginas)
  const filas = visibles.slice((paginaVisible - 1) * TAMANO_PAGINA, paginaVisible * TAMANO_PAGINA)
  const ocupado = isSubmitting || guardar.isPending || quitar.isPending

  function seleccionar(registro: RegistroCatalogo) {
    const valores: Valores = {}
    for (const campo of configuracion.campos) {
      const valor = registro[campo.nombre]
      valores[campo.nombre] = valor == null ? '' : String(valor)
    }

    setSeleccionId(registro.id)
    reset(valores)
  }

  function cancelar() {
    setSeleccionId(null)
    reset(vacios)
  }

  async function eliminar() {
    if (seleccionId == null) {
      return
    }

    const confirmacion = await Swal.fire({
      icon: 'warning',
      title: '¿Desea eliminar el registro?',
      showCancelButton: true,
      confirmButtonText: 'Eliminar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#d9534f',
      cancelButtonColor: '#6c757d',
    })
    if (!confirmacion.isConfirmed) {
      return
    }

    quitar.mutate(seleccionId)
  }

  return (
    <section className="catalogo">
      <h1>{configuracion.titulo}</h1>
      <Form className="catalogo-panel" onSubmit={handleSubmit((valores) => guardar.mutate(valores))} noValidate>
        {configuracion.campos.map((campo) => (
          <Form.Group className="mb-3" controlId={`${configuracion.recurso}-${campo.nombre}`} key={campo.nombre}>
            <Form.Label>
              {campo.etiqueta}
              {campo.requerido ? ' *' : ''}
            </Form.Label>
            <Form.Control
              type="text"
              autoComplete="off"
              isInvalid={Boolean(errors[campo.nombre])}
              {...register(campo.nombre)}
            />
            <Form.Control.Feedback type="invalid">{errors[campo.nombre]?.message}</Form.Control.Feedback>
          </Form.Group>
        ))}
        <div className="catalogo-acciones">
          {seleccionId == null ? (
            <Button type="submit" disabled={ocupado}>
              Crear
            </Button>
          ) : (
            <>
              <Button type="submit" disabled={ocupado}>
                Modificar
              </Button>
              <Button type="button" variant="danger" disabled={ocupado} onClick={() => void eliminar()}>
                Eliminar
              </Button>
            </>
          )}
          <Button type="button" variant="outline-secondary" disabled={ocupado} onClick={cancelar}>
            Cancelar
          </Button>
        </div>
      </Form>

      <div className="catalogo-panel">
        {lista.isPending && <p>Cargando…</p>}
        {lista.isError && <p>No se pudo cargar el listado.</p>}
        {lista.isSuccess && registros.length === 0 && <p>No hay registros.</p>}
        {lista.isSuccess && registros.length > 0 && (
          <>
            {registros.length > TAMANO_PAGINA && (
              <Form.Control
                className="mb-3"
                type="search"
                placeholder={`Buscar por ${configuracion.columnas.map((columna) => columna.titulo.toLocaleLowerCase('es')).join(', ')}`}
                value={busqueda}
                onChange={(evento) => {
                  setBusqueda(evento.target.value)
                  setPagina(1)
                }}
              />
            )}
            {visibles.length === 0 && <p className="mb-0">No hay registros.</p>}
            {visibles.length > 0 && (
              <>
                <div className="table-responsive">
                  <Table hover className="catalogo-tabla mb-0">
                    <thead>
                      <tr>
                        <th>Seleccionar</th>
                        {configuracion.columnas.map((columna) => (
                          <th key={columna.clave}>{columna.titulo}</th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {filas.map((registro) => (
                        <tr key={registro.id} className={registro.id === seleccionId ? 'table-active' : undefined}>
                          <td>
                            <Button
                              type="button"
                              size="sm"
                              variant={registro.id === seleccionId ? 'primary' : 'outline-primary'}
                              onClick={() => seleccionar(registro)}
                            >
                              Seleccionar
                            </Button>
                          </td>
                          {configuracion.columnas.map((columna) => (
                            <td key={columna.clave}>{texto(registro[columna.clave])}</td>
                          ))}
                        </tr>
                      ))}
                    </tbody>
                  </Table>
                </div>
                {totalPaginas > 1 && (
                  <Pagination className="catalogo-paginacion">
                    <Pagination.Prev
                      disabled={paginaVisible === 1}
                      onClick={() => setPagina(paginaVisible - 1)}
                    />
                    {Array.from({ length: totalPaginas }, (_, indice) => (
                      <Pagination.Item
                        key={indice + 1}
                        active={paginaVisible === indice + 1}
                        onClick={() => setPagina(indice + 1)}
                      >
                        {indice + 1}
                      </Pagination.Item>
                    ))}
                    <Pagination.Next
                      disabled={paginaVisible === totalPaginas}
                      onClick={() => setPagina(paginaVisible + 1)}
                    />
                  </Pagination>
                )}
              </>
            )}
          </>
        )}
      </div>
    </section>
  )
}

function crearEsquema(campos: CampoCatalogo[]) {
  const forma: Record<string, z.ZodType<string>> = {}
  for (const campo of campos) {
    const base = z.string().trim()
    forma[campo.nombre] = campo.requerido
      ? base.min(1, campo.mensajeRequerido).max(campo.maximo, campo.mensajeMaximo)
      : base.max(campo.maximo, campo.mensajeMaximo)
  }

  return z.object(forma)
}

function valoresVacios(campos: CampoCatalogo[]): Valores {
  return Object.fromEntries(campos.map((campo) => [campo.nombre, '']))
}

function aSolicitud(campos: CampoCatalogo[], valores: Valores) {
  const cuerpo: Record<string, string | number | null> = {}
  for (const campo of campos) {
    const textoLimpio = valores[campo.nombre]?.trim() ?? ''
    if (campo.numero) {
      cuerpo[campo.nombre] = textoLimpio === '' ? null : Number(textoLimpio)
    } else {
      cuerpo[campo.nombre] = textoLimpio === '' ? null : textoLimpio
    }
  }

  return cuerpo
}

function filtrarRegistros(registros: RegistroCatalogo[], columnas: ConfiguracionCatalogo['columnas'], busqueda: string) {
  const consulta = normalizar(busqueda.trim())
  if (!consulta) {
    return registros
  }

  return registros.filter((registro) =>
    columnas.some((columna) => normalizar(texto(registro[columna.clave])).includes(consulta)),
  )
}

function texto(valor: string | number | boolean | null | undefined) {
  return valor == null || valor === '' ? '' : String(valor)
}

function normalizar(valor: string) {
  return valor.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLocaleLowerCase('es')
}
