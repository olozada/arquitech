import { useState } from 'react'
import { Form } from 'react-bootstrap'

/** Normaliza un token numérico: "1.234,56"→"1234.56", "12,5"→"12.5", "1.234.567"→"1234567", "0.125"→"0.125" */
function normalizarToken(tok: string) {
  if (tok.includes(',')) return tok.replace(/\./g, '').replace(',', '.')
  if ((tok.match(/\./g)?.length ?? 0) > 1) return tok.replace(/\./g, '')
  return tok
}

/**
 * Evalúa "12,5" | "=12*3,5" | "=[1.01]*1,35" | "=([2.01]+[2.03])*1,35".
 * [codigo] se resuelve con cantidadDe. Devuelve null si es inválido.
 */
export function evaluar(texto: string, cantidadDe?: (codigo: string) => number | undefined): number | null {
  let s = texto.trim()
  if (!s) return null
  if (s.startsWith('=')) s = s.slice(1)
  s = s.replace(/\[([^\]]+)\]/g, (_, cod: string) => {
    const v = cantidadDe?.(cod.trim())
    return v === undefined ? 'NaN' : `(${v})`
  })
  s = s.replace(/\d[\d.,]*/g, normalizarToken)
  if (!/^[\d+\-*/().\sNa]+$/.test(s)) return null
  try {
    const r = Function(`"use strict";return (${s})`)() as number
    return Number.isFinite(r) ? r : null
  } catch {
    return null
  }
}

function moverFoco(el: HTMLInputElement, paso: number) {
  const celdas = [...document.querySelectorAll<HTMLInputElement>(`input[data-col="${el.dataset.col}"]:not(:disabled)`)]
  celdas[celdas.indexOf(el) + paso]?.focus()
}

export function CeldaNumero({
  valor,
  col,
  disabled,
  decimales = 4,
  formula,
  cantidadDe,
  alCambiar,
}: {
  valor: number
  col: string
  disabled?: boolean
  decimales?: number
  formula?: string | null
  cantidadDe?: (codigo: string) => number | undefined
  alCambiar: (valor: number, formula: string | null) => void
}) {
  const [texto, setTexto] = useState<string | null>(null)
  const [error, setError] = useState(false)

  function confirmar() {
    if (texto === null) return
    const r = evaluar(texto, cantidadDe)
    if (r === null) {
      setError(true)
      return
    }
    setError(false)
    const f = texto.trim().startsWith('=') ? texto.trim() : null
    if (r !== valor || f !== (formula ?? null)) alCambiar(r, f)
    setTexto(null)
  }

  return (
    <Form.Control
      size="sm"
      data-col={col}
      className="text-end"
      isInvalid={error}
      disabled={disabled}
      title={formula ?? undefined}
      value={texto ?? valor.toLocaleString('es-CO', { maximumFractionDigits: decimales })}
      onFocus={(e) => {
        setTexto(formula ?? String(valor).replace('.', ','))
        const t = e.currentTarget
        requestAnimationFrame(() => t.select())
      }}
      onChange={(e) => setTexto(e.target.value)}
      onBlur={confirmar}
      onKeyDown={(e) => {
        if (e.key === 'Enter') {
          e.preventDefault()
          confirmar()
          moverFoco(e.currentTarget as HTMLInputElement, e.shiftKey ? -1 : 1)
        }
        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
          e.preventDefault()
          confirmar()
          moverFoco(e.currentTarget as HTMLInputElement, e.key === 'ArrowDown' ? 1 : -1)
        }
        if (e.key === 'Escape') {
          setTexto(null)
          setError(false)
          e.currentTarget.blur()
        }
      }}
    />
  )
}