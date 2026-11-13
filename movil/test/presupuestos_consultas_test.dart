import 'dart:convert';
import 'dart:io';

import 'package:arquitectos_movil/core/db/consultas.dart';
import 'package:arquitectos_movil/funciones/presupuestos/presupuesto_modelos.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'presupuesto_modelos_test.dart' show detallePresupuestoPrueba;
import 'soporte.dart';

void main() {
  setUpAll(() {
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test('presupuestoPorId lee los JSON del paquete', () async {
    final carpeta = await carpetaPrueba('presupuesto-db');
    addTearDown(() => borrarCarpeta(carpeta));
    final ruta = '${carpeta.path}${Platform.pathSeparator}local.sqlite';
    final base = await baseConEsquema(ruta);
    await sembrar(base);
    await base.ejecutar(
      '''
INSERT INTO presupuestos (
  id, construccion_id, construccion, nombre, version, estado, total,
  actualizado_en, detalle_json, validacion_json, seguimiento_json
) VALUES (
  5, 7, 'Cimentación', 'Paquete', 1, 'Aprobado', 1245.2,
  '2026-11-13', ?, '[]', '{}'
)
''',
      [jsonEncode(detallePresupuestoPrueba)],
    );
    final consultas = ConsultasLocales(base);
    final paquete = await consultas.presupuestoPorId(5);
    await base.cerrar();
    expect(paquete, isNotNull);
    expect(paquete!.nombre, 'Paquete');
    final detalle = PresupuestoDetalleVista.desdeJson(paquete.detalleJson);
    expect(detalle.capitulos.length, 1);
  });
}
