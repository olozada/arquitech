import 'dart:io';

import 'package:arquitectos_movil/core/db/base_local.dart';
import 'package:arquitectos_movil/core/db/consultas.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'soporte.dart';

Future<void> _sembrarGrande(BaseLocal base) async {
  await sembrar(base);
  const actas = 2000;
  const lineasPorActa = 20;
  await base.ejecutar('BEGIN');
  try {
    for (var acta = 1000; acta < 1000 + actas; acta++) {
      await base.ejecutar(
        '''
INSERT INTO actas (
  numero_acta, fecha_inicial, fecha_final, arquitecto_id, revisor_id, contratista_id,
  cerrada, fecha_registro, row_version
) VALUES (?, '2026-06-01', '2026-06-30', 1, 1, 1, 0, '2026-06-01T00:00:00Z', 'RV')
''',
        [acta],
      );
      for (var linea = 0; linea < lineasPorActa; linea++) {
        final localId = 'a$acta-l$linea';
        await base.ejecutar(
          '''
INSERT INTO acta_detalles (
  local_id, id, numero_acta, construccion_id, concepto_id, cantidad, precio, subtotal,
  descripcion, tipo_usuario_id, aprobado, estado_local
) VALUES (?, NULL, ?, 7, 1, '1.0000', '10.00', '10.00', ?, 3, 1, 'sincronizado')
''',
          [localId, acta, 'Línea $linea'],
        );
      }
    }
    await base.ejecutar('COMMIT');
  } catch (error) {
    await base.ejecutar('ROLLBACK');
    rethrow;
  }
}

void main() {
  setUpAll(() {
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test(
    'listarActas con 2000 actas × 20 líneas responde en menos de 300 ms',
    () async {
      final carpeta = await carpetaPrueba('consultas-rendimiento');
      addTearDown(() => borrarCarpeta(carpeta));
      final base = await baseConEsquema(
        '${carpeta.path}${Platform.pathSeparator}rendimiento.sqlite',
      );
      await _sembrarGrande(base);
      final consultas = ConsultasLocales(base);
      final inicio = DateTime.now();
      final actas = await consultas.listarActas(const FiltroActas());
      final ms = DateTime.now().difference(inicio).inMilliseconds;
      expect(actas.length, greaterThanOrEqualTo(2000));
      expect(ms, lessThan(300));
      await base.cerrar();
    },
    timeout: const Timeout(Duration(minutes: 3)),
  );
}
