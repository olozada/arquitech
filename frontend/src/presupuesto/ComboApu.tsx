import { useEffect, useMemo, useRef, useState } from 'react'
import { Form } from 'react-bootstrap'
import { dinero } from '../actas/formato'
import { normalizar } from './texto'

export type ApuOpcion = {
  id: number
  codigo: string | null
  descripcion: string
  unidad: string
  costoTotal?: number
  unidadMedidaId?: number | null
}

export function ComboApu({
  valor,
  apus,
  disabled,
  alElegir,
}: {
  valor: number | null
  apus: ApuOpcion[]
  disabled?: boolean
  alElegir: (apu: ApuOpcion | null) => void
}) {
  const [texto, setTexto] = useState('')
  const [abierto, setAbierto] = useState(false)
  const [indice, setIndice] = useState(0)
  const contenedor = useRef<HTMLDivElement>(null)

  const seleccionado = valor != null && valor > 0 ? apus.find((fila) => fila.id === valor) : null

  const filtrados = useMemo(() => {
    const clave = normalizar(texto.trim())
    if (!clave) return apus.slice(0, 15)
    return apus
      .filter((fila) => {
        const etiqueta = normalizar(`${fila.codigo ?? ''} ${fila.descripcion} ${fila.unidad}`)
        return etiqueta.includes(clave)
      })
      .slice(0, 15)
  }, [apus, texto])

  useEffect(() => {
    function cerrar(evento: MouseEvent) {
      if (contenedor.current && !contenedor.current.contains(evento.target as Node)) {
        setAbierto(false)
      }
    }
    document.addEventListener('mousedown', cerrar)
    return () => document.removeEventListener('mousedown', cerrar)
  }, [])

  function elegir(apu: ApuOpcion | null) {
    alElegir(apu)
    setTexto(apu ? `${apu.codigo ? `${apu.codigo} ` : ''}${apu.descripcion}` : '')
    setAbierto(false)
  }

  const etiqueta =
    seleccionado && !abierto
      ? `${seleccionado.codigo ? `${seleccionado.codigo} ` : ''}${seleccionado.descripcion}`
      : texto

  return (
    <div ref={contenedor} className="position-relative">
      <Form.Control
        size="sm"
        data-col="apu"
        disabled={disabled}
        value={etiqueta}
        placeholder="Valor directo"
        onFocus={() => {
          setAbierto(true)
          setIndice(0)
          if (seleccionado) {
            setTexto(`${seleccionado.codigo ? `${seleccionado.codigo} ` : ''}${seleccionado.descripcion}`)
          }
        }}
        onChange={(evento) => {
          setTexto(evento.target.value)
          setAbierto(true)
          setIndice(0)
          if (!evento.target.value.trim()) {
            elegir(null)
          }
        }}
        onKeyDown={(evento) => {
          if (!abierto && (evento.key === 'ArrowDown' || evento.key === 'Enter')) {
            setAbierto(true)
            return
          }
          if (evento.key === 'Escape') {
            setAbierto(false)
            evento.currentTarget.blur()
            return
          }
          if (evento.key === 'ArrowDown') {
            evento.preventDefault()
            setIndice((i) => Math.min(i + 1, filtrados.length))
            return
          }
          if (evento.key === 'ArrowUp') {
            evento.preventDefault()
            setIndice((i) => Math.max(i - 1, 0))
            return
          }
          if (evento.key === 'Enter' && abierto) {
            evento.preventDefault()
            if (indice === 0) {
              elegir(null)
            } else {
              const apu = filtrados[indice - 1]
              if (apu) elegir(apu)
            }
          }
        }}
      />
      {abierto && !disabled && (
        <div className="presupuesto-combo-lista shadow-sm border rounded bg-white">
          <button
            type="button"
            className={`presupuesto-combo-opcion w-100 text-start${indice === 0 ? ' active' : ''}`}
            onMouseDown={(e) => {
              e.preventDefault()
              elegir(null)
            }}
          >
            Valor directo
          </button>
          {filtrados.map((apu, i) => (
            <button
              key={apu.id}
              type="button"
              className={`presupuesto-combo-opcion w-100 text-start${indice === i + 1 ? ' active' : ''}`}
              onMouseDown={(e) => {
                e.preventDefault()
                elegir(apu)
              }}
            >
              <span className="text-muted small">{apu.codigo ?? '—'}</span> {apu.descripcion}{' '}
              <span className="text-secondary small">
                · {apu.unidad} · {dinero(apu.costoTotal ?? 0, 0)}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}