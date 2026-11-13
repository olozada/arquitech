import 'dart:io';

import 'package:arquitectos_movil/app/controlador.dart';
import 'package:arquitectos_movil/app/limpieza_local.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'soporte.dart';

void main() {
  setUpAll(() {
    TestWidgetsFlutterBinding.ensureInitialized();
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test('alIniciar borra restos y conserva la base activa', () async {
    final carpeta = await carpetaPrueba('limpieza-inicio');
    addTearDown(() => borrarCarpeta(carpeta));
    final activa = File(
      '${carpeta.path}${Platform.pathSeparator}arquitectos.sqlite',
    );
    await activa.writeAsString('base-activa');
    await File(
      '${carpeta.path}${Platform.pathSeparator}arquitectos.sqlite.plano',
    ).writeAsString('plano');
    await File('${carpeta.path}${Platform.pathSeparator}paquete.sqlite.gz')
        .writeAsString('gz');

    await LimpiezaLocal.alIniciar(carpeta);

    expect(await activa.exists(), isTrue);
    expect(await activa.readAsString(), 'base-activa');
    expect(
      await File(
        '${carpeta.path}${Platform.pathSeparator}arquitectos.sqlite.plano',
      ).exists(),
      isFalse,
    );
    expect(
      await File('${carpeta.path}${Platform.pathSeparator}paquete.sqlite.gz')
          .exists(),
      isFalse,
    );
  });

  test('cerrar sesión elimina la carpeta descargas', () async {
    final carpeta = await carpetaPrueba('limpieza-borrar');
    addTearDown(() => borrarCarpeta(carpeta));
    final descargas = await LimpiezaLocal.carpetaDescargas(carpeta);
    await File('${descargas.path}${Platform.pathSeparator}acta-1.pdf')
        .writeAsBytes(const [1, 2, 3]);
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final dependencias = dependenciasPrueba(
      carpeta: carpeta,
      cliente: ClienteFalso(await paqueteDePrueba(origen)),
    );
    final controlador = ControladorAplicacion(dependencias);
    addTearDown(controlador.dispose);
    await controlador.iniciar();
    await controlador.entrar('ana', 'Clave12345');
    await controlador.cerrarSesion();

    expect(await descargas.exists(), isFalse);
  });
}
