import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/aplicacion.dart';
import '../../app/formulario_adaptable.dart';
import '../../app/pantalla_marca.dart';
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
    return PantallaMarca(
      children: [
        const TituloMarca(
          titulo: 'Iniciar sesión',
          subtitulo:
              'Accede a tu cuenta y descubre todo lo que tenemos para ti.',
        ),
        AvisoTexto(controlador.aviso),
        AutofillGroup(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [
              CampoEtiquetado(
                etiqueta: 'Usuario',
                child: TextField(
                  key: const Key('usuario'),
                  controller: _usuario,
                  enabled: !controlador.ocupado,
                  decoration: const InputDecoration(
                    hintText: 'Ingresa tu usuario',
                  ),
                  autocorrect: false,
                  textInputAction: TextInputAction.next,
                  autofillHints: const [AutofillHints.username],
                ),
              ),
              const SizedBox(height: 16),
              CampoEtiquetado(
                etiqueta: 'Contraseña',
                // El campo tiene su propio botón para mostrar la contraseña.
                fusionarSemantica: false,
                child: CampoContrasena(
                  key: const Key('contrasena'),
                  controller: _contrasena,
                  pista: 'Introduce tu contraseña',
                  habilitado: !controlador.ocupado,
                  textInputAction: TextInputAction.done,
                  autofillHints: const [AutofillHints.password],
                  onSubmitted: (_) => _entrar(),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 28),
        FilledButton(
          key: const Key('entrar'),
          onPressed: controlador.ocupado ? null : () => _entrar(),
          child: Text(controlador.ocupado ? 'Ingresando…' : 'Ingresar'),
        ),
      ],
    );
  }
}
