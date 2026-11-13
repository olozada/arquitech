export function parcial(veces: number, largo?: number | null, ancho?: number | null, alto?: number | null) {
  return (veces || 1) * (largo ?? 1) * (ancho ?? 1) * (alto ?? 1)
}
