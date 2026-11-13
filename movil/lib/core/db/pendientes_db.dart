import 'dart:io';

import 'package:flutter/services.dart';

import 'base_local.dart';
import 'esquema.dart';

class PendientesDb {
  PendientesDb({required this.abrir, required this.ruta});

  final AbrirBase abrir;
  final Future<String> Function() ruta;
  BaseLocal? _abierta;

  Future<BaseLocal> abrirDb(String? clave) async {
    final abierta = _abierta;
    if (abierta != null) {
      return abierta;
    }
    final archivo = File(await ruta());
    if (!await archivo.exists()) {
      final base = await abrir.abrir(archivo.path, clave: clave);
      await aplicarEsquema(
        base,
        await rootBundle.loadString('assets/esquema_pendientes_v1.sql'),
      );
      await base.cerrar();
    }
    final base = await abrir.abrir(archivo.path, clave: clave);
    _abierta = base;
    return base;
  }

  Future<void> cerrar() async {
    final abierta = _abierta;
    _abierta = null;
    if (abierta != null) {
      await abierta.cerrar();
    }
  }

  Future<int> contar() async {
    final base = _abierta;
    if (base == null) {
      return 0;
    }
    final filas = await base.consultar(
      'SELECT COUNT(*) AS total FROM operaciones_fallidas',
    );
    final total = filas.first['total'];
    return total is int ? total : int.parse(total.toString());
  }

  Future<List<OperacionFallidaRegistro>> listar() async {
    final base = _abierta;
    if (base == null) {
      return const [];
    }
    final filas = await base.consultar('''
SELECT id_operacion, secuencia, tipo, numero_acta, local_id, estado, mensaje,
       payload_json, resultado_json, registrada_en
FROM operaciones_fallidas
ORDER BY secuencia
''');
    return filas.map(OperacionFallidaRegistro.desdeFila).toList();
  }

  Future<void> insertar(OperacionFallidaRegistro registro) async {
    final base = _abierta!;
    await base.ejecutar(
      '''
INSERT INTO operaciones_fallidas (
  id_operacion, secuencia, tipo, numero_acta, local_id, estado, mensaje,
  payload_json, resultado_json, registrada_en
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
''',
      [
        registro.idOperacion,
        registro.secuencia,
        registro.tipo,
        registro.numeroActa,
        registro.localId,
        registro.estado,
        registro.mensaje,
        registro.payloadJson,
        registro.resultadoJson,
        registro.registradaEn,
      ],
    );
  }

  Future<void> borrar(String idOperacion) async {
    await _abierta!.ejecutar(
      'DELETE FROM operaciones_fallidas WHERE id_operacion = ?',
      [idOperacion],
    );
  }
}

class OperacionFallidaRegistro {
  const OperacionFallidaRegistro({
    required this.idOperacion,
    required this.secuencia,
    required this.tipo,
    required this.numeroActa,
    this.localId,
    required this.estado,
    required this.mensaje,
    required this.payloadJson,
    required this.resultadoJson,
    required this.registradaEn,
  });

  final String idOperacion;
  final int secuencia;
  final String tipo;
  final int numeroActa;
  final String? localId;
  final String estado;
  final String mensaje;
  final String payloadJson;
  final String resultadoJson;
  final String registradaEn;

  factory OperacionFallidaRegistro.desdeFila(Map<String, Object?> fila) {
    return OperacionFallidaRegistro(
      idOperacion: fila['id_operacion']?.toString() ?? '',
      secuencia: _entero(fila['secuencia']),
      tipo: fila['tipo']?.toString() ?? '',
      numeroActa: _entero(fila['numero_acta']),
      localId: fila['local_id']?.toString(),
      estado: fila['estado']?.toString() ?? '',
      mensaje: fila['mensaje']?.toString() ?? '',
      payloadJson: fila['payload_json']?.toString() ?? '',
      resultadoJson: fila['resultado_json']?.toString() ?? '',
      registradaEn: fila['registrada_en']?.toString() ?? '',
    );
  }

  static int _entero(Object? valor) =>
      valor is int ? valor : int.parse(valor.toString());
}
