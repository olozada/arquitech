import 'package:arquitectos_movil/core/db/consultas.dart';
import 'package:arquitectos_movil/funciones/actas/detalle_acta.dart';
import 'package:arquitectos_movil/funciones/actas/detalle_acta_acciones.dart';
import 'package:decimal/decimal.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_test/flutter_test.dart';

final _lineaEjemplo = LineaDetalle(
  localId: 's1',
  construccionId: 7,
  construccion: 'Cimentación',
  conceptoId: 1,
  tipoUsuarioId: 3,
  perfil: 'Contratista',
  descripcion: 'Excavación manual',
  cantidad: '2.0000',
  precio: '750.00',
  subtotal: '1500.00',
  aprobado: true,
  estadoLocal: 'sincronizado',
);

final _detalleCerrada = DetalleActa(
  numero: 120,
  fechaInicial: '2026-11-01',
  fechaFinal: '2026-11-30',
  arquitecto: 'Ana Arquitecta',
  revisor: 'Rosa Revisor',
  contratista: 'Carlos Contratista',
  observacion: 'Obra norte',
  cerrada: true,
  totalAprobado: Decimal.parse('1500'),
  lineas: [_lineaEjemplo],
);

class _SinAcciones implements DetalleActaAcciones {
  @override
  bool esRevisor() => false;

  @override
  bool puedeAgregarLinea(bool actaCerrada) => false;

  @override
  bool puedeEditarLinea(LineaDetalle linea, bool actaCerrada) => false;

  @override
  Future<void> aprobarTodasLineas(int numeroActa) async {}

  @override
  Future<void> eliminarLinea(int numeroActa, String localId) async {}

  @override
  Future<void> revisarLinea(
    int numeroActa,
    String localId,
    bool aprobado,
  ) async {}
}

void main() {
  testWidgets('acta cerrada: sin acciones de edición', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: DetalleActaLayout(
          numero: 120,
          acciones: _SinAcciones(),
          detalle: _detalleCerrada,
        ),
      ),
    );

    expect(find.text('Excavación manual'), findsOneWidget);
    expect(find.text('Cimentación'), findsOneWidget);
    expect(find.textContaining('cerrada'), findsOneWidget);
    expect(find.text('Agregar línea'), findsNothing);
    expect(find.text('Editar'), findsNothing);
    expect(find.text('Eliminar'), findsNothing);
    expect(find.byType(Switch), findsNothing);
  });
}
