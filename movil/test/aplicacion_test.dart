import 'dart:io';

import 'package:arquitectos_movil/app/aplicacion.dart';
import 'package:arquitectos_movil/app/controlador.dart';
import 'package:arquitectos_movil/app/dependencias.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'soporte.dart';

void main() {
  setUpAll(() {
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  late Directory carpeta;
  late Dependencias dependencias;
  late ControladorAplicacion controlador;

  setUp(() async {
    carpeta = await carpetaPrueba('aplicacion');
    dependencias = dependenciasPrueba(
      carpeta: carpeta,
      cliente: ClienteFalso(PaqueteListo(gzip: const [], sha256: '')),
    );
    controlador = ControladorAplicacion(dependencias);
  });

  tearDown(() async {
    controlador.dispose();
    await dependencias.repositorio.cerrar();
    await borrarCarpeta(carpeta);
  });

  testWidgets('la aplicación arranca y cubre la pantalla al pasar a segundo plano', (
    tester,
  ) async {
    await tester.pumpWidget(
      Aplicacion(dependencias: dependencias, controlador: controlador),
    );
    await tester.pump();

    expect(find.text('Abriendo Arquitectos…'), findsOneWidget);
    expect(tester.takeException(), isNull);

    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.inactive);
    await tester.pump();

    expect(find.text('Arquitectos'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
