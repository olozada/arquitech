export function errorContrasena(contrasena: string, usuario?: string): string | null {
  if (contrasena.length < 10) {
    return 'La contraseña debe tener al menos 10 caracteres.'
  }

  if (!/\p{L}/u.test(contrasena) || !/\d/.test(contrasena)) {
    return 'La contraseña debe incluir letras y números.'
  }

  if (
    usuario &&
    contrasena.localeCompare(usuario.trim(), 'es', { sensitivity: 'accent' }) === 0
  ) {
    return 'La contraseña no puede ser igual al usuario.'
  }

  return null
}
