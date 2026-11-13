import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/aplicacion.dart';
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
    return Scaffold(
      appBar: AppBar(title: const Text('Cambiar contraseña')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            if (controlador.debeCambiar)
              const Padding(
                padding: EdgeInsets.only(bottom: 12),
                child: Text('Debe cambiar la contraseña para continuar.'),
              ),
            AvisoTexto(controlador.aviso),
            TextField(
              key: const Key('contrasena-actual'),
              controller: _actual,
              obscureText: true,
              decoration: const InputDecoration(
                labelText: 'Contraseña actual *',
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              key: const Key('contrasena-nueva'),
              controller: _nueva,
              obscureText: true,
              decoration: const InputDecoration(
                labelText: 'Contraseña nueva *',
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              key: const Key('contrasena-confirmacion'),
              controller: _confirmacion,
              obscureText: true,
              decoration: const InputDecoration(
                labelText: 'Confirmar contraseña *',
              ),
            ),
            const SizedBox(height: 24),
            FilledButton(
              key: const Key('guardar-contrasena'),
              onPressed: controlador.ocupado
                  ? null
                  : () => controlador.cambiarContrasena(
                      _actual.text,
                      _nueva.text,
                      _confirmacion.text,
                    ),
              child: Text(controlador.ocupado ? 'Guardando…' : 'Guardar'),
            ),
          ],
        ),
      ),
    );
  }
}
