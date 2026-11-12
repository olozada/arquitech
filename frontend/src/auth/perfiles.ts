export const perfilAdministrador = 1
export const perfilArquitecto = 2
export const perfilContratista = 3
export const perfilRevisor = 4

export function esAdministrador(tipoUsuario: string | undefined) {
  return tipoUsuario === 'Administrador'
}

export function esRevisor(tipoUsuario: string | undefined) {
  return tipoUsuario === 'Revisor'
}

export function perfilDeNuevoItem(tipoUsuario: string | undefined) {
  if (tipoUsuario === 'Contratista') {
    return { id: perfilContratista, nombre: 'Contratista' }
  }

  if (tipoUsuario === 'Revisor') {
    return { id: perfilRevisor, nombre: 'Revisor' }
  }

  return { id: perfilArquitecto, nombre: 'Arquitecto' }
}
