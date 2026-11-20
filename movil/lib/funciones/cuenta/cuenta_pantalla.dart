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
    final tema = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Cuenta')),
      body: CuerpoFormulario(
        anchoMaximo: 480,
        children: [
          Text(
            usuario?.nombre ?? '',
            style: tema.textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            usuario == null ? '' : nombrePerfil(usuario.tipoUsuarioId),
            style: tema.textTheme.bodyMedium?.copyWith(
              color: tema.colorScheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: 16),
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
