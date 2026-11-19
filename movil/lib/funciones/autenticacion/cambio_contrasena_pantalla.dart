import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/aplicacion.dart';
import '../../app/formulario_adaptable.dart';
import '../../app/tema.dart';

class CambioContrasenaPantalla extends ConsumerStatefulWidget {
  const CambioContrasenaPantalla({super.key});

  @override
  ConsumerState<CambioContrasenaPantalla> createState() => _EstadoCambio();
}

class _EstadoCambio extends ConsumerState<CambioContrasenaPantalla> {
  final _actual = TextEditingController();
  final _nueva = TextEditingController();
  final _confirmacion = TextEditingController();

  @override
  void dispose() {
    _actual.dispose();
    _nueva.dispose();
    _confirmacion.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final controlador = ref.watch(controladorProvider);
    final tema = Theme.of(context);
    void guardar() => controlador.cambiarContrasena(
      _actual.text,
      _nueva.text,
      _confirmacion.text,
    );
    return Scaffold(
      appBar: AppBar(title: const Text('Cambiar contraseña')),
      body: CuerpoFormulario(
        anchoMaximo: 480,
        children: [
          if (controlador.debeCambiar)
            Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: Text(
                'Debe cambiar la contraseña para continuar.',
                style: tema.textTheme.bodyLarge,
              ),
            ),
          AvisoTexto(controlador.aviso),
          AutofillGroup(
            child: SeccionFormulario(
              titulo: 'Contraseña',
              icono: Icons.password,
              espacio: 16,
              children: [
                CampoContrasena(
                  key: const Key('contrasena-actual'),
                  controller: _actual,
                  etiqueta: 'Contraseña actual *',
                  textInputAction: TextInputAction.next,
                  autofillHints: const [AutofillHints.password],
                ),
                CampoContrasena(
                  key: const Key('contrasena-nueva'),
                  controller: _nueva,
                  etiqueta: 'Contraseña nueva *',
                  textInputAction: TextInputAction.next,
                  autofillHints: const [AutofillHints.newPassword],
                ),
                CampoContrasena(
                  key: const Key('contrasena-confirmacion'),
                  controller: _confirmacion,
                  etiqueta: 'Confirmar contraseña *',
                  textInputAction: TextInputAction.done,
                  autofillHints: const [AutofillHints.newPassword],
                  onSubmitted: controlador.ocupado ? null : (_) => guardar(),
                ),
              ],
            ),
          ),
          const SizedBox(height: 8),
          FilledButton(
            key: const Key('guardar-contrasena'),
            onPressed: controlador.ocupado ? null : guardar,
            child: Text(controlador.ocupado ? 'Guardando…' : 'Guardar'),
          ),
        ],
      ),
    );
  }
}
