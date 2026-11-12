import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes, useLocation, useParams } from 'react-router-dom'
import { esAdministrador } from './auth/perfiles'
import { RutaPrivada } from './auth/RutaPrivada'
import { useSesion } from './auth/contextoSesion'
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
const ListadoActas = lazy(() =>
  import('./paginas/ListadoActas').then((modulo) => ({ default: modulo.ListadoActas })),
)
const TomaActas = lazy(() => import('./paginas/TomaActas').then((modulo) => ({ default: modulo.TomaActas })))
const ReporteActas = lazy(() =>
  import('./paginas/ReporteActas').then((modulo) => ({ default: modulo.ReporteActas })),
)
const ReporteMateriales = lazy(() =>
  import('./paginas/ReporteMateriales').then((modulo) => ({ default: modulo.ReporteMateriales })),
)
const Usuarios = lazy(() => import('./paginas/Usuarios').then((modulo) => ({ default: modulo.Usuarios })))
const Auditoria = lazy(() => import('./paginas/Auditoria').then((modulo) => ({ default: modulo.Auditoria })))
const CambiarContrasena = lazy(() =>
  import('./paginas/CambiarContrasena').then((modulo) => ({ default: modulo.CambiarContrasena })),
)
const Presupuestos = lazy(() => import('./paginas/Presupuestos').then((modulo) => ({ default: modulo.Presupuestos })))
const EditorPresupuesto = lazy(() =>
  import('./paginas/EditorPresupuesto').then((modulo) => ({ default: modulo.EditorPresupuesto })),
)
const SeguimientoPresupuesto = lazy(() =>
  import('./paginas/SeguimientoPresupuesto').then((modulo) => ({ default: modulo.SeguimientoPresupuesto })),
)
const InsumosPresupuesto = lazy(() =>
  import('./paginas/InsumosPresupuesto').then((modulo) => ({ default: modulo.InsumosPresupuesto })),
)
const ManoObraPresupuesto = lazy(() =>
  import('./paginas/ManoObraPresupuesto').then((modulo) => ({ default: modulo.ManoObraPresupuesto })),
)
const Apus = lazy(() => import('./paginas/Apus').then((modulo) => ({ default: modulo.Apus })))

function Inicio() {
  const { sesion } = useSesion()
  if (!esAdministrador(sesion?.tipoUsuario)) {
    return <Navigate to="/actas" replace />
  }

  return <Dashboard />
}

function PaginaNuevaActa() {
  const { sesion } = useSesion()
  const estado = useLocation().state as { nueva?: unknown } | null
  const marca = typeof estado?.nueva === 'number' ? estado.nueva : 0
  if (!esAdministrador(sesion?.tipoUsuario)) {
    return <Navigate to="/actas" replace />
  }

  return <TomaActas key={marca || 'borrador'} />
}

function PaginaActa() {
  const { numero } = useParams()
  return <TomaActas key={numero} />
}

export function App() {
  return (
    <Suspense fallback={<p className="cargando-pagina">Cargando…</p>}>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route element={<RutaPrivada />}>
          <Route element={<Layout />}>
            <Route path="/" element={<Inicio />} />
            {catalogos.map((catalogo) => (
              <Route
                key={catalogo.recurso}
                path={catalogo.ruta}
                element={<CatalogoCrud key={catalogo.recurso} configuracion={catalogo} />}
              />
            ))}
            <Route path="admin/materiales" element={<Navigate to="/presupuesto/insumos" replace />} />
            <Route path="admin/proveedores-materiales" element={<MaterialesProveedor />} />
            <Route path="admin/usuarios" element={<Usuarios />} />
            <Route path="admin/auditoria" element={<Auditoria />} />
            <Route path="cuenta/contrasena" element={<CambiarContrasena />} />
            <Route path="actas" element={<ListadoActas />} />
            <Route path="actas/nueva" element={<PaginaNuevaActa />} />
            <Route path="actas/:numero" element={<PaginaActa />} />
            <Route path="informes/actas" element={<ReporteActas />} />
            <Route path="informes/materiales" element={<ReporteMateriales />} />
            <Route path="presupuestos" element={<Presupuestos />} />
            <Route path="presupuestos/:id/seguimiento" element={<SeguimientoPresupuesto />} />
            <Route path="presupuestos/:id" element={<EditorPresupuesto />} />
            <Route path="presupuesto/insumos" element={<InsumosPresupuesto />} />
            <Route path="presupuesto/mano-obra" element={<ManoObraPresupuesto />} />
            <Route path="presupuesto/apu/:id" element={<Apus />} />
            <Route path="presupuesto/apu" element={<Apus />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </Suspense>
  )
}
