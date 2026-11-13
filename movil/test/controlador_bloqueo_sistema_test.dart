import 'dart:io';

import 'package:arquitectos_movil/app/controlador.dart';
import 'package:arquitectos_movil/app/dependencias.dart';
import 'package:arquitectos_movil/core/errores/mensajes.dart';
import 'package:arquitectos_movil/core/seguridad/desbloqueador.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'soporte.dart';

void main() {
  setUpAll(() {
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test('sin bloqueo del sistema, el login se rechaza', () async {
    final carpeta = await carpetaPrueba('bloqueo-login');
    addTearDown(() => borrarCarpeta(carpeta));
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final paquete = await paqueteDePrueba(origen);
    final dependencias = dependenciasPrueba(
      carpeta: carpeta,
      cliente: ClienteFalso(paquete),
    ).copyWith(desbloqueador: DesbloqueadorFijo(true, soportaBloqueo: false));
    final controlador = ControladorAplicacion(dependencias);
    addTearDown(controlador.dispose);

    await controlador.iniciar();
    await controlador.entrar('ana', 'Clave12345');

    expect(controlador.aviso, Mensajes.requiereBloqueo);
    expect(controlador.ruta, RutaAplicacion.login);
  });

  test('con bloqueo del sistema, el login continúa', () async {
    final carpeta = await carpetaPrueba('bloqueo-ok');
    addTearDown(() => borrarCarpeta(carpeta));
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final paquete = await paqueteDePrueba(origen);
    final dependencias = dependenciasPrueba(
      carpeta: carpeta,
      cliente: ClienteFalso(paquete),
    );
    final controlador = ControladorAplicacion(dependencias);
    addTearDown(controlador.dispose);

    await controlador.iniciar();
    await controlador.entrar('ana', 'Clave12345');

    expect(controlador.ruta, RutaAplicacion.inicio);
  });

  test('sin bloqueo en desbloqueo, muestra mensaje y no entra', () async {
    final carpeta = await carpetaPrueba('bloqueo-des');
    addTearDown(() => borrarCarpeta(carpeta));
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final dependencias = dependenciasPrueba(
      carpeta: carpeta,
      cliente: ClienteFalso(await paqueteDePrueba(origen)),
    ).copyWith(desbloqueador: DesbloqueadorFijo(true, soportaBloqueo: false));
    final controlador = ControladorAplicacion(dependencias);
    addTearDown(controlador.dispose);

    await controlador.iniciar();
    controlador.ruta = RutaAplicacion.desbloqueo;
    await controlador.desbloquear();

    expect(controlador.aviso, Mensajes.requiereBloqueo);
    expect(controlador.ruta, RutaAplicacion.desbloqueo);
  });
}

extension on Dependencias {
  Dependencias copyWith({Desbloqueador? desbloqueador}) {
    return Dependencias(
      configuracion: configuracion,
      cliente: cliente,
      almacen: almacen,
      desbloqueador: desbloqueador ?? this.desbloqueador,
      red: red,
      repositorio: repositorio,
      importador: importador,
      cola: cola,
      pendientes: pendientes,
      sincronizador: sincronizador,
      comunes: comunes,
      directorio: directorio,
      ahora: ahora,
      identidad: identidad,
    );
  }
}
