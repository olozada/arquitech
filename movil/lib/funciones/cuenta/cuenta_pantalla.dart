import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/aplicacion.dart';
import '../../app/formulario_adaptable.dart';
import '../../core/db/consultas.dart';
import 'cerrar_sesion.dart';

class CuentaPantalla extends ConsumerWidget {
  const CuentaPantalla({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controlador = ref.watch(controladorProvider);
    final usuario = controlador.usuario;
    return Scaffold(
      appBar: AppBar(title: const Text('Cuenta')),
      body: CuerpoFormulario(
        anchoMaximo: 480,
        children: [
          Text(usuario?.nombre ?? ''),
          const SizedBox(height: 8),
          Text(usuario == null ? '' : nombrePerfil(usuario.tipoUsuarioId)),
          const SizedBox(height: 8),
          Text('Dispositivo: ${controlador.dispositivoId ?? ''}'),
          const SizedBox(height: 8),
          Text(
            'Versión de la app: ${controlador.dependencias.configuracion.versionAppRegistro}',
          ),
          const SizedBox(height: 8),
          Text(
            'Esquema: ${controlador.dependencias.configuracion.versionEsquema}',
          ),
          const SizedBox(height: 24),
          FilledButton(
            key: const Key('cerrar-sesion'),
            onPressed: controlador.ocupado
                ? null
                : () => confirmarCerrarSesion(context, controlador),
            child: const Text('Cerrar sesión'),
          ),
        ],
      ),
    );
  }
}
