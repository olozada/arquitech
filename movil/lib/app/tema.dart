import 'package:material_ui/material_ui.dart';

ThemeData temaArquitectos() {
  final esquema = ColorScheme.fromSeed(seedColor: const Color(0xFF0B5F4C));
  return ThemeData(
    colorScheme: esquema,
    useMaterial3: true,
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(48)),
    ),
    inputDecorationTheme: const InputDecorationTheme(
      constraints: BoxConstraints(minHeight: 48),
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
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Text(
        texto!,
        key: const Key('aviso'),
        style: TextStyle(color: Theme.of(context).colorScheme.error),
      ),
    );
  }
}
