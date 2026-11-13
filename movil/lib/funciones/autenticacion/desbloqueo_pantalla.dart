import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/aplicacion.dart';
import '../../app/tema.dart';
import '../../core/decimal/dinero.dart';
import '../../core/errores/mensajes.dart';

class DesbloqueoPantalla extends ConsumerWidget {
  const DesbloqueoPantalla({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controlador = ref.watch(controladorProvider);
    final ultima = controlador.ultimaSincronizacion;
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Spacer(),
              Text(
                'Arquitectos',
                style: Theme.of(context).textTheme.headlineMedium,
              ),
              const SizedBox(height: 12),
              Text(controlador.usuario?.nombre ?? 'Sesión guardada'),
              const SizedBox(height: 8),
              Text(
                ultima == null
                    ? 'Sin sincronizar'
                    : 'Última sincronización: ${fechaVisible(ultima)}',
              ),
              const SizedBox(height: 24),
              AvisoTexto(controlador.aviso),
              FilledButton(
                key: const Key('desbloquear'),
                onPressed: controlador.ocupado ? null : controlador.desbloquear,
                child: const Text('Desbloquear'),
              ),
              if (controlador.aviso == Mensajes.requiereBloqueo) ...[
                const SizedBox(height: 12),
                OutlinedButton(
                  key: const Key('cerrar-sesion-bloqueo'),
                  onPressed: controlador.ocupado
                      ? null
                      : controlador.cerrarSesion,
                  child: const Text('Cerrar sesión'),
                ),
              ],
              const Spacer(),
            ],
          ),
        ),
      ),
    );
  }
}
