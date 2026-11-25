import { Suspense, useState, type FormEvent } from 'react'
import { useQuery } from '@tanstack/react-query'
import { LogOut, Search } from 'lucide-react'
import { Link, Outlet, useNavigate } from 'react-router-dom'
import { obtenerMenu } from '../api/menu'
import { useSesion } from '../auth/contextoSesion'
import { MenuLateral } from './MenuLateral'
import { Menu as IconoMenu } from './iconosMenu'

const marca = `${import.meta.env.BASE_URL}marca/logo.svg`

export function Layout() {
  const { sesion, cerrarSesion } = useSesion()
  const navegar = useNavigate()
  const [menuAbierto, setMenuAbierto] = useState(false)
  const [consulta, setConsulta] = useState('')
  const menu = useQuery({
    queryKey: ['menu'],
    queryFn: obtenerMenu,
  })
  const fecha = new Intl.DateTimeFormat('es-CO', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format(new Date())

  function buscar(evento: FormEvent) {
    evento.preventDefault()
    const texto = consulta.trim()
    if (!texto) {
      return
    }

    setMenuAbierto(false)
    void navegar(`/actas?numero=${encodeURIComponent(texto)}`)
  }

  return (
    <div className="app-shell">
      <aside className={menuAbierto ? 'sidebar abierto' : 'sidebar'}>
        <div className="sidebar-marca">
          <img src={marca} alt="" />
          <div>
            <strong>Arquitech</strong>
            <small>Construcción inteligente.</small>
          </div>
        </div>
        <p className="sidebar-seccion">Módulos de Gestión</p>
        <nav aria-label="Menú principal">
          {menu.isPending && <p className="menu-estado">Cargando menú…</p>}
          {menu.isError && <p className="menu-estado">No se pudo cargar el menú.</p>}
          {menu.data && <MenuLateral opciones={menu.data} alNavegar={() => setMenuAbierto(false)} />}
        </nav>
        <div className="sidebar-cuenta">
          <Link to="/cuenta/contrasena" onClick={() => setMenuAbierto(false)}>
            Contraseña
          </Link>
          {sesion?.tipoUsuario === 'Administrador' && (
            <>
              <Link to="/admin/usuarios" onClick={() => setMenuAbierto(false)}>
                Usuarios
              </Link>
              <Link to="/admin/auditoria" onClick={() => setMenuAbierto(false)}>
                Historial
              </Link>
            </>
          )}
        </div>
        <div className="sidebar-usuario">
          <span className="sidebar-avatar" aria-hidden="true">
            {iniciales(sesion?.nombre ?? '')}
          </span>
          <div>
            <strong>{sesion?.nombre}</strong>
            <span>{sesion?.tipoUsuario}</span>
          </div>
          <button type="button" className="sidebar-salir" aria-label="Cerrar sesión" onClick={cerrarSesion}>
            <LogOut size={16} />
          </button>
        </div>
      </aside>
      {menuAbierto && (
        <button type="button" className="sidebar-fondo" aria-label="Cerrar menú" onClick={() => setMenuAbierto(false)} />
      )}
      <div className="principal">
        <header className="cabecera">
          <button type="button" className="boton-menu" aria-label="Abrir menú" onClick={() => setMenuAbierto(true)}>
            <IconoMenu size={20} aria-hidden="true" />
          </button>
          <p className="cabecera-fecha">{fecha}</p>
          <form className="cabecera-buscar" onSubmit={buscar}>
            <Search size={16} aria-hidden="true" />
            <input
              type="search"
              value={consulta}
              placeholder="Buscar acta, obra o presupuesto"
              aria-label="Buscar acta, obra o presupuesto"
              onChange={(evento) => setConsulta(evento.target.value)}
            />
          </form>
        </header>
        <main className="pagina">
          <Suspense fallback={<p className="cargando-pagina">Cargando…</p>}>
            <Outlet />
          </Suspense>
        </main>
      </div>
    </div>
  )
}

function iniciales(nombre: string) {
  const partes = nombre.trim().split(/\s+/).filter(Boolean)
  return partes
    .slice(0, 2)
    .map((parte) => parte[0]?.toLocaleUpperCase('es') ?? '')
    .join('')
}
