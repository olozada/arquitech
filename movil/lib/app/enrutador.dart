import 'package:material_ui/material_ui.dart';
import 'package:go_router/go_router.dart';

import '../funciones/actas/detalle_acta.dart';
import '../funciones/actas/formulario_linea.dart';
import '../funciones/actas/lista_actas.dart';
import '../funciones/pendientes/pendientes_pantalla.dart';
import '../sincronizacion/modelos.dart';
import '../funciones/autenticacion/cambio_contrasena_pantalla.dart';
import '../funciones/autenticacion/desbloqueo_pantalla.dart';
import '../funciones/autenticacion/login_pantalla.dart';
import '../funciones/cuenta/cuenta_pantalla.dart';
import '../funciones/inicio/inicio_pantalla.dart';
import '../funciones/presupuestos/detalle_presupuesto.dart';
import '../funciones/presupuestos/lista_presupuestos.dart';
import 'controlador.dart';

GoRouter crearEnrutador(ControladorAplicacion controlador) {
  return GoRouter(
    initialLocation: '/carga',
    refreshListenable: controlador.rutas,
    redirect: (context, state) {
      final destino = switch (controlador.ruta) {
        RutaAplicacion.cargando => '/carga',
        RutaAplicacion.login => '/login',
        RutaAplicacion.desbloqueo => '/desbloqueo',
        RutaAplicacion.cambiarContrasena => '/cambiar-contrasena',
        RutaAplicacion.actualice => '/actualice',
        RutaAplicacion.inicio => null,
      };
      final lugar = state.matchedLocation;
      if (destino == null) {
        const publicas = {
          '/login',
          '/desbloqueo',
          '/cambiar-contrasena',
          '/carga',
          '/actualice',
        };
        if (publicas.contains(lugar)) {
          return '/inicio';
        }
        return null;
      }
      if (lugar != destino) {
        return destino;
      }
      return null;
    },
    routes: [
      GoRoute(path: '/carga', builder: (context, state) => const _Carga()),
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginPantalla(),
      ),
      GoRoute(
        path: '/desbloqueo',
        builder: (context, state) => const DesbloqueoPantalla(),
      ),
      GoRoute(
        path: '/cambiar-contrasena',
        builder: (context, state) => const CambioContrasenaPantalla(),
      ),
      GoRoute(
        path: '/actualice',
        builder: (context, state) => const _Actualice(),
      ),
      GoRoute(
        path: '/inicio',
        builder: (context, state) => const InicioPantalla(),
      ),
      GoRoute(
        path: '/actas',
        builder: (context, state) => const ListaActasPantalla(),
      ),
      GoRoute(
        path: '/actas/:numero',
        builder: (context, state) => DetalleActaPantalla(
          numero: int.parse(state.pathParameters['numero']!),
        ),
        routes: [
          GoRoute(
            path: 'linea/nueva',
            builder: (context, state) => FormularioLineaPantalla(
              numeroActa: int.parse(state.pathParameters['numero']!),
              inicial: state.extra is LineaSincronizacion
                  ? state.extra! as LineaSincronizacion
                  : null,
            ),
          ),
          GoRoute(
            path: 'linea/:localId',
            builder: (context, state) => FormularioLineaPantalla(
              numeroActa: int.parse(state.pathParameters['numero']!),
              localId: state.pathParameters['localId'],
              inicial: state.extra is LineaSincronizacion
                  ? state.extra! as LineaSincronizacion
                  : null,
            ),
          ),
        ],
      ),
      GoRoute(
        path: '/pendientes',
        builder: (context, state) => const PendientesPantalla(),
      ),
      GoRoute(
        path: '/presupuestos',
        builder: (context, state) => const ListaPresupuestosPantalla(),
        routes: [
          GoRoute(
            path: ':id',
            builder: (context, state) => DetallePresupuestoPantalla(
              id: int.parse(state.pathParameters['id']!),
            ),
          ),
        ],
      ),
      GoRoute(
        path: '/cuenta',
        builder: (context, state) => const CuentaPantalla(),
      ),
    ],
  );
}

class _Carga extends StatelessWidget {
  const _Carga();

  @override
  Widget build(BuildContext context) {
    return const Scaffold(body: Center(child: Text('Abriendo Arquitectos…')));
  }
}

class _Actualice extends StatelessWidget {
  const _Actualice();

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: EdgeInsets.all(24),
            child: Text(
              'Actualice la aplicación.',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 22),
            ),
          ),
        ),
      ),
    );
  }
}
