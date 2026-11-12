export type Sesion = {
  nombre: string
  tipoUsuario: string
  debeCambiarContrasena?: boolean
}

const CLAVE = 'arquitectos.sesion'

export function leerSesion(): Sesion | null {
  const texto = localStorage.getItem(CLAVE)
  if (!texto) {
    return null
  }

  try {
    const valor: unknown = JSON.parse(texto)
    if (!esSesion(valor)) {
      return null
    }

    return {
      nombre: valor.nombre,
      tipoUsuario: valor.tipoUsuario,
      debeCambiarContrasena: valor.debeCambiarContrasena,
    }
  } catch {
    return null
  }
}

export function guardarSesion(sesion: Sesion) {
  localStorage.setItem(CLAVE, JSON.stringify(sesion))
}

export function borrarSesion() {
  localStorage.removeItem(CLAVE)
}

function esSesion(valor: unknown): valor is Sesion {
  if (!valor || typeof valor !== 'object') {
    return false
  }

  const sesion = valor as Partial<Sesion>
  if (sesion.debeCambiarContrasena != null && typeof sesion.debeCambiarContrasena !== 'boolean') {
    return false
  }

  return Boolean(sesion.nombre && sesion.tipoUsuario)
}
