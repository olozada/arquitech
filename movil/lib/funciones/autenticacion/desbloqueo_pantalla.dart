import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/aplicacion.dart';
import '../../app/formulario_adaptable.dart';
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
    return Scaffold(
      body: CuerpoFormulario(
        anchoMaximo: 420,
        centrarVertical: true,
        children: [
          Icon(Icons.lock_outline, size: 56, color: esquema.primary),
          const SizedBox(height: 12),
          Text(
            'Arquitectos',
            textAlign: TextAlign.center,
            style: tema.textTheme.headlineMedium?.copyWith(
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 12),
          Text(
            controlador.usuario?.nombre ?? 'Sesión guardada',
            textAlign: TextAlign.center,
            style: tema.textTheme.titleMedium,
          ),
          const SizedBox(height: 4),
          Text(
            ultima == null
                ? 'Sin sincronizar'
                : 'Última sincronización: ${fechaVisible(ultima)}',
            textAlign: TextAlign.center,
            style: tema.textTheme.bodyMedium?.copyWith(
              color: esquema.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: 32),
          AvisoTexto(controlador.aviso),
          FilledButton.icon(
            key: const Key('desbloquear'),
            onPressed: controlador.ocupado ? null : controlador.desbloquear,
            icon: const Icon(Icons.fingerprint),
            label: const Text('Desbloquear'),
          ),
          const SizedBox(height: 12),
          OutlinedButton(
            key: const Key('cerrar-sesion-bloqueo'),
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
