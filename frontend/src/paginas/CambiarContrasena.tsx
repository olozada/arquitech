import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button, Form } from 'react-bootstrap'
import Swal from 'sweetalert2'
import { z } from 'zod'
import { cliente, mensajeError } from '../api/cliente'
import { useSesion } from '../auth/contextoSesion'
import { errorContrasena } from '../auth/contrasena'

const esquema = z
  .object({
    contrasenaActual: z.string().min(1, 'Debe ingresar la contraseña actual.'),
    contrasenaNueva: z
      .string()
      .min(1, 'Debe ingresar la contraseña nueva.')
      .superRefine((valor, contexto) => {
        const mensaje = errorContrasena(valor)
        if (mensaje) {
          contexto.addIssue({ code: 'custom', message: mensaje })
        }
      }),
    confirmacion: z.string().min(1, 'Debe confirmar la contraseña.'),
  })
  .refine((valores) => valores.contrasenaNueva === valores.confirmacion, {
    path: ['confirmacion'],
    message: 'Las contraseñas no coinciden.',
  })

type Valores = z.infer<typeof esquema>

export function CambiarContrasena() {
  const { sesion, iniciarSesion } = useSesion()
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<Valores>({
    resolver: zodResolver(esquema),
    defaultValues: { contrasenaActual: '', contrasenaNueva: '', confirmacion: '' },
  })

  async function enviar(valores: Valores) {
    try {
      await cliente.post('/api/auth/cambiar-contrasena', {
        contrasenaActual: valores.contrasenaActual,
        contrasenaNueva: valores.contrasenaNueva,
      })
      if (sesion) {
        iniciarSesion({ ...sesion, debeCambiarContrasena: false })
      }
      reset()
      await Swal.fire({ icon: 'success', title: '¡Finalizado!', confirmButtonColor: '#1abb9c' })
    } catch (error) {
      await Swal.fire({
        icon: 'error',
        title: '¡Error!',
        text: mensajeError(error),
        confirmButtonColor: '#1abb9c',
      })
    }
  }

  return (
    <section className="catalogo">
      <h1>Cambiar contraseña</h1>
      <Form className="catalogo-panel" onSubmit={handleSubmit(enviar)} noValidate>
        <Form.Group className="mb-3" controlId="contrasena-actual">
          <Form.Label>Contraseña actual *</Form.Label>
          <Form.Control
            type="password"
            autoComplete="current-password"
            isInvalid={Boolean(errors.contrasenaActual)}
            {...register('contrasenaActual')}
          />
          <Form.Control.Feedback type="invalid">{errors.contrasenaActual?.message}</Form.Control.Feedback>
        </Form.Group>
        <Form.Group className="mb-3" controlId="contrasena-nueva">
          <Form.Label>Contraseña nueva *</Form.Label>
          <Form.Control
            type="password"
            autoComplete="new-password"
            isInvalid={Boolean(errors.contrasenaNueva)}
            {...register('contrasenaNueva')}
          />
          <Form.Control.Feedback type="invalid">{errors.contrasenaNueva?.message}</Form.Control.Feedback>
        </Form.Group>
        <Form.Group className="mb-3" controlId="contrasena-confirmacion">
          <Form.Label>Confirmar contraseña *</Form.Label>
          <Form.Control
            type="password"
            autoComplete="new-password"
            isInvalid={Boolean(errors.confirmacion)}
            {...register('confirmacion')}
          />
          <Form.Control.Feedback type="invalid">{errors.confirmacion?.message}</Form.Control.Feedback>
        </Form.Group>
        <Button type="submit" disabled={isSubmitting}>
          Guardar
        </Button>
      </Form>
    </section>
  )
}
