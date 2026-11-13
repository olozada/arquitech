import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button, Form, Nav, Table } from 'react-bootstrap'
import { useForm } from 'react-hook-form'
import Swal from 'sweetalert2'
import { z } from 'zod'
import { cliente, mensajeError } from '../api/cliente'
import { Paginador } from '../componentes/Paginador'
import { tamanoListado } from '../api/pagina'
import type { Pagina } from '../api/pagina'
import { errorContrasena } from '../auth/contrasena'
import { useSesion } from '../auth/contextoSesion'

const esquema = z.object({
  usuario: z
    .string()
    .trim()
    .min(1, 'Debe ingresar el usuario.')
    .max(50, 'El usuario no puede superar 50 caracteres.'),
  nombre: z
    .string()
    .trim()
    .min(1, 'Debe ingresar el nombre.')
    .max(150, 'El nombre no puede superar 150 caracteres.'),
  password: z.string(),
  tipoUsuarioId: z.string().min(1, 'Debe seleccionar el tipo de usuario.'),
  perfilId: z.string(),
})

type Valores = z.infer<typeof esquema>

type Usuario = {
  id: number
  usuario: string
  nombre: string
  tipoUsuarioId: number
  tipoUsuario: string
  activo: boolean
  perfilId: number | null
}

type TipoUsuario = {
  id: number
  nombre: string
}

type Dispositivo = {
  id: string
  nombre: string
  plataforma: string
  versionApp: string
  registradoEn: string
  ultimaSincronizacion: string | null
  revocadoEn: string | null
}

const vacios: Valores = { usuario: '', nombre: '', password: '', tipoUsuarioId: '', perfilId: '' }

const recursosPerfil: Record<string, string> = {
  '2': 'arquitectos',
  '3': 'contratistas',
  '4': 'revisores',
}

export function Usuarios() {
  const { sesion } = useSesion()
  const consultas = useQueryClient()
  const [seleccionId, setSeleccionId] = useState<number | null>(null)
  const [pestana, setPestana] = useState<'usuarios' | 'dispositivos'>('usuarios')
  const {
    register,
    handleSubmit,
    reset,
    setError,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<Valores>({
    resolver: zodResolver(esquema),
    defaultValues: vacios,
  })

  const tipos = useQuery({
    queryKey: ['tipos-usuario'],
    queryFn: async () => {
      const { data } = await cliente.get<TipoUsuario[]>('/api/usuarios/tipos')
      return data
    },
    enabled: sesion?.tipoUsuario === 'Administrador',
  })

  const [pagina, setPagina] = useState(1)
  const lista = useQuery({
    queryKey: ['usuarios', pagina],
    queryFn: async () => {
      const { data } = await cliente.get<Pagina<Usuario>>('/api/usuarios', { params: { pagina, tamano: tamanoListado } })
      return data
    },
    enabled: sesion?.tipoUsuario === 'Administrador',
  })

  const guardar = useMutation({
    mutationFn: async (valores: Valores) => {
      const cuerpo = {
        usuario: valores.usuario.trim(),
        nombre: valores.nombre.trim(),
        password: valores.password.trim() === '' ? null : valores.password,
        tipoUsuarioId: Number(valores.tipoUsuarioId),
        perfilId: valores.perfilId ? Number(valores.perfilId) : null,
      }
      if (seleccionId == null) {
        const { data } = await cliente.post<Usuario>('/api/usuarios', cuerpo)
        return data
      }

      const { data } = await cliente.put<Usuario>(`/api/usuarios/${seleccionId}`, cuerpo)
      return data
    },
    onSuccess: async () => {
      await consultas.invalidateQueries({ queryKey: ['usuarios'] })
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
      await cliente.delete(`/api/usuarios/${id}`)
    },
    onSuccess: async () => {
      await consultas.invalidateQueries({ queryKey: ['usuarios'] })
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

  const dispositivos = useQuery({
    queryKey: ['dispositivos', seleccionId],
    enabled: pestana === 'dispositivos' && seleccionId != null && sesion?.tipoUsuario === 'Administrador',
    queryFn: async () => (await cliente.get<Dispositivo[]>(`/api/usuarios/${seleccionId}/dispositivos`)).data,
  })

  const revocar = useMutation({
    mutationFn: async (dispositivoId: string) => {
      await cliente.post(`/api/usuarios/${seleccionId}/dispositivos/${dispositivoId}/revocar`)
    },
    onSuccess: async () => {
      await consultas.invalidateQueries({ queryKey: ['dispositivos', seleccionId] })
      await Swal.fire({ icon: 'success', title: 'Dispositivo revocado', confirmButtonColor: '#1abb9c' })
    },
    onError: async (error) => {
      await Swal.fire({ icon: 'error', title: '¡Error!', text: mensajeError(error), confirmButtonColor: '#1abb9c' })
    },
  })

  const tipoSeleccionado = watch('tipoUsuarioId')
  const recursoPerfil = recursosPerfil[tipoSeleccionado]
  const personas = useQuery({
    queryKey: ['catalogo', recursoPerfil],
    enabled: Boolean(recursoPerfil),
    queryFn: async () => (await cliente.get<Array<{ id: number; nombre: string }>>(`/api/${recursoPerfil}`)).data,
  })

  if (sesion?.tipoUsuario !== 'Administrador') {
    return (
      <section className="catalogo">
        <h1>Usuarios</h1>
        <p>No tiene permiso para administrar usuarios.</p>
      </section>
    )
  }

  const ocupado = isSubmitting || guardar.isPending || quitar.isPending
  const registros = lista.data?.items ?? []

  function seleccionar(registro: Usuario) {
    setSeleccionId(registro.id)
    reset({
      usuario: registro.usuario,
      nombre: registro.nombre,
      password: '',
      tipoUsuarioId: String(registro.tipoUsuarioId),
      perfilId: registro.perfilId ? String(registro.perfilId) : '',
    })
  }

  function cancelar() {
    setSeleccionId(null)
    reset(vacios)
  }

  function enviar(valores: Valores) {
    const password = valores.password.trim()
    if (seleccionId == null && password.length === 0) {
      setError('password', { message: 'Debe ingresar la contraseña.' })
      return
    }

    if (password.length > 0) {
      const mensaje = errorContrasena(password, valores.usuario)
      if (mensaje) {
        setError('password', { message: mensaje })
        return
      }
    }

    if (recursosPerfil[valores.tipoUsuarioId] && !valores.perfilId) {
      setError('perfilId', { message: 'Debe seleccionar la persona del perfil.' })
      return
    }

    guardar.mutate(valores)
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

  async function revocarDispositivo(dispositivo: Dispositivo) {
    const confirmacion = await Swal.fire({
      icon: 'warning',
      title: `¿Revocar ${dispositivo.nombre}?`,
      text: 'El teléfono tendrá que volver a iniciar sesión y perderá los datos locales.',
      showCancelButton: true,
      confirmButtonText: 'Revocar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#d9534f',
      cancelButtonColor: '#6c757d',
    })
    if (confirmacion.isConfirmed) {
      revocar.mutate(dispositivo.id)
    }
  }

  return (
    <section className="catalogo">
      <h1>Usuarios</h1>
      <Nav variant="tabs" activeKey={pestana} onSelect={(clave) => setPestana(clave === 'dispositivos' ? 'dispositivos' : 'usuarios')} className="mb-3">
        <Nav.Item>
          <Nav.Link eventKey="usuarios">Usuarios</Nav.Link>
        </Nav.Item>
        <Nav.Item>
          <Nav.Link eventKey="dispositivos">Dispositivos</Nav.Link>
        </Nav.Item>
      </Nav>
      {pestana === 'dispositivos' ? (
        <div className="catalogo-panel">
          {seleccionId == null && <p>Seleccione un usuario en la pestaña Usuarios.</p>}
          {seleccionId != null && dispositivos.isPending && <p>Cargando…</p>}
          {seleccionId != null && dispositivos.isError && <p>No se pudieron cargar los dispositivos.</p>}
          {seleccionId != null && dispositivos.isSuccess && dispositivos.data.length === 0 && <p>Este usuario no tiene dispositivos.</p>}
          {seleccionId != null && dispositivos.isSuccess && dispositivos.data.length > 0 && (
            <div className="table-responsive">
              <Table hover className="catalogo-tabla mb-0">
                <thead>
                  <tr>
                    <th>Nombre</th>
                    <th>Plataforma</th>
                    <th>Versión</th>
                    <th>Última sincronización</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {dispositivos.data.map((dispositivo) => (
                    <tr key={dispositivo.id}>
                      <td>{dispositivo.nombre}</td>
                      <td>{dispositivo.plataforma}</td>
                      <td>{dispositivo.versionApp}</td>
                      <td>{dispositivo.ultimaSincronizacion ? new Date(dispositivo.ultimaSincronizacion).toLocaleString('es-CO') : 'Sin sincronizar'}</td>
                      <td className="text-end">
                        {dispositivo.revocadoEn ? (
                          'Revocado'
                        ) : (
                          <Button type="button" size="sm" variant="danger" disabled={revocar.isPending} onClick={() => void revocarDispositivo(dispositivo)}>
                            Revocar
                          </Button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </div>
          )}
        </div>
      ) : (
      <>
      <Form className="catalogo-panel" onSubmit={handleSubmit(enviar)} noValidate>
        <Form.Group className="mb-3" controlId="usuario-login">
          <Form.Label>Usuario *</Form.Label>
          <Form.Control autoComplete="off" isInvalid={Boolean(errors.usuario)} {...register('usuario')} />
          <Form.Control.Feedback type="invalid">{errors.usuario?.message}</Form.Control.Feedback>
        </Form.Group>
        <Form.Group className="mb-3" controlId="usuario-nombre">
          <Form.Label>Nombre *</Form.Label>
          <Form.Control autoComplete="off" isInvalid={Boolean(errors.nombre)} {...register('nombre')} />
          <Form.Control.Feedback type="invalid">{errors.nombre?.message}</Form.Control.Feedback>
        </Form.Group>
        <Form.Group className="mb-3" controlId="usuario-password">
          <Form.Label>Contraseña {seleccionId == null ? '*' : ''}</Form.Label>
          <Form.Control
            type="password"
            autoComplete="new-password"
            isInvalid={Boolean(errors.password)}
            {...register('password')}
          />
          {seleccionId != null && (
            <Form.Text>Déjela en blanco para conservar la actual.</Form.Text>
          )}
          <Form.Control.Feedback type="invalid">{errors.password?.message}</Form.Control.Feedback>
        </Form.Group>
        <Form.Group className="mb-3" controlId="usuario-tipo">
          <Form.Label>Tipo de usuario *</Form.Label>
          <Form.Select isInvalid={Boolean(errors.tipoUsuarioId)} {...register('tipoUsuarioId')}>
            <option value="">Seleccione</option>
            {(tipos.data ?? []).map((tipo) => (
              <option key={tipo.id} value={tipo.id}>
                {tipo.nombre}
              </option>
            ))}
          </Form.Select>
          <Form.Control.Feedback type="invalid">{errors.tipoUsuarioId?.message}</Form.Control.Feedback>
        </Form.Group>
        {recursoPerfil && (
          <Form.Group className="mb-3" controlId="usuario-perfil">
            <Form.Label>Persona del perfil *</Form.Label>
            <Form.Select isInvalid={Boolean(errors.perfilId)} {...register('perfilId')}>
              <option value="">Seleccione</option>
              {(personas.data ?? []).map((persona) => (
                <option key={persona.id} value={persona.id}>
                  {persona.nombre}
                </option>
              ))}
            </Form.Select>
            <Form.Control.Feedback type="invalid">{errors.perfilId?.message}</Form.Control.Feedback>
          </Form.Group>
        )}
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
          <div className="table-responsive">
            <Table hover className="catalogo-tabla mb-0">
              <thead>
                <tr>
                  <th>Seleccionar</th>
                  <th>Código</th>
                  <th>Usuario</th>
                  <th>Nombre</th>
                  <th>Tipo</th>
                </tr>
              </thead>
              <tbody>
                {registros.map((registro) => (
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
                    <td>{registro.id}</td>
                    <td>{registro.usuario}</td>
                    <td>{registro.nombre}</td>
                    <td>{registro.tipoUsuario}</td>
                  </tr>
                ))}
              </tbody>
            </Table>
            <Paginador pagina={pagina} total={lista.data?.total ?? 0} tamano={lista.data?.tamano ?? tamanoListado} alCambiar={setPagina} />
          </div>
        )}
      </div>
      </>
      )}
    </section>
  )
}
