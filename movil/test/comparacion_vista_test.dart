import 'package:arquitectos_movil/funciones/pendientes/comparacion_fallida.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('muestra filas local y servidor', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: ComparacionFallidaVista(
          datos: ComparacionFallida(
            localDescripcion: 'Local',
            servidorDescripcion: 'Servidor',
          ),
        ),
      ),
    );
    expect(find.textContaining('Local: Local'), findsOneWidget);
    expect(find.textContaining('Servidor: Servidor'), findsOneWidget);
  });
}
