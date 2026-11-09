export type Sesion = {
  token: string
  nombre: string
  tipoUsuario: string
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

    return valor
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
  return Boolean(sesion.token && sesion.nombre && sesion.tipoUsuario)
}
