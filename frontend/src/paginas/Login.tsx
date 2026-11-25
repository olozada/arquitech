import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Eye, EyeOff, Shield } from 'lucide-react'
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

const marca = `${import.meta.env.BASE_URL}marca`

export function Login() {
  const { sesion, iniciarSesion } = useSesion()
  const navegar = useNavigate()
  const [visible, setVisible] = useState(false)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormularioLogin>({
    resolver: zodResolver(esquema),
    defaultValues: { usuario: '', password: '' },
  })

  if (sesion) {
    return <Navigate to={sesion.debeCambiarContrasena ? '/cuenta/contrasena' : '/'} replace />
  }

  async function enviar(valores: FormularioLogin) {
    try {
      const { data } = await cliente.post<Sesion>('/api/auth/login', valores)
      iniciarSesion(data)
      await navegar(data.debeCambiarContrasena ? '/cuenta/contrasena' : '/')
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
      <section className="login-panel" style={{ backgroundImage: `url(${marca}/obra.png)` }}>
        <div className="login-marca">
          <img src={`${marca}/logo.svg`} alt="" />
          <h1>Arquitech</h1>
          <p>Construcción inteligente.</p>
        </div>
        <div className="login-chips">
          <span>Bitácoras y actas en obra</span>
          <span>Presupuestos y seguimiento</span>
          <span>Funciona sin conexión</span>
        </div>
      </section>
      <Form className="login-formulario" onSubmit={handleSubmit(enviar)} noValidate>
        <h2>Iniciar sesión</h2>
        <p className="login-ayuda">Accede a tu cuenta y descubre todo lo que tenemos para ti.</p>
        <Form.Group className="mb-3" controlId="usuario">
          <Form.Label>Usuario</Form.Label>
          <Form.Control
            className="login-campo"
            type="text"
            autoComplete="username"
            placeholder="Ingresa tu usuario"
            isInvalid={Boolean(errors.usuario)}
            {...register('usuario')}
          />
          <Form.Control.Feedback type="invalid">{errors.usuario?.message}</Form.Control.Feedback>
        </Form.Group>
        <Form.Group className="mb-1" controlId="password">
          <Form.Label>Contraseña</Form.Label>
          <div className="login-clave">
            <Form.Control
              className="login-campo"
              type={visible ? 'text' : 'password'}
              autoComplete="current-password"
              placeholder="Introduce tu contraseña"
              isInvalid={Boolean(errors.password)}
              {...register('password')}
            />
            <button type="button" className="login-ver" aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'} onClick={() => setVisible((valor) => !valor)}>
              {visible ? <EyeOff size={18} /> : <Eye size={18} />}
            </button>
          </div>
          <Form.Control.Feedback type="invalid">{errors.password?.message}</Form.Control.Feedback>
        </Form.Group>
        <button
          type="button"
          className="login-olvido"
          onClick={() =>
            void Swal.fire({
              icon: 'info',
              title: 'Recuperar contraseña',
              text: 'Pide al administrador que restablezca tu contraseña desde Usuarios.',
            })
          }
        >
          ¿Olvidaste tu contraseña?
        </button>
        <Button type="submit" className="login-ingresar w-100" disabled={isSubmitting}>
          {isSubmitting ? 'Ingresando…' : 'Ingresar'}
        </Button>
        <p className="login-aviso">
          <Shield size={20} aria-hidden="true" />
          <span>Tus datos se guardan en el dispositivo y se sincronizan cuando hay conexión.</span>
        </p>
      </Form>
    </main>
  )
}
