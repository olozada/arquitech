import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { RutaPrivada } from './auth/RutaPrivada'
import { catalogos } from './catalogos/definiciones'
import { Layout } from './componentes/Layout'

const Login = lazy(() => import('./paginas/Login').then((modulo) => ({ default: modulo.Login })))
const Dashboard = lazy(() => import('./paginas/Dashboard').then((modulo) => ({ default: modulo.Dashboard })))
const CatalogoCrud = lazy(() =>
  import('./componentes/CatalogoCrud').then((modulo) => ({ default: modulo.CatalogoCrud })),
)
const MaterialesProveedor = lazy(() =>
  import('./paginas/MaterialesProveedor').then((modulo) => ({ default: modulo.MaterialesProveedor })),
)
const TomaActas = lazy(() => import('./paginas/TomaActas').then((modulo) => ({ default: modulo.TomaActas })))
const ReporteActas = lazy(() =>
  import('./paginas/ReporteActas').then((modulo) => ({ default: modulo.ReporteActas })),
)
const Usuarios = lazy(() => import('./paginas/Usuarios').then((modulo) => ({ default: modulo.Usuarios })))
const CambiarContrasena = lazy(() =>
  import('./paginas/CambiarContrasena').then((modulo) => ({ default: modulo.CambiarContrasena })),
)

export function App() {
  return (
    <Suspense fallback={<p className="cargando-pagina">Cargando…</p>}>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route element={<RutaPrivada />}>
          <Route element={<Layout />}>
            <Route path="/" element={<Dashboard />} />
            {catalogos.map((catalogo) => (
              <Route
                key={catalogo.recurso}
                path={catalogo.ruta}
                element={<CatalogoCrud key={catalogo.recurso} configuracion={catalogo} />}
              />
            ))}
            <Route path="admin/proveedores-materiales" element={<MaterialesProveedor />} />
            <Route path="admin/usuarios" element={<Usuarios />} />
            <Route path="cuenta/contrasena" element={<CambiarContrasena />} />
            <Route path="actas/nueva" element={<TomaActas />} />
            <Route path="informes/actas" element={<ReporteActas />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </Suspense>
  )
}
