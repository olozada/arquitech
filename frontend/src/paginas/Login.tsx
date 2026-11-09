import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Navigate, useNavigate } from 'react-router-dom'
import { Button, Form } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { z } from 'zod'
import { cliente, mensajeError } from '../api/cliente'
import type { Sesion } from '../api/sesion'
import { useSesion } from '../auth/contextoSesion'

const esquema = z.object({
  usuario: z.string().trim().min(1, 'Debe ingresar el usuario.'),
  password: z.string().min(1, 'Debe ingresar la contraseña.'),
})

type FormularioLogin = z.infer<typeof esquema>

export function Login() {
  const { sesion, iniciarSesion } = useSesion()
  const navegar = useNavigate()
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormularioLogin>({
    resolver: zodResolver(esquema),
    defaultValues: { usuario: '', password: '' },
  })

  if (sesion) {
    return <Navigate to="/" replace />
  }

  async function enviar(valores: FormularioLogin) {
    try {
      const { data } = await cliente.post<Sesion>('/api/auth/login', valores)
      iniciarSesion(data)
      await navegar('/')
    } catch (error) {
      await Swal.fire({
        icon: 'error',
        title: '¡Error!',
        text: mensajeError(error),
      })
    }
  }

  return (
    <main className="login-pagina">
      <Form className="login-tarjeta" onSubmit={handleSubmit(enviar)} noValidate>
        <h1>Arquitectos</h1>
        <p className="login-ayuda">Ingrese con su usuario.</p>
        <Form.Group className="mb-3" controlId="usuario">
          <Form.Label>Usuario</Form.Label>
          <Form.Control
            type="text"
            autoComplete="username"
            isInvalid={Boolean(errors.usuario)}
            {...register('usuario')}
          />
          <Form.Control.Feedback type="invalid">{errors.usuario?.message}</Form.Control.Feedback>
        </Form.Group>
        <Form.Group className="mb-4" controlId="password">
          <Form.Label>Contraseña</Form.Label>
          <Form.Control
            type="password"
            autoComplete="current-password"
            isInvalid={Boolean(errors.password)}
            {...register('password')}
          />
          <Form.Control.Feedback type="invalid">{errors.password?.message}</Form.Control.Feedback>
        </Form.Group>
        <Button type="submit" className="w-100" disabled={isSubmitting}>
          {isSubmitting ? 'Ingresando…' : 'Ingresar'}
        </Button>
      </Form>
    </main>
  )
}
