import 'package:arquitectos_movil/app/tema.dart';
import 'package:arquitectos_movil/core/db/consultas.dart';
import 'package:arquitectos_movil/core/errores/mensajes.dart';
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

  testWidgets('línea propia sin descripción y sin sincronizar', (tester) async {
    final nueva = LineaDetalle(
      localId: 'c1',
      construccionId: 7,
      construccion: 'Cimentación',
      conceptoId: 1,
      tipoUsuarioId: 2,
      perfil: 'Arquitecto',
      descripcion: '',
      cantidad: '3.5',
      precio: '45000',
      subtotal: '157500',
      aprobado: true,
      estadoLocal: 'nuevo',
    );
    await tester.pumpWidget(
      MaterialApp(
        home: DetalleActaLayout(
          numero: 120,
          acciones: _ArquitectoEdita(),
          detalle: DetalleActa(
            numero: 120,
            fechaInicial: '2026-11-01',
            fechaFinal: '2026-11-30',
            arquitecto: 'Ana Arquitecta',
            revisor: 'Rosa Revisor',
            contratista: 'Carlos Contratista',
            observacion: null,
            cerrada: false,
            totalAprobado: Decimal.parse('157500'),
            lineas: [nueva],
          ),
        ),
      ),
    );

    expect(find.text('Sin descripción'), findsOneWidget);
    expect(
      find.text('Arquitecto · Aprobado · Sin sincronizar'),
      findsOneWidget,
    );
    expect(find.text('Agregar línea'), findsOneWidget);
    final lista = tester.widget<ListView>(find.byType(ListView));
    expect((lista.padding! as EdgeInsets).bottom, greaterThanOrEqualTo(88));
  });

  testWidgets('revisor: el interruptor se anuncia como «Aprobar»', (
    tester,
  ) async {
    final manejador = tester.ensureSemantics();
    await tester.pumpWidget(
      MaterialApp(
        home: DetalleActaLayout(
          numero: 120,
          acciones: _Revisor(),
          detalle: DetalleActa(
            numero: 120,
            fechaInicial: '2026-11-01',
            fechaFinal: '2026-11-30',
            arquitecto: 'Ana Arquitecta',
            revisor: 'Rosa Revisor',
            contratista: 'Carlos Contratista',
            observacion: null,
            cerrada: false,
            totalAprobado: Decimal.parse('1500'),
            lineas: [_lineaEjemplo],
          ),
        ),
      ),
    );
    expect(
      tester.getSemantics(find.byType(Switch)),
      matchesSemantics(
        label: 'Aprobar',
        hasToggledState: true,
        isToggled: true,
        hasEnabledState: true,
        isEnabled: true,
        isFocusable: true,
        hasTapAction: true,
        hasFocusAction: true,
      ),
    );
    manejador.dispose();
  });

  testWidgets('el aviso de datos actualizados no se pinta como error', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Column(
            children: [
              AvisoTexto(Mensajes.datosActualizados),
              AvisoTexto(Mensajes.sinConexion),
            ],
          ),
        ),
      ),
    );
    final esquema = Theme.of(tester.element(find.byType(Scaffold))).colorScheme;
    final cajas = tester
        .widgetList<DecoratedBox>(
          find.descendant(
            of: find.byType(AvisoTexto),
            matching: find.byType(DecoratedBox),
          ),
        )
        .map((caja) => (caja.decoration as BoxDecoration).color)
        .toList();
    expect(cajas, [esquema.primaryContainer, esquema.errorContainer]);
    expect(find.byIcon(Icons.check_circle_outline), findsOneWidget);
    expect(find.byIcon(Icons.error_outline), findsOneWidget);
  });
}

class _Revisor extends _SinAcciones {
  @override
  bool esRevisor() => true;
}

class _ArquitectoEdita extends _SinAcciones {
  @override
  bool puedeAgregarLinea(bool actaCerrada) => !actaCerrada;

  @override
  bool puedeEditarLinea(LineaDetalle linea, bool actaCerrada) =>
      !actaCerrada && linea.tipoUsuarioId == 2;
}
