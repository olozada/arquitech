import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/aplicacion.dart';
import '../../app/tema.dart';
import '../../core/errores/mensajes.dart';

class LoginPantalla extends ConsumerStatefulWidget {
  const LoginPantalla({super.key});

  @override
  ConsumerState<LoginPantalla> createState() => _EstadoLogin();
}

class _EstadoLogin extends ConsumerState<LoginPantalla> {
  final _usuario = TextEditingController();
  final _contrasena = TextEditingController();

  @override
  void dispose() {
    _usuario.dispose();
    _contrasena.dispose();
    super.dispose();
  }

  Future<void> _entrar({bool reemplazar = false}) async {
    final controlador = ref.read(controladorProvider);
    await controlador.entrar(
      _usuario.text,
      _contrasena.text,
      reemplazarUsuario: reemplazar,
    );
    if (!mounted || !controlador.requiereConfirmacionCambio || reemplazar) {
      return;
    }
    final continuar = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cambiar de usuario'),
        content: const Text(Mensajes.cambioUsuario),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancelar'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Continuar'),
          ),
        ],
      ),
    );
    if (continuar == true && mounted) {
      await controlador.entrar(
        _usuario.text,
        _contrasena.text,
        reemplazarUsuario: true,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final controlador = ref.watch(controladorProvider);
    return Scaffold(
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            const SizedBox(height: 32),
            Text(
              'Arquitectos',
              style: Theme.of(context).textTheme.headlineMedium,
            ),
            const SizedBox(height: 8),
            const Text(
              'Inicie sesión para descargar sus actas. Hace falta conexión.',
            ),
            const SizedBox(height: 24),
            AvisoTexto(controlador.aviso),
            TextField(
              key: const Key('usuario'),
              controller: _usuario,
              decoration: const InputDecoration(labelText: 'Usuario'),
              textInputAction: TextInputAction.next,
              autofillHints: const [AutofillHints.username],
            ),
            const SizedBox(height: 12),
            TextField(
              key: const Key('contrasena'),
              controller: _contrasena,
              decoration: const InputDecoration(labelText: 'Contraseña'),
              obscureText: true,
              autofillHints: const [AutofillHints.password],
              onSubmitted: (_) => _entrar(),
            ),
            const SizedBox(height: 24),
            FilledButton(
              key: const Key('entrar'),
              onPressed: controlador.ocupado ? null : () => _entrar(),
              child: Text(controlador.ocupado ? 'Entrando…' : 'Entrar'),
            ),
          ],
        ),
      ),
    );
  }
}
