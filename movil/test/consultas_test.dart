import 'dart:io';

import 'package:arquitectos_movil/core/db/consultas.dart';
import 'package:decimal/decimal.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'soporte.dart';

void main() {
  setUpAll(() {
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  late Directory carpeta;

  setUp(() async {
    carpeta = await carpetaPrueba('consultas');
  });

  tearDown(() => borrarCarpeta(carpeta));

  test(
    'el total aprobado ignora la línea rechazada y filtra cerradas',
    () async {
      final base = await baseConEsquema(
        '${carpeta.path}${Platform.pathSeparator}datos.sqlite',
      );
      await sembrar(base);
      final consultas = ConsultasLocales(base);

      final todas = await consultas.listarActas(const FiltroActas());
      expect(todas.map((acta) => acta.numero), [120, 80]);
      expect(todas.first.totalAprobado, Decimal.parse('1500.00'));
      expect(todas.first.arquitecto, 'Ana Arquitecta');

      final abiertas = await consultas.listarActas(
        const FiltroActas(estado: EstadoActaFiltro.abiertas),
      );
      expect(abiertas.map((acta) => acta.numero), [120]);

      final porNombre = await consultas.listarActas(
        const FiltroActas(texto: 'carlos'),
      );
      expect(porNombre, hasLength(2));

      final detalle = await consultas.detalleActa(120);
      expect(detalle!.lineas.map((linea) => linea.descripcion), [
        'Excavación manual',
        'Rechazada',
      ]);
      expect(detalle.totalAprobado, Decimal.parse('1500.00'));
      expect(detalle.observacion, 'Obra norte');

      await base.ejecutar(
        "INSERT INTO outbox (id_operacion, secuencia, tipo, numero_acta, payload_json, creada_en, estado) "
        "VALUES ('op', 1, 'AgregarLinea', 120, '{}', '2026-11-13T00:00:00Z', 'pendiente')",
      );
      expect(await consultas.pendientes(), 1);
      final conCambios = await consultas.listarActas(
        const FiltroActas(estado: EstadoActaFiltro.abiertas),
      );
      expect(conCambios.single.cambiosSinSincronizar, isTrue);
      await base.cerrar();
    },
  );

  test('la búsqueda de insumos ignora mayúsculas y tildes', () async {
    final base = await baseConEsquema(
      '${carpeta.path}${Platform.pathSeparator}insumos.sqlite',
    );
    await base.ejecutar(
      "INSERT INTO insumos (id, codigo, descripcion, unidad) VALUES (1, 'CEM', 'Cemento', 'bulto'), (2, 'ARN', 'Arena de río', 'm3')",
    );
    final consultas = ConsultasLocales(base);

    final porMinuscula = await consultas.buscarInsumos('cem');
    final porTilde = await consultas.buscarInsumos('rio');
    final corta = await consultas.buscarInsumos('c');

    expect(porMinuscula.map((item) => item.descripcion), ['Cemento']);
    expect(porTilde.map((item) => item.descripcion), ['Arena de río']);
    expect(corta, isEmpty);
    await base.cerrar();
  });
}
