import { Suspense, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, Outlet } from 'react-router-dom'
import { obtenerMenu } from '../api/menu'
import { useSesion } from '../auth/contextoSesion'
import { MenuLateral } from './MenuLateral'

export function Layout() {
  const { sesion, cerrarSesion } = useSesion()
  const [menuAbierto, setMenuAbierto] = useState(false)
  const menu = useQuery({
    queryKey: ['menu'],
    queryFn: obtenerMenu,
  })

  return (
    <div className="app-shell">
      <aside className={menuAbierto ? 'sidebar abierto' : 'sidebar'}>
        <div className="sidebar-marca">Arquitectos</div>
        <nav aria-label="Menú principal">
          {menu.isPending && <p className="menu-estado">Cargando menú…</p>}
          {menu.isError && <p className="menu-estado">No se pudo cargar el menú.</p>}
          {menu.data && (
            <MenuLateral opciones={menu.data} alNavegar={() => setMenuAbierto(false)} />
          )}
        </nav>
      </aside>
      {menuAbierto && (
        <button
          type="button"
          className="sidebar-fondo"
          aria-label="Cerrar menú"
          onClick={() => setMenuAbierto(false)}
        />
      )}
      <div className="principal">
        <header className="cabecera">
          <button
            type="button"
            className="boton-menu"
            aria-label="Abrir menú"
            onClick={() => setMenuAbierto(true)}
          >
            <i className="fa fa-bars" aria-hidden="true" />
          </button>
          <Link className="cabecera-enlace" to="/cuenta/contrasena">
            Contraseña
          </Link>
          {sesion?.tipoUsuario === 'Administrador' && (
            <Link className="cabecera-enlace" to="/admin/usuarios">
              Usuarios
            </Link>
          )}
          <div className="cabecera-usuario">
            <strong>{sesion?.nombre}</strong>
            <span>{sesion?.tipoUsuario}</span>
          </div>
          <button type="button" className="btn btn-outline-secondary btn-sm" onClick={cerrarSesion}>
            Salir
          </button>
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
