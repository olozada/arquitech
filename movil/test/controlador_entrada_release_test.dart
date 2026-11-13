import 'dart:io';

import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:arquitectos_movil/app/controlador.dart';
import 'package:arquitectos_movil/core/errores/mensajes.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'soporte.dart';

void main() {
  setUpAll(() {
    TestWidgetsFlutterBinding.ensureInitialized();
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test('entrar con API http se rechaza en release simulado', () async {
    final carpeta = await carpetaPrueba('login-http');
    addTearDown(() => borrarCarpeta(carpeta));
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final dependencias = dependenciasPrueba(
      carpeta: carpeta,
      cliente: ClienteFalso(await paqueteDePrueba(origen)),
      configuracion: const ConfiguracionApp(
        urlApi: 'http://api.insegura',
        versionApp: '1.0.0',
        buildApp: '1',
        esRelease: true,
      ),
    );
    final controlador = ControladorAplicacion(dependencias);
    addTearDown(controlador.dispose);
    await controlador.iniciar();
    await controlador.entrar('ana', 'Clave12345');
    expect(controlador.aviso, Mensajes.faltaServidor);
    expect(controlador.ruta, RutaAplicacion.login);
  });
}
