import 'dart:convert';
import 'dart:io';

import 'package:arquitectos_movil/app/aplicacion.dart';
import 'package:arquitectos_movil/app/controlador.dart';
import 'package:arquitectos_movil/app/dependencias.dart';
import 'package:arquitectos_movil/app/formulario_adaptable.dart';
import 'package:arquitectos_movil/app/tema.dart';
import 'package:arquitectos_movil/core/db/pendientes_db.dart';
import 'package:arquitectos_movil/funciones/actas/formulario_linea.dart';
import 'package:arquitectos_movil/funciones/actas/lista_actas.dart';
import 'package:arquitectos_movil/funciones/autenticacion/cambio_contrasena_pantalla.dart';
import 'package:arquitectos_movil/funciones/autenticacion/desbloqueo_pantalla.dart';
import 'package:arquitectos_movil/funciones/autenticacion/login_pantalla.dart';
import 'package:arquitectos_movil/funciones/cuenta/cuenta_pantalla.dart';
import 'package:arquitectos_movil/funciones/inicio/inicio_pantalla.dart';
import 'package:arquitectos_movil/funciones/pendientes/pendientes_pantalla.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'soporte.dart';

const _tamanos = <String, Size>{
  'teléfono pequeño': Size(320, 568),
  'teléfono': Size(390, 844),
  'teléfono horizontal': Size(844, 390),
  'tablet vertical': Size(800, 1280),
  'tablet horizontal': Size(1280, 800),
};

void main() {
  setUpAll(() {
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  late Directory carpeta;
  late Dependencias dependencias;
  late ControladorAplicacion controlador;

  setUp(() async {
    carpeta = await carpetaPrueba('responsive');
    dependencias = dependenciasPrueba(
      carpeta: carpeta,
      cliente: ClienteFalso(PaqueteListo(gzip: const [], sha256: '')),
    );
    controlador = ControladorAplicacion(dependencias);
  });

  tearDown(() async {
    await dependencias.pendientes.cerrar();
    await dependencias.repositorio.cerrar();
    await borrarCarpeta(carpeta);
  });

  Future<void> mostrar(
    WidgetTester tester,
    Size tamano,
    Widget pantalla,
  ) async {
    tester.view.physicalSize = tamano;
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    await tester.runAsync(() async {
      await tester.pumpWidget(
        ProviderScope(
          overrides: [controladorProvider.overrideWith((ref) => controlador)],
          child: MaterialApp(theme: temaArquitectos(), home: pantalla),
        ),
      );
      await Future<void>.delayed(const Duration(milliseconds: 100));
    });
    await tester.pump();
  }

  Finder boton(String texto) => find.ancestor(
    of: find.text(texto),
    matching: find.byWidgetPredicate((widget) => widget is FilledButton),
  );

  group('sin desbordes en ningún tamaño', () {
    for (final MapEntry(key: nombre, value: tamano) in _tamanos.entries) {
      testWidgets('login · $nombre', (tester) async {
        await mostrar(tester, tamano, const LoginPantalla());
        expect(tester.takeException(), isNull);
        expect(find.byKey(const Key('usuario')), findsOneWidget);
        expect(find.byKey(const Key('entrar')), findsOneWidget);
      });

      testWidgets('cambio de contraseña · $nombre', (tester) async {
        await mostrar(tester, tamano, const CambioContrasenaPantalla());
        expect(tester.takeException(), isNull);
        expect(find.byKey(const Key('guardar-contrasena')), findsOneWidget);
      });

      testWidgets('desbloqueo · $nombre', (tester) async {
        await mostrar(tester, tamano, const DesbloqueoPantalla());
        expect(tester.takeException(), isNull);
        expect(find.byKey(const Key('desbloquear')), findsOneWidget);
      });

      testWidgets('formulario de línea · $nombre', (tester) async {
        await mostrar(
          tester,
          tamano,
          const FormularioLineaPantalla(numeroActa: 1),
        );
        expect(find.text('Cargando catálogos…'), findsNothing);
        await tester.enterText(
          find.widgetWithText(TextField, 'Cantidad'),
          '2,5',
        );
        await tester.enterText(
          find.widgetWithText(TextField, 'Precio'),
          '1000',
        );
        await tester.pump();
        expect(tester.takeException(), isNull);
        expect(find.text('Subtotal'), findsOneWidget);
        expect(boton('Guardar'), findsOneWidget);
      });

      testWidgets('lista de actas · $nombre', (tester) async {
        await mostrar(tester, tamano, const ListaActasPantalla());
        expect(tester.takeException(), isNull);
        expect(find.byKey(const Key('buscar-acta')), findsOneWidget);
        expect(find.text('No hay actas descargadas.'), findsOneWidget);
      });

      testWidgets('inicio · $nombre', (tester) async {
        await mostrar(tester, tamano, const InicioPantalla());
        expect(tester.takeException(), isNull);
        expect(find.byKey(const Key('sincronizar')), findsOneWidget);
        expect(
          tester.getSize(find.byKey(const Key('sincronizar'))).width,
          lessThanOrEqualTo(480),
        );
      });

      testWidgets('cuenta · $nombre', (tester) async {
        await mostrar(tester, tamano, const CuentaPantalla());
        expect(tester.takeException(), isNull);
        expect(find.byKey(const Key('cerrar-sesion')), findsOneWidget);
      });

      testWidgets('pendientes con un conflicto · $nombre', (tester) async {
        await tester.runAsync(() async {
          await dependencias.pendientes.abrirDb(null);
          await dependencias.pendientes.insertar(
            OperacionFallidaRegistro(
              idOperacion: 'op-1',
              secuencia: 1,
              tipo: 'EditarLinea',
              numeroActa: 120,
              localId: 's1',
              estado: 'Conflicto',
              mensaje: 'Otra persona modificó esta línea.',
              payloadJson: jsonEncode({
                'linea': {
                  'construccionId': 7,
                  'conceptoId': 1,
                  'cantidad': '2',
                  'precio': '750',
                  'descripcion': 'Excavación manual',
                },
              }),
              resultadoJson: '{}',
              registradaEn: '2026-11-19T00:00:00Z',
            ),
          );
        });
        await mostrar(tester, tamano, const PendientesPantalla());
        expect(tester.takeException(), isNull);
        expect(find.text('Rehacer'), findsOneWidget);
        expect(find.text('Descartar'), findsOneWidget);
      });
    }
  });

  group('el layout se adapta al ancho', () {
    testWidgets('login: una columna en teléfono, dos paneles en tablet', (
      tester,
    ) async {
      await mostrar(tester, _tamanos['teléfono']!, const LoginPantalla());
      expect(find.text('Arquitectos'), findsOneWidget);
      expect(find.text('Iniciar sesión'), findsNothing);

      await mostrar(
        tester,
        _tamanos['tablet horizontal']!,
        const LoginPantalla(),
      );
      expect(find.text('Arquitectos'), findsOneWidget);
      expect(find.text('Iniciar sesión'), findsOneWidget);
      final campo = tester.getRect(find.byKey(const Key('usuario')));
      expect(campo.left, greaterThan(640));
      expect(campo.width, lessThanOrEqualTo(420));
    });

    testWidgets('formulario de línea: campos apilados en teléfono pequeño', (
      tester,
    ) async {
      await mostrar(
        tester,
        _tamanos['teléfono pequeño']!,
        const FormularioLineaPantalla(numeroActa: 1),
      );
      final construccion = tester.getRect(
        find.widgetWithText(DropdownButtonFormField<int>, 'Construcción'),
      );
      final concepto = tester.getRect(
        find.widgetWithText(DropdownButtonFormField<int>, 'Concepto'),
      );
      expect(concepto.top, greaterThan(construccion.bottom));
      final cantidad = tester.getRect(
        find.widgetWithText(TextField, 'Cantidad'),
      );
      final precio = tester.getRect(find.widgetWithText(TextField, 'Precio'));
      expect(precio.top, greaterThan(cantidad.bottom));
      expect(tester.getSize(boton('Guardar')).width, 320 - 2 * 16);
    });

    testWidgets('formulario de línea: cantidad y precio en fila en teléfono', (
      tester,
    ) async {
      await mostrar(
        tester,
        _tamanos['teléfono']!,
        const FormularioLineaPantalla(numeroActa: 1),
      );
      final cantidad = tester.getRect(
        find.widgetWithText(TextField, 'Cantidad'),
      );
      final precio = tester.getRect(find.widgetWithText(TextField, 'Precio'));
      expect(precio.top, cantidad.top);
      expect(precio.left, greaterThan(cantidad.right));
    });

    testWidgets(
      'formulario de línea: dos columnas y botón compacto en tablet',
      (tester) async {
        await mostrar(
          tester,
          _tamanos['tablet horizontal']!,
          const FormularioLineaPantalla(numeroActa: 1),
        );
        final construccion = tester.getRect(
          find.widgetWithText(DropdownButtonFormField<int>, 'Construcción'),
        );
        final cantidad = tester.getRect(
          find.widgetWithText(TextField, 'Cantidad'),
        );
        expect(cantidad.left, greaterThan(construccion.right));
        expect(tester.getSize(boton('Guardar')).width, 280);
      },
    );

    testWidgets('lista de actas: filtros en panel lateral en tablet', (
      tester,
    ) async {
      await mostrar(
        tester,
        _tamanos['tablet horizontal']!,
        const ListaActasPantalla(),
      );
      expect(find.byType(VerticalDivider), findsOneWidget);
      expect(
        tester.getRect(find.byKey(const Key('buscar-acta'))).right,
        lessThanOrEqualTo(360),
      );

      await mostrar(tester, _tamanos['teléfono']!, const ListaActasPantalla());
      expect(find.byType(VerticalDivider), findsNothing);
    });
  });

  test('clases de tamaño de ventana de Material 3', () {
    expect(TamanoVentana.desdeAncho(599), TamanoVentana.compacto);
    expect(TamanoVentana.desdeAncho(600), TamanoVentana.mediano);
    expect(TamanoVentana.desdeAncho(839), TamanoVentana.mediano);
    expect(TamanoVentana.desdeAncho(840), TamanoVentana.expandido);
  });
}
