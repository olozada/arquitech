import 'dart:convert';

import 'package:decimal/decimal.dart';
import 'package:uuid/uuid.dart';

import '../core/db/base_local.dart';
import '../core/db/transaccion.dart';
import '../core/decimal/dinero.dart';
import 'modelos.dart';

part 'cola_movil_lineas.dart';

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
abstract class ColaBase {
  Uuid get _uuid;

  Future<int> siguienteSecuencia(BaseLocal base);
}

class ColaMovil extends ColaBase with ColaMovilLineas {
  ColaMovil({Uuid? uuid}) : _uuid = uuid ?? const Uuid();

  @override
  final Uuid _uuid;

  @override
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

  int _entero(Object? valor) =>
      valor is int ? valor : int.parse(valor.toString());
}
