import 'package:material_ui/material_ui.dart';

import '../core/errores/mensajes.dart';

ThemeData temaArquitectos() {
  final esquema = ColorScheme.fromSeed(seedColor: const Color(0xFF0B5F4C));
  const radio = BorderRadius.all(Radius.circular(12));
  const forma = RoundedRectangleBorder(borderRadius: radio);
  return ThemeData(
    colorScheme: esquema,
    useMaterial3: true,
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        minimumSize: const Size.fromHeight(48),
        shape: forma,
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size.fromHeight(48),
        shape: forma,
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      constraints: const BoxConstraints(minHeight: 48),
      border: const OutlineInputBorder(borderRadius: radio),
      enabledBorder: OutlineInputBorder(
        borderRadius: radio,
        borderSide: BorderSide(color: esquema.outline),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: radio,
        borderSide: BorderSide(color: esquema.primary, width: 2),
      ),
      filled: true,
      fillColor: esquema.surfaceContainerLowest,
    ),
  );
}

class AvisoTexto extends StatelessWidget {
  const AvisoTexto(this.texto, {super.key});

  final String? texto;

  @override
  Widget build(BuildContext context) {
    if (texto == null || texto!.isEmpty) {
      return const SizedBox.shrink();
    }
    final esquema = Theme.of(context).colorScheme;
    // El único aviso de éxito es «Datos actualizados»; el resto son errores.
    final exito = texto == Mensajes.datosActualizados;
    final fondo = exito ? esquema.primaryContainer : esquema.errorContainer;
    final frente = exito
        ? esquema.onPrimaryContainer
        : esquema.onErrorContainer;
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Semantics(
        liveRegion: true,
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: fondo,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(
                  exito ? Icons.check_circle_outline : Icons.error_outline,
                  size: 20,
                  color: frente,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    texto!,
                    key: const Key('aviso'),
                    style: TextStyle(color: frente),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
