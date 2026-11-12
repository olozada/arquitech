import { useState, createElement } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import type { OpcionMenu } from '../api/menu'
import { ChevronDown, ChevronUp, iconoDeMenu } from './iconosMenu'

type Propiedades = {
  opciones: OpcionMenu[]
  alNavegar: () => void
}

export function MenuLateral({ opciones, alNavegar }: Propiedades) {
  return (
    <ul className="menu-lista">
      {opciones.map((opcion) => (
        <li key={opcion.id}>
          <Nodo opcion={opcion} alNavegar={alNavegar} />
        </li>
      ))}
    </ul>
  )
}

function Nodo({ opcion, alNavegar }: { opcion: OpcionMenu; alNavegar: () => void }) {
  const ubicacion = useLocation()
  const hijos = opcion.hijos ?? []
  const activo = hijos.some((hijo) => contieneRuta(hijo, ubicacion.pathname))
  const [abierto, setAbierto] = useState(activo)

  if (hijos.length > 0) {
    return (
      <div className="menu-grupo">
        <button
          type="button"
          className={activo ? 'menu-titulo activo' : 'menu-titulo'}
          aria-expanded={abierto}
          onClick={() => setAbierto((valor) => !valor)}
        >
          <Icono nombre={opcion.icono} id={opcion.id} />
          <span>{opcion.descripcion}</span>
          {abierto ? <ChevronUp size={16} aria-hidden="true" /> : <ChevronDown size={16} aria-hidden="true" />}
        </button>
        {abierto && <MenuLateral opciones={hijos} alNavegar={alNavegar} />}
      </div>
    )
  }

  if (!opcion.url) {
    return (
      <span className="menu-titulo">
        <Icono nombre={opcion.icono} id={opcion.id} />
        <span>{opcion.descripcion}</span>
      </span>
    )
  }

  return (
    <NavLink
      to={opcion.url}
      end={opcion.url === '/'}
      className={({ isActive }) => (isActive ? 'menu-enlace activo' : 'menu-enlace')}
      onClick={alNavegar}
    >
      <Icono nombre={opcion.icono} id={opcion.id} />
      <span>{opcion.descripcion}</span>
    </NavLink>
  )
}

function contieneRuta(opcion: OpcionMenu, ruta: string): boolean {
  if (opcion.url === ruta) {
    return true
  }

  return (opcion.hijos ?? []).some((hijo) => contieneRuta(hijo, ruta))
}

function Icono({ nombre, id }: { nombre: string | null; id: number }) {
  const icono = iconoDeMenu(nombre, id)
  if (!icono) {
    return null
  }

  return createElement(icono, { size: 18, 'aria-hidden': true })
}
