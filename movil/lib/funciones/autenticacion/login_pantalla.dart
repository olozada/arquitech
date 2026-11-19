import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/aplicacion.dart';
import '../../app/formulario_adaptable.dart';
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
    final formulario = AutofillGroup(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          AvisoTexto(controlador.aviso),
          TextField(
            key: const Key('usuario'),
            controller: _usuario,
            enabled: !controlador.ocupado,
            decoration: const InputDecoration(
              labelText: 'Usuario',
              prefixIcon: Icon(Icons.person_outline),
            ),
            autocorrect: false,
            textInputAction: TextInputAction.next,
            autofillHints: const [AutofillHints.username],
          ),
          const SizedBox(height: 16),
          CampoContrasena(
            key: const Key('contrasena'),
            controller: _contrasena,
            etiqueta: 'Contraseña',
            habilitado: !controlador.ocupado,
            textInputAction: TextInputAction.done,
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
    );
    return Scaffold(
      body: LayoutBuilder(
        builder: (context, restricciones) {
          final tamano = TamanoVentana.desdeAncho(restricciones.maxWidth);
          if (tamano == TamanoVentana.expandido) {
            return Row(
              children: [
                const Expanded(child: _PanelMarca()),
                Expanded(
                  child: CuerpoFormulario(
                    anchoMaximo: 420,
                    centrarVertical: true,
                    children: [
                      Text(
                        'Iniciar sesión',
                        style: Theme.of(context).textTheme.headlineSmall,
                      ),
                      const SizedBox(height: 24),
                      formulario,
                    ],
                  ),
                ),
              ],
            );
          }
          return CuerpoFormulario(
            anchoMaximo: 420,
            centrarVertical: true,
            children: [
              const _Encabezado(),
              const SizedBox(height: 32),
              formulario,
            ],
          );
        },
      ),
    );
  }
}

const _descripcion =
    'Inicie sesión para descargar sus actas. Hace falta conexión.';

class _Encabezado extends StatelessWidget {
  const _Encabezado();

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    return Column(
      children: [
        Icon(Icons.architecture, size: 56, color: tema.colorScheme.primary),
        const SizedBox(height: 12),
        Text(
          'Arquitectos',
          textAlign: TextAlign.center,
          style: tema.textTheme.headlineMedium?.copyWith(
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          _descripcion,
          textAlign: TextAlign.center,
          style: tema.textTheme.bodyMedium?.copyWith(
            color: tema.colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}

class _PanelMarca extends StatelessWidget {
  const _PanelMarca();

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    return ColoredBox(
      color: esquema.primaryContainer,
      child: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(48),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(
                    Icons.architecture,
                    size: 72,
                    color: esquema.onPrimaryContainer,
                  ),
                  const SizedBox(height: 24),
                  Text(
                    'Arquitectos',
                    style: tema.textTheme.displaySmall?.copyWith(
                      color: esquema.onPrimaryContainer,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    _descripcion,
                    style: tema.textTheme.titleMedium?.copyWith(
                      color: esquema.onPrimaryContainer,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
