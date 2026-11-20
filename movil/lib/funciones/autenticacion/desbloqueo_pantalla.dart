import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/aplicacion.dart';
import '../../app/pantalla_marca.dart';
import '../../app/tema.dart';
import '../../core/decimal/dinero.dart';
import '../cuenta/cerrar_sesion.dart';

class DesbloqueoPantalla extends ConsumerWidget {
  const DesbloqueoPantalla({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controlador = ref.watch(controladorProvider);
    final ultima = controlador.ultimaSincronizacion;
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    return PantallaMarca(
      children: [
        const TituloMarca(
          titulo: 'Sincronización',
          subtitulo:
              'Consulta el estado de tu información y mantente actualizado.',
        ),
        MergeSemantics(
          child: Row(
            children: [
              Expanded(
                child: Text(
                  'Última sincronización:',
                  style: tema.textTheme.bodyMedium,
                ),
              ),
              const SizedBox(width: 12),
              Text(
                ultima == null ? 'Sin sincronizar' : fechaVisible(ultima),
                style: tema.textTheme.bodyMedium?.copyWith(
                  fontWeight: FontWeight.w500,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 28),
        AvisoTexto(controlador.aviso),
        FilledButton(
          key: const Key('desbloquear'),
          onPressed: controlador.ocupado ? null : controlador.desbloquear,
          child: const Text('Desbloquear'),
        ),
        const SizedBox(height: 12),
        FilledButton(
          key: const Key('cerrar-sesion-bloqueo'),
          style: FilledButton.styleFrom(
            backgroundColor: esquema.inverseSurface,
            foregroundColor: esquema.onInverseSurface,
            textStyle: tema.textTheme.labelLarge?.copyWith(
              fontSize: 16,
              fontWeight: FontWeight.w400,
            ),
          ),
          onPressed: controlador.ocupado
              ? null
              : () => confirmarCerrarSesion(context, controlador),
          child: const Text('Cerrar sesión'),
        ),
      ],
    );
  }
}
