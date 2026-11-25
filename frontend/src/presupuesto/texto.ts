/** Quita tildes y pasa a minúsculas para búsquedas. */
export function normalizar(texto: string) {
  return texto
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
}