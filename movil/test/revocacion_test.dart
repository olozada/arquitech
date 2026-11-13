import 'dart:io';

import 'package:arquitectos_movil/app/controlador.dart';
import 'package:arquitectos_movil/core/seguridad/claves.dart';
import 'package:arquitectos_movil/sincronizacion/cola_movil.dart';
import 'package:arquitectos_movil/sincronizacion/modelos.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'soporte.dart';

void main() {
  setUpAll(() {
    TestWidgetsFlutterBinding.ensureInitialized();
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test('la revocación borra la base y vuelve al login', () async {
    final carpeta = await carpetaPrueba('revocado');
    addTearDown(() => borrarCarpeta(carpeta));
    final cliente = ClienteFalso(await paqueteDePrueba(carpeta));
    final deps = dependenciasPrueba(carpeta: carpeta, cliente: cliente);
    final controlador = ControladorAplicacion(deps);
    await controlador.iniciar();
    await controlador.entrar('ana', 'Clave12345');
    expect(controlador.tieneBase, isTrue);
    expect(await deps.repositorio.existe(), isTrue);

    final base = await deps.repositorio.obtener(
      await deps.almacen.leer(Claves.sql),
    );
    await deps.cola.agregarLinea(
      base: base,
      numeroActa: 120,
      tipoUsuarioId: 3,
      linea: const LineaSincronizacion(
        construccionId: 7,
        conceptoId: 1,
        cantidad: '1.0000',
        precio: '1.00',
        descripcion: 'Revocación',
      ),
      creadaEn: DateTime.utc(2026, 11, 13),
    );
    cliente.revocarEnSincronizar = true;
    await controlador.sincronizar();

    expect(controlador.ruta, RutaAplicacion.login);
    expect(controlador.tieneBase, isFalse);
    expect(await deps.repositorio.existe(), isFalse);
    expect(await deps.almacen.leer(Claves.refresh), isNull);
    expect(controlador.aviso, contains('revocado'));
  });

  test('cerrar sesión borra datos locales y claves', () async {
    final carpeta = await carpetaPrueba('cerrar');
    addTearDown(() => borrarCarpeta(carpeta));
    final cliente = ClienteFalso(await paqueteDePrueba(carpeta));
    final deps = dependenciasPrueba(carpeta: carpeta, cliente: cliente);
    final controlador = ControladorAplicacion(deps);
    await controlador.iniciar();
    await controlador.entrar('ana', 'Clave12345');

    final base = await deps.repositorio.obtener(
      await deps.almacen.leer(Claves.sql),
    );
    await deps.cola.agregarLinea(
      base: base,
      numeroActa: 120,
      tipoUsuarioId: 3,
      linea: const LineaSincronizacion(
        construccionId: 7,
        conceptoId: 1,
        cantidad: '1.0000',
        precio: '10.00',
        descripcion: 'Pendiente',
      ),
      creadaEn: DateTime.utc(2026, 11, 13),
    );

    await controlador.cerrarSesion();

    expect(controlador.ruta, RutaAplicacion.login);
    expect(await deps.repositorio.existe(), isFalse);
    expect(await deps.almacen.leer(Claves.refresh), isNull);
    final pendientes = File(
      '${carpeta.path}${Platform.pathSeparator}pendientes.sqlite',
    );
    expect(pendientes.existsSync(), isFalse);
  });
}
