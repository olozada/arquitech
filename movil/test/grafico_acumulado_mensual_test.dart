import 'package:arquitectos_movil/core/decimal/dinero.dart';
import 'package:arquitectos_movil/funciones/presupuestos/grafico_acumulado_mensual.dart';
import 'package:arquitectos_movil/funciones/presupuestos/presupuesto_modelos.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const puntos = [
    PuntoEjecucionMensual(anio: 2025, mes: 1, ejecutado: 100, acumulado: 100),
    PuntoEjecucionMensual(anio: 2025, mes: 2, ejecutado: 150, acumulado: 250),
  ];

  testWidgets('muestra el gráfico de acumulado con accesibilidad', (
    tester,
  ) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: GraficoAcumuladoMensual(puntos: puntos)),
      ),
    );
    expect(find.text('Ejecutado acumulado'), findsOneWidget);
    expect(find.byKey(const Key('grafico-acumulado')), findsOneWidget);
    expect(
      find.bySemanticsLabel(RegExp('Ejecutado acumulado por mes')),
      findsOneWidget,
    );
  });

  testWidgets('la leyenda arranca en el último mes', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: SizedBox(
            width: 360,
            child: GraficoAcumuladoMensual(puntos: puntos),
          ),
        ),
      ),
    );
    final ultimo = puntos.last;
    expect(
      find.text(
        '${ultimo.etiqueta}: acumulado ${dineroPresupuesto(ultimo.acumulado)} · '
        'del mes ${dineroPresupuesto(ultimo.ejecutado)}',
      ),
      findsOneWidget,
    );
  });

  testWidgets('al tocar un punto actualiza la leyenda', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: SizedBox(
            width: 360,
            height: 320,
            child: GraficoAcumuladoMensual(puntos: puntos),
          ),
        ),
      ),
    );

    final tamano = tester.getSize(find.byKey(const Key('grafico-acumulado')));
    final geometria = GeometriaGraficoAcumulado(tamano, puntos);
    await tester.tapAt(
      tester.getTopLeft(find.byKey(const Key('grafico-acumulado'))) +
          geometria.coords.first,
    );
    await tester.pump();

    final primero = puntos.first;
    expect(
      find.text(
        '${primero.etiqueta}: acumulado ${dineroPresupuesto(primero.acumulado)} · '
        'del mes ${dineroPresupuesto(primero.ejecutado)}',
      ),
      findsOneWidget,
    );
  });

  testWidgets('sin puntos no ocupa espacio', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: GraficoAcumuladoMensual(puntos: [])),
      ),
    );
    expect(find.text('Ejecutado acumulado'), findsNothing);
    expect(find.byKey(const Key('grafico-acumulado')), findsNothing);
  });
}
