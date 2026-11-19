import 'package:material_ui/material_ui.dart';

import '../../app/controlador.dart';

Future<void> confirmarCerrarSesion(
  BuildContext context,
  ControladorAplicacion controlador,
) async {
  if (controlador.ocupado) {
    return;
  }
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
}
