import 'dart:io';

import 'package:arquitectos_movil/app/controlador.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'soporte.dart';

void main() {
  setUpAll(() {
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test(
    'a los cinco minutos en segundo plano vuelve a pedir desbloqueo',
    () async {
      final carpeta = await carpetaPrueba('bloqueo');
      addTearDown(() => borrarCarpeta(carpeta));
      final origen = await Directory(
        '${carpeta.path}${Platform.pathSeparator}origen',
      ).create();
      final paquete = await paqueteDePrueba(origen);
      var reloj = DateTime.utc(2026, 11, 13, 12);
      final controlador = ControladorAplicacion(
        dependenciasPrueba(
          carpeta: carpeta,
          cliente: ClienteFalso(paquete),
          ahora: () => reloj,
        ),
      );
      await controlador.iniciar();
      await controlador.entrar('ana', 'Clave12345');
      expect(controlador.ruta, RutaAplicacion.inicio);

      controlador.alFondo();
      reloj = reloj.add(const Duration(minutes: 4));
      controlador.alFrente();
      expect(controlador.ruta, RutaAplicacion.inicio);

      controlador.alFondo();
      reloj = reloj.add(const Duration(minutes: 5));
      controlador.alFrente();
      expect(controlador.ruta, RutaAplicacion.desbloqueo);
      controlador.dispose();
    },
  );
}
