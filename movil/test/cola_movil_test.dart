import 'dart:io';

import 'package:arquitectos_movil/sincronizacion/cola_movil.dart';
import 'package:arquitectos_movil/sincronizacion/modelos.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';
import 'package:uuid/uuid.dart';

import 'soporte.dart';

void main() {
  setUpAll(() {
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test('editar una línea nueva fusiona en un solo AgregarLinea', () async {
    final carpeta = await carpetaPrueba('cola');
    addTearDown(() => borrarCarpeta(carpeta));
    final ruta = '${carpeta.path}${Platform.pathSeparator}local.sqlite';
    final base = await baseConEsquema(ruta);
    await sembrar(base);
    final cola = ColaMovil(uuid: const Uuid());
    final creada = DateTime.utc(2026, 11, 13, 12);

    await cola.agregarLinea(
      base: base,
      numeroActa: 120,
      tipoUsuarioId: 3,
      linea: const LineaSincronizacion(
        construccionId: 7,
        conceptoId: 1,
        cantidad: '1.0000',
        precio: '100.00',
        descripcion: 'Primera',
      ),
      creadaEn: creada,
    );
    final filasNueva = await base.consultar(
      "SELECT local_id FROM acta_detalles WHERE descripcion = 'Primera'",
    );
    final localId = filasNueva.first['local_id']!.toString();

    await cola.editarLinea(
      base: base,
      numeroActa: 120,
      localId: localId,
      linea: const LineaSincronizacion(
        construccionId: 7,
        conceptoId: 1,
        cantidad: '2.0000',
        precio: '150.00',
        descripcion: 'Editada',
      ),
      creadaEn: creada.add(const Duration(minutes: 1)),
    );

    expect(await cola.pendientes(base), 1);
    final colaFilas = await base.consultar(
      "SELECT tipo, payload_json FROM outbox WHERE estado = 'pendiente'",
    );
    expect(colaFilas.length, 1);
    expect(colaFilas.first['tipo'], 'AgregarLinea');
    expect(colaFilas.first['payload_json'], contains('Editada'));
    await base.cerrar();
  });

  test('eliminar una línea nueva quita la fila y la outbox', () async {
    final carpeta = await carpetaPrueba('cola-del');
    addTearDown(() => borrarCarpeta(carpeta));
    final ruta = '${carpeta.path}${Platform.pathSeparator}local.sqlite';
    final base = await baseConEsquema(ruta);
    await sembrar(base);
    final cola = ColaMovil(uuid: const Uuid());
    final creada = DateTime.utc(2026, 11, 13, 12);

    await cola.agregarLinea(
      base: base,
      numeroActa: 120,
      tipoUsuarioId: 3,
      linea: const LineaSincronizacion(
        construccionId: 7,
        conceptoId: 1,
        cantidad: '1.0000',
        precio: '100.00',
        descripcion: 'Borrar',
      ),
      creadaEn: creada,
    );
    final filas = await base.consultar(
      "SELECT local_id FROM acta_detalles WHERE descripcion = 'Borrar'",
    );
    final localId = filas.first['local_id']!.toString();

    await cola.eliminarLinea(
      base: base,
      numeroActa: 120,
      localId: localId,
      creadaEn: creada.add(const Duration(minutes: 1)),
    );

    expect(await cola.pendientes(base), 0);
    final restantes = await base.consultar(
      'SELECT COUNT(*) AS total FROM acta_detalles WHERE local_id = ?',
      [localId],
    );
    expect(restantes.first['total'], 0);
    await base.cerrar();
  });

  test('varias revisiones de la misma línea se fusionan', () async {
    final carpeta = await carpetaPrueba('cola-rev');
    addTearDown(() => borrarCarpeta(carpeta));
    final ruta = '${carpeta.path}${Platform.pathSeparator}local.sqlite';
    final base = await baseConEsquema(ruta);
    await sembrar(base);
    final cola = ColaMovil(uuid: const Uuid());
    const localId = 's1';
    await cola.revisarLinea(
      base: base,
      numeroActa: 120,
      localId: localId,
      aprobado: false,
      creadaEn: DateTime.utc(2026, 11, 13, 12),
    );
    await cola.revisarLinea(
      base: base,
      numeroActa: 120,
      localId: localId,
      aprobado: true,
      creadaEn: DateTime.utc(2026, 11, 13, 12, 1),
    );
    expect(await cola.pendientes(base), 1);
    final filas = await base.consultar(
      "SELECT payload_json FROM outbox WHERE local_id = ?",
      [localId],
    );
    expect(filas.first['payload_json'], contains('"aprobado":true'));
    await base.cerrar();
  });
}
