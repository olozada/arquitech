import 'dart:io';

import 'package:arquitectos_movil/core/api/errores.dart';
import 'package:arquitectos_movil/core/db/base_local.dart';
import 'package:arquitectos_movil/core/db/consultas.dart';
import 'package:arquitectos_movil/core/db/esquema.dart';
import 'package:arquitectos_movil/core/db/importador_paquete.dart';
import 'package:arquitectos_movil/core/errores/mensajes.dart';
import 'package:crypto/crypto.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'soporte.dart';

void main() {
  setUpAll(() {
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  late Directory carpeta;
  late ImportadorPaquete importador;

  setUp(() async {
    carpeta = await carpetaPrueba('importador');
    importador = ImportadorPaquete(
      cifrador: CifradorCopia(),
      abrir: AbrirFfi(),
    );
  });

  tearDown(() => borrarCarpeta(carpeta));

  test('importa el paquete y la app puede leer el acta', () async {
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final paquete = await paqueteDePrueba(origen);
    final destino =
        '${carpeta.path}${Platform.pathSeparator}arquitectos.sqlite';

    await importador.importar(
      contenidoGzip: paquete.gzip,
      sha256Esperado: paquete.sha256.toUpperCase(),
      clave: 'a' * 64,
      rutaActiva: destino,
      operacionesPendientes: 0,
      versionEsquema: 1,
    );

    final base = await AbrirFfi().abrir(destino);
    final actas = await ConsultasLocales(base).listarActas(const FiltroActas());
    expect(actas.first.numero, 120);
    await base.cerrar();
    expect(File('$destino.plano').existsSync(), isFalse);
  });

  test('una huella distinta no reemplaza la base', () async {
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final paquete = await paqueteDePrueba(origen);
    final destino =
        '${carpeta.path}${Platform.pathSeparator}arquitectos.sqlite';
    await File(destino).writeAsString('vieja');

    await expectLater(
      importador.importar(
        contenidoGzip: paquete.gzip,
        sha256Esperado: '0' * 64,
        clave: 'a' * 64,
        rutaActiva: destino,
        operacionesPendientes: 0,
        versionEsquema: 1,
      ),
      throwsA(
        isA<PaqueteInvalido>().having(
          (error) => error.mensaje,
          'mensaje',
          Mensajes.paqueteCorrupto,
        ),
      ),
    );
    expect(await File(destino).readAsString(), 'vieja');
  });

  test('no reemplaza la base si hay operaciones pendientes', () async {
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final paquete = await paqueteDePrueba(origen);

    expect(
      () => importador.importar(
        contenidoGzip: paquete.gzip,
        sha256Esperado: paquete.sha256,
        clave: 'b' * 64,
        rutaActiva:
            '${carpeta.path}${Platform.pathSeparator}arquitectos.sqlite',
        operacionesPendientes: 2,
        versionEsquema: 1,
      ),
      throwsA(isA<ReemplazoBloqueado>()),
    );
  });

  test('un esquema distinto se rechaza', () async {
    final plano = File('${carpeta.path}${Platform.pathSeparator}plano.sqlite');
    final db = await databaseFactoryFfiNoIsolate.openDatabase(plano.path);
    final base = BaseAdaptable(db);
    await aplicarEsquema(
      base,
      File('assets/esquema_movil_v1.sql').readAsStringSync(),
    );
    await base.ejecutar(
      "INSERT INTO meta (clave, valor) VALUES ('version_esquema', '2')",
    );
    await base.cerrar();
    final bytes = await plano.readAsBytes();

    expect(
      () => importador.importar(
        contenidoGzip: gzip.encode(bytes),
        sha256Esperado: sha256.convert(bytes).toString(),
        clave: 'c' * 64,
        rutaActiva: '${carpeta.path}${Platform.pathSeparator}activa.sqlite',
        operacionesPendientes: 0,
        versionEsquema: 1,
      ),
      throwsA(
        isA<PaqueteInvalido>().having(
          (error) => error.mensaje,
          'mensaje',
          Mensajes.paqueteDistinto,
        ),
      ),
    );
  });
}
