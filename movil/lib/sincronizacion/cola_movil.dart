import 'dart:convert';

import 'package:decimal/decimal.dart';
import 'package:uuid/uuid.dart';

import '../core/db/base_local.dart';
import '../core/db/transaccion.dart';
import '../core/decimal/dinero.dart';
import 'modelos.dart';

class FilaCola {
  const FilaCola({
    required this.idOperacion,
    required this.secuencia,
    required this.tipo,
    required this.numeroActa,
    this.localId,
    required this.payloadJson,
    required this.creadaEn,
    required this.estado,
  });

  final String idOperacion;
  final int secuencia;
  final String tipo;
  final int numeroActa;
  final String? localId;
  final String payloadJson;
  final String creadaEn;
  final String estado;

  OperacionSalida aOperacion() {
    final mapa = jsonDecode(payloadJson) as Map<String, Object?>;
    return OperacionSalida(
      idOperacion: idOperacion,
      secuencia: secuencia,
      tipo: tipo,
      numeroActa: numeroActa,
      creadaEn: creadaEn,
      idCliente: mapa['idCliente']?.toString(),
      lineaId: _opcional(mapa['lineaId']),
      rowVersionLinea: mapa['rowVersionLinea']?.toString(),
      aprobado: mapa['aprobado'] as bool?,
      linea: mapa['linea'] is Map
          ? LineaSincronizacion.desdeJson(
              Map<String, Object?>.from(mapa['linea']! as Map),
            )
          : null,
    );
  }

  static int? _opcional(Object? valor) {
    if (valor == null) {
      return null;
    }
    return valor is int ? valor : int.tryParse(valor.toString());
  }
}

/// Compactación de la outbox según MODULO_MOVIL_FLUTTER §6.3.
class ColaMovil {
  ColaMovil({Uuid? uuid}) : _uuid = uuid ?? const Uuid();

  final Uuid _uuid;

  Future<int> siguienteSecuencia(BaseLocal base) async {
    final filas = await base.consultar(
      'SELECT MAX(secuencia) AS maximo FROM outbox',
    );
    final maximo = filas.first['maximo'];
    if (maximo == null) {
      return 1;
    }
    return (maximo is int ? maximo : int.parse(maximo.toString())) + 1;
  }

  Future<int> pendientes(BaseLocal base) async {
    final filas = await base.consultar(
      "SELECT COUNT(*) AS total FROM outbox WHERE estado IN ('pendiente', 'enviando')",
    );
    final total = filas.first['total'];
    return total is int ? total : int.parse(total.toString());
  }

  Future<List<FilaCola>> listarPendientes(BaseLocal base) async {
    final filas = await base.consultar('''
SELECT id_operacion, secuencia, tipo, numero_acta, local_id, payload_json, creada_en, estado
FROM outbox
WHERE estado IN ('pendiente', 'enviando')
ORDER BY secuencia
''');
    return filas
        .map(
          (fila) => FilaCola(
            idOperacion: fila['id_operacion']?.toString() ?? '',
            secuencia: _entero(fila['secuencia']),
            tipo: fila['tipo']?.toString() ?? '',
            numeroActa: _entero(fila['numero_acta']),
            localId: fila['local_id']?.toString(),
            payloadJson: fila['payload_json']?.toString() ?? '{}',
            creadaEn: fila['creada_en']?.toString() ?? '',
            estado: fila['estado']?.toString() ?? 'pendiente',
          ),
        )
        .toList();
  }

  Future<void> marcarEnviando(BaseLocal base, Iterable<String> ids) async {
    for (final id in ids) {
      await base.ejecutar(
        "UPDATE outbox SET estado = 'enviando' WHERE id_operacion = ?",
        [id],
      );
    }
  }

  Future<void> quitarOperacion(BaseLocal base, String idOperacion) async {
    await base.ejecutar('DELETE FROM outbox WHERE id_operacion = ?', [
      idOperacion,
    ]);
  }

  Future<void> devolverAPendiente(BaseLocal base, Iterable<String> ids) async {
    for (final id in ids) {
      await base.ejecutar(
        "UPDATE outbox SET estado = 'pendiente' WHERE id_operacion = ?",
        [id],
      );
    }
  }

  Future<void> recuperarEnviando(BaseLocal base) async {
    await base.ejecutar(
      "UPDATE outbox SET estado = 'pendiente' WHERE estado = 'enviando'",
    );
  }

  Future<void> agregarLinea({
    required BaseLocal base,
    required int numeroActa,
    required int tipoUsuarioId,
    required LineaSincronizacion linea,
    required DateTime creadaEn,
  }) async {
    await transaccion(base, () async {
      final idCliente = _uuid.v4();
      final localId = idCliente;
      final cantidad = redondearLejosDeCero(
        Decimal.parse(linea.cantidad),
        4,
      ).toString();
      final precio = redondearLejosDeCero(
        Decimal.parse(linea.precio),
        2,
      ).toString();
      final subtotal = subtotalLinea(
        Decimal.parse(cantidad),
        Decimal.parse(precio),
      ).toString();
      await base.ejecutar(
        '''
INSERT INTO acta_detalles (
  local_id, id, id_cliente, numero_acta, construccion_id, concepto_id, unidad_medida_id,
  insumo_id, presupuesto_item_id, cantidad, precio, subtotal, descripcion, tipo_usuario_id,
  aprobado, row_version, estado_local
) VALUES (?, NULL, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 1, NULL, 'nuevo')
''',
        [
          localId,
          idCliente,
          numeroActa,
          linea.construccionId,
          linea.conceptoId,
          linea.unidadMedidaId,
          linea.insumoId,
          linea.presupuestoItemId,
          cantidad,
          precio,
          subtotal,
          linea.descripcion.trim(),
          tipoUsuarioId,
        ],
      );
      await _insertarOutbox(
        base,
        localId: localId,
        numeroActa: numeroActa,
        tipo: 'AgregarLinea',
        payload: {
          'idCliente': idCliente,
          'linea': LineaSincronizacion(
            construccionId: linea.construccionId,
            conceptoId: linea.conceptoId,
            unidadMedidaId: linea.unidadMedidaId,
            insumoId: linea.insumoId,
            presupuestoItemId: linea.presupuestoItemId,
            cantidad: cantidad,
            precio: precio,
            descripcion: linea.descripcion.trim(),
          ).aJson(),
        },
        creadaEn: creadaEn,
      );
    });
  }

  Future<void> editarLinea({
    required BaseLocal base,
    required int numeroActa,
    required String localId,
    required LineaSincronizacion linea,
    required DateTime creadaEn,
  }) async {
    await transaccion(base, () async {
      final filas = await base.consultar(
        'SELECT id, id_cliente, estado_local, row_version FROM acta_detalles WHERE local_id = ?',
        [localId],
      );
      if (filas.isEmpty) {
        throw StateError('No se encontró la línea.');
      }
      final detalle = filas.first;
      final estado = detalle['estado_local']?.toString() ?? 'sincronizado';
      final cantidad = redondearLejosDeCero(
        Decimal.parse(linea.cantidad),
        4,
      ).toString();
      final precio = redondearLejosDeCero(
        Decimal.parse(linea.precio),
        2,
      ).toString();
      final subtotal = subtotalLinea(
        Decimal.parse(cantidad),
        Decimal.parse(precio),
      ).toString();
      await base.ejecutar(
        '''
UPDATE acta_detalles SET construccion_id = ?, concepto_id = ?, unidad_medida_id = ?, insumo_id = ?,
  presupuesto_item_id = ?, cantidad = ?, precio = ?, subtotal = ?, descripcion = ?,
  estado_local = CASE WHEN estado_local = 'nuevo' THEN 'nuevo' ELSE 'modificado' END
WHERE local_id = ?
''',
        [
          linea.construccionId,
          linea.conceptoId,
          linea.unidadMedidaId,
          linea.insumoId,
          linea.presupuestoItemId,
          cantidad,
          precio,
          subtotal,
          linea.descripcion.trim(),
          localId,
        ],
      );

      final payloadLinea = LineaSincronizacion(
        construccionId: linea.construccionId,
        conceptoId: linea.conceptoId,
        unidadMedidaId: linea.unidadMedidaId,
        insumoId: linea.insumoId,
        presupuestoItemId: linea.presupuestoItemId,
        cantidad: cantidad,
        precio: precio,
        descripcion: linea.descripcion.trim(),
      );

      if (estado == 'nuevo') {
        await _compactarAgregar(base, localId: localId, linea: payloadLinea);
        return;
      }

      final rowVersion = detalle['row_version']?.toString();
      final lineaId = detalle['id'] as int?;
      final idCliente = detalle['id_cliente']?.toString();
      await _compactarEditar(
        base,
        localId: localId,
        numeroActa: numeroActa,
        linea: payloadLinea,
        lineaId: lineaId,
        idCliente: idCliente,
        rowVersionLinea: rowVersion,
        creadaEn: creadaEn,
      );
    });
  }

  Future<void> eliminarLinea({
    required BaseLocal base,
    required int numeroActa,
    required String localId,
    required DateTime creadaEn,
  }) async {
    await transaccion(base, () async {
      final filas = await base.consultar(
        'SELECT id, id_cliente, estado_local, row_version FROM acta_detalles WHERE local_id = ?',
        [localId],
      );
      if (filas.isEmpty) {
        return;
      }
      final detalle = filas.first;
      final estado = detalle['estado_local']?.toString() ?? 'sincronizado';
      if (estado == 'nuevo') {
        await base.ejecutar('DELETE FROM outbox WHERE local_id = ?', [localId]);
        await base.ejecutar('DELETE FROM acta_detalles WHERE local_id = ?', [
          localId,
        ]);
        return;
      }
      await base.ejecutar(
        "UPDATE acta_detalles SET estado_local = 'eliminado' WHERE local_id = ?",
        [localId],
      );
      await _quitarPendientesDeLinea(base, localId);
      await _insertarOutbox(
        base,
        localId: localId,
        numeroActa: numeroActa,
        tipo: 'EliminarLinea',
        payload: {
          if (detalle['id'] != null) 'lineaId': detalle['id'],
          if (detalle['id_cliente'] != null) 'idCliente': detalle['id_cliente'],
          'rowVersionLinea': detalle['row_version'],
        },
        creadaEn: creadaEn,
      );
    });
  }

  Future<void> revisarLinea({
    required BaseLocal base,
    required int numeroActa,
    required String localId,
    required bool aprobado,
    required DateTime creadaEn,
  }) async {
    await transaccion(base, () async {
      final filas = await base.consultar(
        'SELECT id, row_version FROM acta_detalles WHERE local_id = ?',
        [localId],
      );
      if (filas.isEmpty) {
        return;
      }
      final detalle = filas.first;
      await base.ejecutar(
        'UPDATE acta_detalles SET aprobado = ?, estado_local = ? WHERE local_id = ?',
        [aprobado ? 1 : 0, 'modificado', localId],
      );
      await _compactarRevisar(
        base,
        localId: localId,
        numeroActa: numeroActa,
        lineaId: detalle['id'] as int?,
        rowVersionLinea: detalle['row_version']?.toString(),
        aprobado: aprobado,
        creadaEn: creadaEn,
      );
    });
  }

  Future<void> _compactarAgregar(
    BaseLocal base, {
    required String localId,
    required LineaSincronizacion linea,
  }) async {
    final filas = await base.consultar(
      "SELECT id_operacion, payload_json FROM outbox WHERE local_id = ? AND tipo = 'AgregarLinea' AND estado = 'pendiente'",
      [localId],
    );
    if (filas.isEmpty) {
      return;
    }
    final id = filas.first['id_operacion']!.toString();
    final mapa = jsonDecode(
      filas.first['payload_json']!.toString(),
    ) as Map<String, Object?>;
    mapa['linea'] = linea.aJson();
    await base.ejecutar(
      'UPDATE outbox SET payload_json = ? WHERE id_operacion = ?',
      [jsonEncode(mapa), id],
    );
  }

  Future<void> _compactarEditar(
    BaseLocal base, {
    required String localId,
    required int numeroActa,
    required LineaSincronizacion linea,
    required int? lineaId,
    required String? idCliente,
    required String? rowVersionLinea,
    required DateTime creadaEn,
  }) async {
    final filas = await base.consultar(
      "SELECT id_operacion, payload_json FROM outbox WHERE local_id = ? AND tipo = 'EditarLinea' AND estado = 'pendiente'",
      [localId],
    );
    final payload = {
      if (lineaId != null) 'lineaId': lineaId,
      if (idCliente != null) 'idCliente': idCliente,
      'rowVersionLinea': rowVersionLinea,
      'linea': linea.aJson(),
    };
    if (filas.isNotEmpty) {
      final id = filas.first['id_operacion']!.toString();
      final previo = jsonDecode(
        filas.first['payload_json']!.toString(),
      ) as Map<String, Object?>;
      previo['linea'] = linea.aJson();
      previo.putIfAbsent('rowVersionLinea', () => payload['rowVersionLinea']);
      await base.ejecutar(
        'UPDATE outbox SET payload_json = ? WHERE id_operacion = ?',
        [jsonEncode(previo), id],
      );
      return;
    }
    await _quitarPendientesDeLinea(base, localId);
    await _insertarOutbox(
      base,
      localId: localId,
      numeroActa: numeroActa,
      tipo: 'EditarLinea',
      payload: payload,
      creadaEn: creadaEn,
    );
  }

  Future<void> _compactarRevisar(
    BaseLocal base, {
    required String localId,
    required int numeroActa,
    required int? lineaId,
    required String? rowVersionLinea,
    required bool aprobado,
    required DateTime creadaEn,
  }) async {
    final filas = await base.consultar(
      "SELECT id_operacion FROM outbox WHERE local_id = ? AND tipo = 'RevisarLinea' AND estado = 'pendiente'",
      [localId],
    );
    final payload = {
      if (lineaId != null) 'lineaId': lineaId,
      'rowVersionLinea': rowVersionLinea,
      'aprobado': aprobado,
    };
    if (filas.isNotEmpty) {
      final id = filas.first['id_operacion']!.toString();
      await base.ejecutar(
        'UPDATE outbox SET payload_json = ? WHERE id_operacion = ?',
        [jsonEncode(payload), id],
      );
      return;
    }
    await _quitarPendientesDeLinea(base, localId);
    await _insertarOutbox(
      base,
      localId: localId,
      numeroActa: numeroActa,
      tipo: 'RevisarLinea',
      payload: payload,
      creadaEn: creadaEn,
    );
  }

  Future<void> _quitarPendientesDeLinea(BaseLocal base, String localId) async {
    await base.ejecutar(
      "DELETE FROM outbox WHERE local_id = ? AND tipo IN ('EditarLinea', 'RevisarLinea') AND estado = 'pendiente'",
      [localId],
    );
  }

  Future<void> _insertarOutbox(
    BaseLocal base, {
    required String localId,
    required int numeroActa,
    required String tipo,
    required Map<String, Object?> payload,
    required DateTime creadaEn,
  }) async {
    final id = _uuid.v4();
    final secuencia = await siguienteSecuencia(base);
    await base.ejecutar(
      '''
INSERT INTO outbox (
  id_operacion, secuencia, tipo, numero_acta, local_id, payload_json, creada_en, estado, intentos
) VALUES (?, ?, ?, ?, ?, ?, ?, 'pendiente', 0)
''',
      [
        id,
        secuencia,
        tipo,
        numeroActa,
        localId,
        jsonEncode(payload),
        creadaEn.toUtc().toIso8601String(),
      ],
    );
  }

  int _entero(Object? valor) =>
      valor is int ? valor : int.parse(valor.toString());
}
