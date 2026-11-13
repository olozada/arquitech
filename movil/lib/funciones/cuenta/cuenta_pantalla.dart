import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/aplicacion.dart';
import '../../core/db/consultas.dart';

class CuentaPantalla extends ConsumerWidget {
  const CuentaPantalla({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controlador = ref.watch(controladorProvider);
    final usuario = controlador.usuario;
    return Scaffold(
      appBar: AppBar(title: const Text('Cuenta')),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          Text(usuario?.nombre ?? ''),
          const SizedBox(height: 8),
          Text(usuario == null ? '' : nombrePerfil(usuario.tipoUsuarioId)),
          const SizedBox(height: 8),
          Text('Dispositivo: ${controlador.dispositivoId ?? ''}'),
          const SizedBox(height: 8),
          Text(
            'Versión de la app: ${controlador.dependencias.configuracion.versionApp}',
          ),
          const SizedBox(height: 8),
          Text(
            'Esquema: ${controlador.dependencias.configuracion.versionEsquema}',
          ),
          const SizedBox(height: 24),
          FilledButton(
            onPressed: controlador.ocupado
                ? null
                : () async {
                    if (controlador.pendientes > 0) {
                      final continuar = await showDialog<bool>(
                        context: context,
                        builder: (context) => AlertDialog(
                          title: const Text('Cerrar sesión'),
                          content: const Text(
                            'Hay operaciones pendientes. Si cierra sesión, se perderán.',
                          ),
                          actions: [
                            TextButton(
                              onPressed: () => Navigator.pop(context, false),
                              child: const Text('Cancelar'),
                            ),
                            TextButton(
                              onPressed: () => Navigator.pop(context, true),
                              child: const Text('Cerrar sesión'),
                            ),
                          ],
                        ),
                      );
                      if (continuar != true) {
                        return;
                      }
                    }
                    await controlador.cerrarSesion();
                  },
            child: const Text('Cerrar sesión'),
          ),
        ],
      ),
    );
  }
}
