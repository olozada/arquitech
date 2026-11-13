import 'package:material_ui/material_ui.dart';

/// Cubre la UI en recientes/capturas (iOS y capa extra en Android).
class VeloPrivacidad extends StatelessWidget {
  const VeloPrivacidad({super.key});

  @override
  Widget build(BuildContext context) {
    final esquema = Theme.of(context).colorScheme;
    return ColoredBox(
      color: esquema.surface,
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.architecture, size: 72, color: esquema.primary),
            const SizedBox(height: 16),
            Text(
              'Arquitectos',
              style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                color: esquema.onSurface,
                fontWeight: FontWeight.w600,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
